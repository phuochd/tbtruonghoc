#Requires -Version 7
<#
.SYNOPSIS
    Smoke test (Story 1.11): the app survives a MariaDB stop/start without
    manual intervention and without creating duplicate Site rows.

.DESCRIPTION
    Run by hand, outside `dotnet test` - it stops the shared Compose `mariadb`
    container, which the xUnit suite depends on, so it must never run inside
    that suite.

    Steps:
      1. Check the Compose `mariadb` service is running and healthy.
      2. Build the app into a separate artifacts folder (so a developer's own
         `dotnet run` locking bin/Debug does not get in the way) and start
         one instance of it on a free local port, in Development.
      3. Probe both sites with a live database; record the Piranha_Sites rows.
      4. `stop` the mariadb container and confirm the app observes the outage
         (the probe fails) while its process stays alive.
      5. `start` the container, wait for its healthcheck, and poll until the
         SAME app process serves both sites again.
      6. Confirm Piranha_Sites still holds exactly the expected rows.

    The probe requests a random, non-existent slug on each site. Piranha has
    to query the database to resolve it, so it answers 404 with the database
    up and fails (5xx / connection error) with it down. `GET /` alone proves
    nothing here: UseMemoryCache() can serve it without touching the DB.

    The script itself only reads the database. The app instance it starts
    still runs its normal startup migrate/seed, which is a no-op on an
    already-seeded database. The mariadb container is always started again
    on exit, even on failure.

.PARAMETER StartupTimeoutSeconds
    How long to wait for the app to start serving.

.PARAMETER RecoveryTimeoutSeconds
    How long to wait for mariadb to report healthy again, and then (separately)
    for the app to serve both sites again.

.EXAMPLE
    docker compose up -d mariadb
    pwsh scripts/smoke-mariadb-restart.ps1
#>
[CmdletBinding()]
param(
    [int]$StartupTimeoutSeconds = 180,
    [int]$RecoveryTimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ProjectDir = Join-Path $RepoRoot 'src/TbTruongHoc.Web'
$ArtifactsDir = Join-Path $RepoRoot 'bin/smoke-mariadb-restart'
$ExpectedSiteIds = @('tbtruonghoc', 'trongdoitam-net')
# Matches appsettings.Development.json's Sites:*:Hostnames - requests carry
# these as the Host header, so no hosts-file entries are needed.
$SiteHosts = @('tbtruonghoc.local', 'trongdoitam.local')

function Write-Step([string]$message) { Write-Host "==> $message" -ForegroundColor Cyan }
function Write-Pass([string]$message) { Write-Host "    PASS $message" -ForegroundColor Green }

# ---------------------------------------------------------------- docker ---

# Docker Desktop ships `docker compose` as a CLI plugin, but some installs
# only have the standalone `docker-compose` binary on PATH.
function Resolve-ComposeCommand {
    # `docker inspect` (health checks below) needs the docker CLI either way.
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw '`docker` is not on PATH. Start Docker Desktop (or add its resources\bin folder to PATH) and retry.'
    }
    & docker compose version *> $null
    if ($LASTEXITCODE -eq 0) { return @('docker', 'compose') }
    if (Get-Command docker-compose -ErrorAction SilentlyContinue) { return @('docker-compose') }
    throw 'Neither `docker compose` nor `docker-compose` is available.'
}

# @() keeps a one-element result ('docker-compose') an array, not a string.
# The explicit -f pins the project to this repo whatever the caller's working
# directory is (the .env next to it is then picked up too).
$Compose = @(Resolve-ComposeCommand) + @('-f', (Join-Path $RepoRoot 'docker-compose.yml'))

# Returns stdout only, so compose/client warnings on stderr can never leak
# into parsed output (container ids, Piranha_Sites rows); stderr is kept for
# the error message.
function Invoke-Compose {
    $exe = $Compose[0]
    $prefix = @($Compose | Select-Object -Skip 1)
    $all = & $exe @prefix @args 2>&1
    $stdout = @($all | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] })
    if ($LASTEXITCODE -ne 0) {
        throw "'$($Compose -join ' ') $($args -join ' ')' failed (exit $LASTEXITCODE): $($all | Out-String)"
    }
    return $stdout
}

function Get-MariaDbContainerId {
    $id = (Invoke-Compose ps -q mariadb | Out-String).Trim()
    if (-not $id) { throw 'The Compose `mariadb` service has no container. Run `docker compose up -d mariadb` first.' }
    return $id
}

function Get-MariaDbHealth([string]$containerId) {
    $status = & docker inspect --format '{{.State.Status}}/{{if .State.Health}}{{.State.Health.Status}}{{end}}' $containerId 2>&1
    if ($LASTEXITCODE -ne 0) { throw "docker inspect failed: $status" }
    return ($status | Out-String).Trim()
}

