---
title: 'Piranha multi-site scaffold for Site A & Site B'
type: 'feature'
created: '2026-09-18'
status: 'done'
route: 'dispatch'
baseline_commit: 'NO_VCS'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No Piranha CMS project exists yet. Site A (tbtruonghoc) and Site B (trongdoitam.net) must launch from one shared platform from day one, not diverge into two deployments.

**Approach:** Scaffold a single Piranha CMS 12.2.0 / .NET 8 solution from the official `piranha.mvc` template, wire it to MariaDB 10.11 via Pomelo's `MariaDbServerVersion`, seed two `Site` records (tbtruonghoc as `IsDefault`, trongdoitam.net), and stand up a pinned Docker Compose dev environment (`piranha-app` + `mariadb`) with volume-mounted media.

## Boundaries & Constraints

**Always:**
- Pin the repo's dotnet SDK to 8.x via `global.json` — this machine also has 10.x SDKs installed, and Piranha 12.2.0 does not support net10.0.
- Use Piranha.Templates `piranha.mvc` scaffold and `Piranha.Data.EF.MySql` 12.0.0 (Pomelo, `MariaDbServerVersion`) exactly per architecture AD-1.
- Pin the `mariadb` Compose service image to exactly `10.11`, never `:latest`.
- Create exactly two `Site` records in this story: `tbtruonghoc` (`IsDefault = true`), `trongdoitam.net` (`IsDefault = false`). Seed them deterministically (migration/startup seed), not via manual Manager setup.
- Local dev hostname resolution uses hosts-file aliases (`tbtruonghoc.local`, `trongdoitam.local` → 127.0.0.1), configured only in `appsettings.Development.json` — prod hostnames stay untouched. Document the required one-time hosts-file edit for developers.

**Never:**
- Never stand up a second Piranha instance or deployment "for Site B" — one instance, one database, one Manager login (AD-1).
- Never build SEO fields, per-site contact settings, the lead form, or analytics settings in this story — those are Stories 1.2–1.6.
- Never add a third `Site` record for trongngocanh.com (Site C) — explicitly deferred past phase 1.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Request to Site A hostname | Host header matches tbtruonghoc's configured hostname | Resolves to the `tbtruonghoc` Site record | N/A |
| Request to Site B hostname | Host header matches trongdoitam.net's configured hostname | Resolves to the `trongdoitam.net` Site record | N/A |
| Request to an unmapped/unknown hostname | Host header matches neither configured hostname | Falls back to the `IsDefault` site (`tbtruonghoc`) | N/A |
| Re-running app startup | Site records already exist from a prior run | Seed step is a no-op — no duplicate `Site` records created | N/A |
| `docker compose up` on a clean checkout | No prior containers/volumes | `piranha-app` connects to pinned `mariadb:10.11`; media folder persists via mounted volume | If `mariadb` fails to become ready before `piranha-app` connects, `piranha-app` must retry/wait rather than crash-loop permanently |

</frozen-after-approval>

## Code Map

- `global.json` -- new; pins dotnet SDK to the installed `8.0.406` so `dotnet` commands in this repo don't pick up the 10.x SDKs also present on this machine
- `src/TbTruongHoc.Web/` -- new; output of `dotnet new piranha.mvc`, the Piranha 12.2.0 MVC app (.NET 8)
- `src/TbTruongHoc.Web/appsettings.json`, `appsettings.Development.json` -- new; MariaDB connection string via Pomelo `MariaDbServerVersion`; Development file also carries the `tbtruonghoc.local` / `trongdoitam.local` hostname mapping
- `docker-compose.yml` -- new; `mariadb` service pinned to `mariadb:10.11` + `piranha-app` service built from a local Dockerfile, named volume for DB data, volume-mounted media folder
- `src/TbTruongHoc.Web/Dockerfile` -- new; builds/runs the scaffolded app for Compose
- Startup/seed code inside `src/TbTruongHoc.Web/` (e.g. `Data/SiteSeed.cs` or equivalent Piranha startup hook) -- new; idempotently creates the two `Site` records if they don't already exist

## Tasks & Acceptance

