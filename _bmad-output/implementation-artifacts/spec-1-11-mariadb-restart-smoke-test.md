---
title: 'Smoke-test script for MariaDB restart/recovery (Story 1.11)'
type: 'chore'
created: '2026-09-25'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Nothing automated checks that the app survives a MariaDB outage. The live stop/recover cycle was checked by hand once, in Story 1.1, and never again. `DockerComposeConfigTests` only checks the compose YAML, and the shared-DB xUnit suite cannot stop its own database. The Story 1.1 check also predates `abb5a75`: the app now runs via `dotnet run` against the Compose `mariadb`, not as a Compose service.

**Approach:** Add a standalone PowerShell 7 script, run by hand outside `dotnet test`, that:
1. Starts its own app instance.
2. Proves both sites serve with a live DB.
3. Stops the `mariadb` container, then starts it again.
4. Confirms, without manual intervention, that the same app process reconnects and serves both sites again.
5. Confirms `Piranha_Sites` holds exactly the two expected rows before and after the outage.

The script exits non-zero on any failure.

</frozen-after-approval>

## Implementation Notes

Planning decisions (made in step-02, none of them visible to the user as an intent change):

- **Location/language:** `scripts/smoke-mariadb-restart.ps1`, `#Requires -Version 7`. The dev machine is Windows and pwsh 7 is cross-platform, so no `.sh` twin.
- **App lifecycle:** the script starts its own `dotnet run --project src/TbTruongHoc.Web --no-launch-profile` with `ASPNETCORE_ENVIRONMENT=Development` (needed for user-secrets and the `.local` hostnames) and `--urls http://127.0.0.1:<port>`, on a port that does not collide with the launch profile's 5236. It always kills the process tree in `finally`. The script builds first. If the build fails because a user's own `dotnet run` locks `bin/`, it fails with a clear message (see Story 1.10's lock note).
- **DB-touching probe (key point):** `UseMemoryCache()` means `GET /` can come from cache without touching the DB, so a 200 on `/` proves nothing after recovery. The probe is a random non-existent slug (`/smoke-<guid>`) per site, sent with the `Host` header set to that site's `.local` hostname. Piranha must query the DB to resolve it: the expected result is 404 with the DB up and 5xx while it is down. The probe is read-only and creates no rows. `GET /` for each site must also return 200 before and after. Implementation must confirm the 404-vs-5xx split empirically.
- **Outage observed:** after `docker compose stop mariadb`, the probe must return a non-404 failure (5xx or connection error). Otherwise the test did not actually exercise an outage and the script fails. The app process must still be alive.
- **Recovery:** after `docker compose start mariadb`, wait for the container's healthcheck to report `healthy`, then poll the probe until both sites return 404 and `/` returns 200. Both waits have timeouts and nothing is restarted by hand.
- **Site rows:** read via `docker compose exec -T mariadb sh -c 'mariadb -u"$MYSQL_USER" -p"$MYSQL_PASSWORD" "$MYSQL_DATABASE" -N -e "SELECT InternalId FROM Piranha_Sites ORDER BY InternalId"'`. This uses the container's own env, so there is no `.env` parsing and no credentials in the script. The result must be exactly `tbtruonghoc` and `trongdoitam-net`, each once, both before and after.
- **Preconditions:** `docker compose up -d mariadb` must already be healthy. The script checks this and fails fast with a hint if not. It never runs `down`/`-v` and never mutates data.
- **Docs:** add a README section "Smoke test: MariaDB restart/recovery". Point the `DockerComposeConfigTests` class comment at the script.

Implementation (branch `story/1-11-mariadb-restart-smoke-test`):

- Files: new `scripts/smoke-mariadb-restart.ps1`; README section "Smoke test: MariaDB restart/recovery"; `DockerComposeConfigTests` class comment now names the script (comment only).
- **Surprise: compose CLI.** This machine only has the standalone `docker-compose.exe`, not the `docker compose` plugin. The script resolves `docker compose` first and falls back to `docker-compose`. Also, `docker` was not on the agent shell's PATH (Docker Desktop's `resources\bin`), so verification prepended that path.
- **Build isolation:** the user's own `dotnet run` (PID 23496) held `bin/Debug`. The script builds with `--artifacts-path bin/smoke-mariadb-restart` (git-ignored via `bin/`) and runs the built DLL with the project folder as the working directory, so the folder is also the content root. The DLL is not started via `dotnet run`. The port comes from a loopback `TcpListener(0)`.
- **Probe confirmed empirically:** with mariadb up, an unknown slug gives 404 on both sites. With it stopped, both give 500, and the app log shows `Piranha.Repositories.PageRepository.GetBySlug` → `MySqlConnector ... Couldn't connect to server`. So the probe does reach the DB.
- PowerShell gotchas fixed during the first run: `$home` collides with the read-only automatic `$HOME`. A one-element function result unrolls to a string, so `$Compose` is wrapped in `@()`.
- Verified: a full run printed `SMOKE TEST PASSED`, exit 0. The same app PID served before and after, and `Piranha_Sites` = `[tbtruonghoc, trongdoitam-net]` both times. A forced-failure run (`-RecoveryTimeoutSeconds 1`) exited 1 and printed the app log tail. Its `finally` killed the app process and started mariadb again, which was healthy ~20 s later.
- No app code changed. The app already recovers on its own (MySqlConnector discards dead pooled sessions), so `EnableRetryOnFailure` stays out, as rejected in Story 1.1.
- Supersedes the planning bullets "App lifecycle" and "Preconditions" above. The script runs the built DLL with `Stop-Process` on its single PID (no child tree, so no tree-kill). A locked `bin/` is sidestepped via `--artifacts-path` instead of failing. The script issues no writes, but the app it starts still runs its normal startup migrate/seed.
- Post-review verification: rerun from a working directory outside the repo gave `SMOKE TEST PASSED`, exit 0. The forced failure (`-RecoveryTimeoutSeconds 1`) exited 1, and `finally` now waited until mariadb reported healthy. `dotnet test --filter DockerComposeConfigTests --artifacts-path bin/story-1-11`: 4/4 passed.