function Wait-MariaDbHealthy([string]$containerId, [int]$timeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    do {
        $health = Get-MariaDbHealth $containerId
        if ($health -eq 'running/healthy') { return }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)
    throw "mariadb did not report healthy within $timeoutSeconds s (last state: $health)."
}

function Get-SiteInternalIds {
    # Uses the container's own MYSQL_* env, so no credentials live here.
    $sql = 'SELECT InternalId FROM Piranha_Sites ORDER BY InternalId'
    $rows = Invoke-Compose exec -T mariadb sh -c "mariadb -u`"`$MYSQL_USER`" -p`"`$MYSQL_PASSWORD`" `"`$MYSQL_DATABASE`" -N -e '$sql'"
    return @($rows | ForEach-Object { "$_".Trim() } | Where-Object { $_ })
}

function Assert-ExpectedSites([string]$when) {
    $actual = Get-SiteInternalIds
    if (($actual -join ',') -ne (($ExpectedSiteIds | Sort-Object) -join ',')) {
        throw "Piranha_Sites $when holds [$($actual -join ', ')]; expected exactly [$($ExpectedSiteIds -join ', ')]."
    }
    Write-Pass "Piranha_Sites $when = [$($actual -join ', ')]"
}

# ------------------------------------------------------------------- app ---

$Http = [System.Net.Http.HttpClient]::new([System.Net.Http.HttpClientHandler]@{ AllowAutoRedirect = $false })
$Http.Timeout = [TimeSpan]::FromSeconds(15)

# Returns the HTTP status code, or 0 when no response came back at all.
function Get-Status([string]$baseUrl, [string]$hostName, [string]$path) {
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Get, "$baseUrl$path")
    $request.Headers.Host = $hostName
    try {
        $response = $Http.SendAsync($request).GetAwaiter().GetResult()
        try { return [int]$response.StatusCode } finally { $response.Dispose() }
    }
    catch { return 0 }
    finally { $request.Dispose() }
}

# One round of checks on every site: home page 200 and the DB-touching
# unknown-slug probe 404. Returns a list of failure descriptions (empty = ok).
function Test-SitesServing([string]$baseUrl) {
    $failures = @()
    foreach ($hostName in $SiteHosts) {
        $homeStatus = Get-Status $baseUrl $hostName '/'
        if ($homeStatus -ne 200) { $failures += "$hostName / -> $homeStatus (want 200)" }
        $probe = Get-Status $baseUrl $hostName "/smoke-$([guid]::NewGuid().ToString('N'))"
        if ($probe -ne 404) { $failures += "$hostName probe -> $probe (want 404)" }
    }
    return $failures
}

function Wait-SitesServing([string]$baseUrl, [System.Diagnostics.Process]$process, [int]$timeoutSeconds, [string]$what) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    do {
        Assert-AppAlive $process
        $failures = @(Test-SitesServing $baseUrl)
        if ($failures.Count -eq 0) { return }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)
    throw "$what did not happen within $timeoutSeconds s. Last round: $($failures -join '; ')"
}

function Assert-AppAlive([System.Diagnostics.Process]$process) {
    if ($process.HasExited) { throw "The app process exited (code $($process.ExitCode)). See $script:AppLog." }
}

function Get-FreePort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try { return $listener.LocalEndpoint.Port } finally { $listener.Stop() }
}

# ------------------------------------------------------------------ main ---

$app = $null
$mariaDbStopped = $false
$script:AppLog = Join-Path $ArtifactsDir 'app.log'
$appErrLog = Join-Path $ArtifactsDir 'app.err.log'

