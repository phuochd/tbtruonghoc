using System;
using System.IO;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Partial, static coverage for the spec's I/O &amp; Edge-Case Matrix row 5
/// ("docker compose up on a clean checkout"). This test only guards the
/// *configuration* that makes the described resilience behavior possible -
/// it is a regression check against someone accidentally loosening the
/// pinned image tag, dropping the healthcheck, or losing the database
/// volume - not a live test of the actual crash/retry/recovery behavior
/// itself.
///
/// Only mariadb runs under Compose: the app itself is started with
/// `dotnet run` against the Compose-exposed port, so there is no app
/// service here to assert a depends_on gate or a media mount on.
///
/// The live behavior (the app reconnecting after mariadb restarts, data
/// surviving a real container restart) is deliberately NOT exercised here.
/// Simulating it faithfully means stopping/restarting the shared
/// docker-compose "mariadb" container that this same test run's other
/// integration tests (HostnameResolutionTests, SiteSeedIdempotencyTests)
/// depend on, and waiting out real healthcheck/retry timers (many tens of
/// seconds) - that belongs in a standalone script outside this suite.
/// </summary>
public class DockerComposeConfigTests
{
    [Fact]
    public void MariaDb_Image_Is_Pinned_To_10_11_Not_Latest()
    {
        var compose = ReadDockerComposeYaml();

        Assert.Contains("image: mariadb:10.11", compose);
        Assert.DoesNotContain("mariadb:latest", compose);
    }

    [Fact]
    public void MariaDb_Exposes_A_Healthcheck_Callers_Can_Wait_On()
    {
        var compose = ReadDockerComposeYaml();
        var mariadbSection = ExtractServiceBlock(compose, "mariadb:");

        // `docker compose up -d mariadb` only reports "healthy" once this
        // healthcheck passes, which is what a developer (and Story 1.11's
        // recovery script) waits on before starting the app against it.
        Assert.Contains("healthcheck:", mariadbSection);
        Assert.Contains("retries:", mariadbSection);
    }

    [Fact]
    public void MariaDb_Has_Restart_Policy_So_It_Comes_Back_After_A_Restart()
    {
        var compose = ReadDockerComposeYaml();
        var mariadbSection = ExtractServiceBlock(compose, "mariadb:");

        // If the container or the Docker daemon goes down, this policy is
        // what brings the database back instead of leaving it dead - it must
        // not be "no" or missing.
        Assert.Contains("restart: unless-stopped", mariadbSection);
    }

    [Fact]
    public void Database_Data_Persists_Via_A_Mounted_Volume()
    {
        var compose = ReadDockerComposeYaml();

        var mariadbSection = ExtractServiceBlock(compose, "mariadb:");
        Assert.Contains("mariadb-data:/var/lib/mysql", mariadbSection);

        // The named volume must also be declared at the top level, or the
        // mount above silently becomes an anonymous volume that a
        // `docker compose down` discards along with the database.
        Assert.Matches(@"(?m)^volumes:\s*$(\s|\S)*?^\s+mariadb-data:", compose);
    }

    private static string ReadDockerComposeYaml()
    {
        var path = Path.Combine(FindRepoRoot(), "docker-compose.yml");
        Assert.True(File.Exists(path), $"docker-compose.yml not found at expected path: {path}");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Extracts the top-level service block (e.g. "mariadb:" or
    /// "piranha-app:") from the compose file's raw text, up to the next
    /// top-level ("services:"-indented) key or end of file.
    /// </summary>
    private static string ExtractServiceBlock(string composeYaml, string serviceHeader)
    {
        var lines = composeYaml.Replace("\r\n", "\n").Split('\n');
        var blockLines = new System.Collections.Generic.List<string>();
        var inBlock = false;

        foreach (var line in lines)
        {
            if (!inBlock)
            {
                if (line.TrimStart() == serviceHeader && line.StartsWith("  "))
                {
                    inBlock = true;
                    blockLines.Add(line);
                }
                continue;
            }

            // A new service (2-space indent, e.g. another "service:") or a
            // new top-level section (0-space indent, e.g. the file's
            // trailing "volumes:" key) both end the current block.
            var trimmedStart = line.TrimStart(' ');
            var indent = line.Length - trimmedStart.Length;
            var isSiblingKey = trimmedStart.Length > 0
                && indent <= 2
                && line.TrimEnd().EndsWith(":");

            if (isSiblingKey)
            {
                break;
            }

            blockLines.Add(line);
        }

        Assert.True(blockLines.Count > 1, $"Could not locate a '{serviceHeader}' service block in docker-compose.yml");
        return string.Join('\n', blockLines);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TbTruongHoc.sln")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new InvalidOperationException(
                $"Could not locate repo root (TbTruongHoc.sln) starting from {AppContext.BaseDirectory}");
        }

        return dir.FullName;
    }
}