**Execution:**
- [x] `global.json` -- add, pinned to `8.0.406` -- prevents ambient 10.x SDK resolution on this machine
- [x] `src/TbTruongHoc.Web/` -- scaffold via `dotnet new piranha.mvc` -- establishes the Piranha 12.2.0 MVC app per AD-1
- [x] `src/TbTruongHoc.Web/*.csproj` / connection config -- wire `Piranha.Data.EF.MySql` 12.0.0 with `MariaDbServerVersion` against MariaDB 10.11 -- matches architecture stack table
- [x] `docker-compose.yml`, `src/TbTruongHoc.Web/Dockerfile` -- add, `mariadb:10.11` pinned + `piranha-app`, volume-mounted media + named DB volume -- local/prod parity
- [x] Startup seed -- add, creates `tbtruonghoc` (`IsDefault=true`) and `trongdoitam.net` Site records idempotently -- AC1
- [x] `appsettings.Development.json` (+ note in repo docs) -- configure `tbtruonghoc.local` / `trongdoitam.local` hostname mapping and document the required hosts-file entries -- makes AC1's per-hostname resolution testable locally
- [x] `tests/TbTruongHoc.Web.Tests/` -- add, xUnit + `WebApplicationFactory<Program>` integration tests against the real MariaDB, covering the I/O Matrix rows -- closes the Matrix Test Audit gate

**Acceptance Criteria:**
- Given a fresh scaffold, when the app starts against MariaDB 10.11, then exactly two `Site` records exist (`tbtruonghoc` `IsDefault=true`, `trongdoitam.net` `IsDefault=false`), and re-running startup creates no duplicates.
- Given `docker compose up` from a clean checkout, when containers start, then `piranha-app` connects successfully to the pinned `mariadb:10.11` service and the media folder persists across container restarts via the mounted volume.
- Given the scaffolded repo, when inspected, then no second Piranha instance, deployment config, or duplicate Site/database setup exists anywhere.

## Implementation Notes

- Raw `dotnet new piranha.mvc` output defaulted to `net9.0` and SQLite packages; corrected to `net8.0` and swapped `Piranha.Data.EF.SQLite`/`Piranha.AspNetCore.Identity.SQLite` → `Piranha.Data.EF.MySql`/`Piranha.AspNetCore.Identity.MySQL` (both pinned `12.0.0`) per the spec's boundary.
- Piranha's own `Db<T>` base class auto-inserts a placeholder `Site` (`InternalId="Default"`) on first touch of an empty database. `SiteSeed.cs` detects and repurposes that framework-seeded row into the `tbtruonghoc` record instead of inserting a third row — verified exactly 2 `Site` rows on a clean DB and across restarts.
- Host port mappings deviate from Piranha/MariaDB defaults (MariaDB → host `3307`, app → host `8091`) because this dev machine already has other unrelated Docker workloads on `3306`/`8080`. Container-internal ports are untouched; this is a machine-local accommodation, not a spec requirement — revisit if CI/another dev machine expects the defaults.
- `Piranha` 12.0.0 (pinned per spec) carries known NU1901/NU1902 advisory warnings on every build/restore. Not addressed here — bumping the version would violate the pinned-version boundary; flagged for a conscious decision later.
- Added `public partial class Program { }` to `Program.cs` (one line) so `WebApplicationFactory<Program>` in the test project can see the top-level-statement `Program` class, which is `internal` by default. Standard ASP.NET Core testability pattern; no runtime behavior changed.
- Matrix Test Audit: rows 1–4 (hostname resolution ×3, seed idempotency) have full automated xUnit coverage run against the real MariaDB (`dotnet test`: 8/8 passed, independently re-verified). Row 5 (`docker compose up` resilience) has a static config-guard test (mariadb pinned to `10.11`, `depends_on: service_healthy`, `restart: unless-stopped`, volumes present) plus the prior manual verification (stopped mariadb, confirmed piranha-app auto-recovered with no duplicate Sites) — a live stop/recover cycle was deliberately not automated in the xUnit suite because it would tear down the shared dev DB/containers other tests depend on, and the actual recovery behavior is Docker's own `restart: unless-stopped` + healthcheck policy, not app code this story wrote.
- Docker Compose stack (`tbtruonghoc-mariadb-1`, `tbtruonghoc-piranha-app-1`) is left running/healthy. Site A: `http://tbtruonghoc.local:8091/`, Site B: `http://trongdoitam.local:8091/` (needs the hosts-file entries documented in README.md), Manager: `/manager`.
- Post-review patch round (9 findings, see Review Triage Log): moved MariaDB dev credentials out of tracked files into `.env`/`.env.example` (Compose variable substitution); added `.gitignore`; removed dead `blobstorage` config; de-duplicated the `Site` `InternalId` constants (`internal` on `SiteSeed` + `InternalsVisibleTo`); documented test-running in README; fixed `DockerComposeConfigTests`'s indentation-boundary bug; pinned the Dockerfile's build-stage SDK tag to `8.0.406`; switched the runtime container to a non-root `USER app`; added a `Hostnames`-survives-reseed assertion to `SiteSeedIdempotencyTests`. Independently re-verified: `dotnet build` (0 errors) and `dotnet test` (8/8 passed) both re-run after the patch.