try {
    Write-Step 'Checking the Compose mariadb service'
    $containerId = Get-MariaDbContainerId
    $health = Get-MariaDbHealth $containerId
    if ($health -ne 'running/healthy') {
        throw "mariadb is '$health', not running/healthy. Run ``docker compose up -d mariadb`` and wait for it first."
    }
    Write-Pass "mariadb container $($containerId.Substring(0, 12)) is healthy"

    Write-Step "Building the app into $ArtifactsDir"
    $buildOutput = & dotnet build $ProjectDir --artifacts-path $ArtifactsDir --nologo -v quiet 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed:`n$($buildOutput | Out-String)" }
    $dll = Get-ChildItem (Join-Path $ArtifactsDir 'bin') -Recurse -Filter 'TbTruongHoc.Web.dll' |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $dll) { throw "Build succeeded but TbTruongHoc.Web.dll was not found under $ArtifactsDir/bin." }
    # An incremental build has been seen to "succeed" into an output folder
    # missing every NuGet dependency, so the app then dies at startup with
    # "Could not load file or assembly 'Piranha'". Rebuild once from scratch.
    if (-not (Test-Path (Join-Path $dll.DirectoryName 'Piranha.dll'))) {
        Write-Host '    Package DLLs missing from the build output - rebuilding without incremental build'
        $buildOutput = & dotnet build $ProjectDir --artifacts-path $ArtifactsDir --nologo -v quiet --no-incremental 2>&1
        if ($LASTEXITCODE -ne 0) { throw "dotnet build --no-incremental failed:`n$($buildOutput | Out-String)" }
        if (-not (Test-Path (Join-Path $dll.DirectoryName 'Piranha.dll'))) {
            throw "Piranha.dll is still missing from $($dll.DirectoryName). Delete $ArtifactsDir and retry."
        }
    }

    $port = Get-FreePort
    $baseUrl = "http://127.0.0.1:$port"
    Write-Step "Starting the app on $baseUrl (Development)"
    # Development is what loads user-secrets (the connection string) and the
    # .local hostnames. The working directory is the project folder, so it is
    # the content root (appsettings, Views, wwwroot) exactly as `dotnet run`.
    $savedEnv = @{ ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT; ASPNETCORE_URLS = $env:ASPNETCORE_URLS }
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = $baseUrl
    try {
        $app = Start-Process dotnet -ArgumentList "`"$($dll.FullName)`"" -WorkingDirectory $ProjectDir `
            -RedirectStandardOutput $script:AppLog -RedirectStandardError $appErrLog -PassThru -NoNewWindow
    }
    finally {
        $env:ASPNETCORE_ENVIRONMENT = $savedEnv.ASPNETCORE_ENVIRONMENT
        $env:ASPNETCORE_URLS = $savedEnv.ASPNETCORE_URLS
    }

    Wait-SitesServing $baseUrl $app $StartupTimeoutSeconds 'App startup (both sites serving)'
    Write-Pass "app (PID $($app.Id)) serves both sites; unknown-slug probe -> 404"
    Assert-ExpectedSites 'before the outage'

    Write-Step 'Stopping mariadb (simulated outage)'
    $mariaDbStopped = $true
    Invoke-Compose stop mariadb | Out-Null
    $outage = @()
    $seen = @()
    foreach ($hostName in $SiteHosts) {
        $status = Get-Status $baseUrl $hostName "/smoke-$([guid]::NewGuid().ToString('N'))"
        $seen += "$hostName -> $status"
        # Only a 5xx or no response at all counts as the outage being seen.
        if ($status -ne 0 -and $status -lt 500) { $outage += "$hostName ($status)" }
    }
    if ($outage.Count -gt 0) {
        throw "With mariadb stopped, the probe did not fail for [$($outage -join ', ')] - it is not reaching the database, so this run proves nothing."
    }
    Assert-AppAlive $app
    Write-Pass "the probe fails while mariadb is down ($($seen -join '; ')), and the app process is still alive"

    Write-Step 'Starting mariadb again'
    Invoke-Compose start mariadb | Out-Null
    Wait-MariaDbHealthy $containerId $RecoveryTimeoutSeconds
    $mariaDbStopped = $false
    Write-Pass 'mariadb is healthy again'

    Write-Step 'Waiting for the same app process to recover'
    Wait-SitesServing $baseUrl $app $RecoveryTimeoutSeconds 'Recovery (both sites serving again)'
    Write-Pass "app (PID $($app.Id)) serves both sites again with no restart"
    Assert-ExpectedSites 'after recovery'

    Write-Host ''
    Write-Host 'SMOKE TEST PASSED' -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ''
    Write-Host "SMOKE TEST FAILED: $($_.Exception.Message)" -ForegroundColor Red
    foreach ($log in @($script:AppLog, $appErrLog)) {
        if ((Test-Path $log) -and (Get-Item $log).Length -gt 0) {
            Write-Host "--- last lines of $log ---"
            Get-Content $log -Tail 30 | Write-Host
        }
    }
    exit 1
}
finally {
    if ($app -and -not $app.HasExited) { Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue }
    if ($mariaDbStopped) {
        # Never leave the shared dev database down, whatever failed.
        # Wait for healthy too, so a `dotnet test` run straight after a failed
        # smoke run does not hit a database that is still starting.
        Write-Host 'Starting mariadb again after a failed run...'
        try {
            Invoke-Compose start mariadb | Out-Null
            Wait-MariaDbHealthy $containerId 180
            Write-Host 'mariadb is healthy again.'
        }
        catch { Write-Host "mariadb may not be healthy yet: $($_.Exception.Message)" -ForegroundColor Red }
    }
    $Http.Dispose()
}