## Review Triage Log

Layer run: `blind-hunter` (the only configured layer; none skipped). 12 findings.

- **[low, patch]** Compose calls depended on the caller's working directory (no `-f`). Compose does search parent folders, so running from `scripts/` worked, but running from outside the repo would fail. Fixed: `-f <repo>/docker-compose.yml` is baked into `$Compose`. Verified by a passing run from the session scratchpad dir.
- **[low, patch]** The `docker-compose` fallback still needs `docker` for `docker inspect`. Fixed: `docker` is now required up front, with a clear PATH hint.
- **[medium, patch]** `Invoke-Compose` merged stderr (`2>&1`) into parsed output, so a compose or client warning would corrupt the container id or the `Piranha_Sites` rows. Fixed: it returns stdout only (ErrorRecords are filtered out) and keeps the full output for the error message.
- **[medium, patch]** The outage check accepted any non-404 status, including a 200/302, as "outage observed", which is looser than the spec's "5xx or connection error". Fixed: it now requires `0` or `>= 500`.
- **[low, patch]** On failure only `app.log` was tailed, and `app.err.log` was captured but never shown. Fixed: both are tailed when non-empty.
- **[low, patch]** "Never writes to the database" was inaccurate, because the started app runs its startup migrate/seed. Fixed the wording in the script help and the README.
- **[low, patch]** The failure-path `finally` started mariadb without waiting for it to be healthy, so a `dotnet test` run straight after could hit a DB that was still starting. Fixed: a bounded `Wait-MariaDbHealthy` runs there, with errors caught.
- **[low, patch]** The planning bullets in Implementation Notes contradicted the final design. Implementation Notes are append-only, so a superseding note was appended.
- **[maybe-false, defer]** Starting the app while the DB is down probably crashes startup, since the `abb5a75` Compose change removed the restart safety net. Logged in deferred-work.md (would be medium). It is outside this story's live-outage scope.
- **[low, rejected]** "The before/after Site check can hardly fail, since seeding runs only at startup." That is the AC as written (rows before and after the outage cycle of a running app). Seed idempotency across restarts is already covered by `SiteSeedIdempotencyTests`. Adding an app restart would extend the frozen scope.
- **[low, rejected]** Site IDs and hostnames are hard-coded, duplicating config. A drift would make the script fail loudly (a 404 on `/` or a site-set mismatch), not pass silently. Parsing `appsettings` adds machinery for a rare change.
- **[low, rejected]** The `Get-FreePort` release-then-bind race: it needs another process to grab the same ephemeral port within milliseconds, and a hit fails loudly at startup.
- **[false]** "`Assert-AppAlive` is used before it is defined." PowerShell resolves functions at call time, and every definition runs before `main`.
- **[low, rejected]** "No automated parse check for the script." It is run by hand and fails immediately on a syntax error. Adding an xUnit parser test for a manual tool is not worth it.
- **[false]** "`last_updated` went backwards (16:30 → 16:10)." 16:10 is the actual system time when it was written. The previous 16:30 value was ahead of the real clock.