## Spec Change Log

## Review Triage Log

- **[defer]** README.md:3 says "Piranha CMS 12.2.0" while every package in `TbTruongHoc.Web.csproj` is pinned `12.0.0`. Verified real, but the "12.2.0" wording traces back to this spec's own frozen Intent line and to ARCHITECTURE-SPINE.md's pre-existing "Piranha CMS 12.2.0" framing (written before this story), which already coexists there with a "12.0.0" package-pin table — this story only echoed an ambiguity that predates it. Cosmetic/doc-accuracy only, `low`.
- **[patch]** No `.gitignore` exists (only `.dockerignore`); build artifacts and `media/` are one `git add -A` away from source control once VCS is initialized. Verified: root listing has no `.gitignore`. `medium` (retroactive cleanup is painful once committed).
- **[patch]** `docker-compose.yml`/`appsettings*.json` hardcode MariaDB dev credentials (`piranha_dev_pw`, `piranha_root_dev_pw`) in tracked files. Verified present at `docker-compose.yml:8-9,31` and `appsettings.json:11`. Dev-only DB per architecture (Compose is explicitly dev-only, never prod), but visible immediately to any reader — `low`.
- **[patch]** `appsettings.json:12` `blobstorage` connection string is dead config (empty `AccountName`/`AccountKey`); `Program.cs:24` uses `UseFileStorage` (local disk), never blob storage. Verified unused via grep. `low`, cosmetic confusion only.
- **[patch]** `SiteSeed.cs:19-20`'s `TbTruongHocInternalId`/`TrongDoiTamInternalId` are `private` and independently redeclared in `HostnameResolutionTests.cs:30-31` and `SiteSeedIdempotencyTests.cs:71-72`. Verified via grep — three copies of the same two strings. `low`, real drift risk.
- **[reject]** Seed's read-then-write (`SiteSeed.cs:35,43,64`) is not concurrency-safe across simultaneous cold-start instances. Verified the race is structurally real, but ARCHITECTURE-SPINE.md's Deferred section commits to a single production host with no replica/load-balancer plan — the trigger condition (two instances racing an empty DB) is not a reachable deployment scenario today, and a correct fix (transaction/unique-constraint upsert) is more than a direct correction. Rejected as low-probability-plus-disproportionate-fix.
- **[false]** Claim: bootstrap-repurposing (`SiteSeed.cs:36-57`) is asymmetric — only the tbtruonghoc branch absorbs Piranha's "Default" placeholder, so a stray Default row could coexist with an already-seeded tbtruonghoc if "repurposing is skipped." Refuted: `EnsureSeededAsync` runs the tbtruonghoc block synchronously to completion (or throws, aborting the whole call) before `EnsureSiteAsync` for trongdoitam-net ever executes (`SiteSeed.cs:59`) — there is no code path where trongdoitam-net's branch runs while tbtruonghoc's repurposing is left half-done.
- **[reject]** No CI workflow despite `.github/` appearing in `.dockerignore`. Premature: no VCS exists yet in this repo (`baseline_commit: NO_VCS`) — there is nothing for CI to run against until git is initialized, which is outside this story's scope.
- **[patch]** README.md has no instructions for running `tests/TbTruongHoc.Web.Tests`, including that `mariadb` must already be up before `dotnet test` passes. Verified via full README read — no test-running section exists. `low`.
- **[patch]** `DockerComposeConfigTests.cs:112-114`'s `ExtractServiceBlock` sibling-boundary check only recognizes 2-space-indented keys; traced through `docker-compose.yml`'s actual layout and confirmed the top-level 0-indent `volumes:` key after `piranha-app:`'s block is not recognized as a boundary and gets silently appended into the returned block text (the loop only stops one line later, at `  mariadb-data:`). Does not corrupt any *current* assertion's pass/fail, but is a real latent brittleness in test-support code. `low`.
- **[reject]** No container-level `HEALTHCHECK` for `piranha-app` (only `mariadb` has one), so a hung-but-not-crashed process isn't auto-recovered. Verified absent in the Dockerfile. Real gap, but low likelihood at this project's current single-host, pre-content-launch scale, and a correct fix needs a new health endpoint plus an HTTP client in the runtime image — more than a direct correction. Rejected as disproportionate for now.
- **[patch]** `Dockerfile`'s runtime stage never sets a non-root `USER`, so the container runs as root by default. Verified absent. `medium` (standard hardening gap, self-hosted app will eventually be internet-facing); fix is a one-line `USER app`/`USER $APP_UID` addition.
- **[patch]** `Dockerfile:4`'s build stage uses the floating `mcr.microsoft.com/dotnet/sdk:8.0` tag while `global.json` pins exactly `8.0.406`, contradicting the Dockerfile's own comment that it "mirrors the repo-root global.json behavior." Verified. `low` (MCR's `8.0` tag only moves forward in practice, so real build failure is unlikely, but the claim-vs-code mismatch is real and the fix is a one-line exact pin).
- **[reject]** Missing null/empty checks on `builder.Configuration.GetConnectionString("piranha")` (`Program.cs:29`) and on the two `Sites:*:Hostnames` config values (`SiteSeed.cs:32-33`). Both values are present and working in the checked-in `appsettings.json`; hitting either gap requires a developer deliberately deleting a working config entry — unlikely in everyday use, and a correct fix adds guards/branches rather than a direct correction. Rejected per the low-finding criteria.
- **[false]** Claim: `editorconfig.json` referenced by `Program.cs:72` (`EditorConfig.FromFile("editorconfig.json")`) is not created anywhere in this diff. Refuted: `src/TbTruongHoc.Web/editorconfig.json` exists on disk (default `piranha.mvc` scaffold output) — it was simply out of scope for the hand-rolled diff, which excluded unmodified vendor scaffold files.
- **[reject]** No `EnableRetryOnFailure()` on the Pomelo MySql connection (`Program.cs:32-33`) to handle transient DB connection drops. Real EF Core best practice, but app and DB are co-located on one self-hosted box (not a cloud DB with independent network hops) per architecture — low real-world benefit here, and enabling retry policies is behavioral complexity, not a direct correction. Rejected as premature.
- **[false]** Claim: mariadb's healthcheck budget (20 retries × 5s + 30s start_period ≈ 130s) may be insufficient for `mariadb` to become ready. Not demonstrated — 130s is an ample, standard-generous margin for MariaDB cold start in a small dev/single-host container, and no evidence was given that this specific setup exceeds it.
- **[patch]** (pre-verified, verification-gap layer) `SiteSeedIdempotencyTests.cs:692-731` asserts only `Site` count/`InternalId`/`Id` across a re-seed, never `Hostnames` — so a regression making `EnsureSiteAsync`/the tbtruonghoc branch overwrite `Hostnames` on every restart (contradicting `SiteSeed.cs`'s own "does not clobber Manager-edited hostnames" doc-comment guarantee) would ship undetected. Filed evidence trusted per verification-gap pre-verification rule.
- **[defer]** (pre-verified, verification-gap layer, grouped with the edge-case-hunter's matching claim) `DockerComposeConfigTests` only pins compose YAML wording for matrix row 5, never exercises the live mariadb-unavailable retry/recovery behavior itself — already a documented, deliberate trade-off (see Implementation Notes) to avoid tearing down shared dev infra other tests depend on. Filed disposition (defer) accepted as-is.
- **[false]** (verification-gap "other findings") `Program.cs` calls `options.UseManager()`/`UseTinyMCE()` inside both the `AddPiranha` and `UsePiranha` delegates. Refuted: this is Piranha's own documented dual-registration pattern — the two calls target distinct extension points (service/DI registration during `AddPiranha` vs. middleware activation during `UsePiranha`), matching the stock `piranha.mvc` template output, not accidental duplication.

## Verification

**Commands:**
- `dotnet build` -- expected: builds cleanly under the pinned .NET 8 SDK
- `docker compose up -d && docker compose ps` -- expected: both `piranha-app` and `mariadb` report running/healthy
- App startup logs / `dotnet ef database update` -- expected: migrations apply cleanly against MariaDB 10.11 with no errors

**Manual checks (if no CLI):**
- Request the configured Site A dev hostname and confirm it renders Site A's default page, not Site B's; repeat for the Site B hostname.
