---
title: 'Fix SiteSettings validation bypass on Manager save path + clean up test-debris pages (Story 1.9)'
type: 'bugfix'
created: '2026-09-23'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '6a97afac7d045eb657b64ee7efb29822368f1b69'
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Piranha Manager's built-in UI always saves `SiteSettings` as a `DynamicSiteContent`, never the app's own `SiteSettings` POCO, but the `App.Hooks.SiteContent.RegisterOnBeforeSave` hook in `Program.cs` only runs its Zalo/Maps/GA4/SearchConsoleVerification checks when `model is SiteSettings` — so it always short-circuits on a real Manager save, letting an unsafe value (e.g. `javascript:...` in the Zalo/Maps URL fields) persist despite 11 green tests, because every existing test saves through the app's own strongly-typed path, never Manager's real one. Separately, three integration test files create real `Page` rows via a `CreatePublishedPageAsync` helper and never delete them, leaving ~105 debris rows in the shared dev database.

**Approach:** Extend the hook with a second branch for `DynamicSiteContent`: read each region's raw `StringField.Value` out of `Regions` (cast to `IDictionary<string, object>`) and run the same `SiteSettingsValidation` checks already used for the typed path, preserving the same cache-eviction and `ValidationException` behavior on rejection. Add test coverage that saves through a real `DynamicSiteContent`, built the same way Manager's `SiteService.SaveContent` builds one, to prove the fix and guard against regression. Add `finally`-block page cleanup to the three test files that create pages, and delete the existing debris rows from the shared dev database once.

## Boundaries & Constraints

**Always:**
- Reuse `SiteSettingsValidation.IsSafeAbsoluteUrl` / `IsValidGa4MeasurementId` / `IsValidSearchConsoleVerification` unchanged, validating the raw string pulled from the region — do not edit their regexes or bodies.
- Preserve the typed-path behavior exactly as-is: same cache-eviction key format (`$"SiteContent_{model.Id}"`), same `ValidationException` messages, same "empty/whitespace is always accepted" rule — only add a parallel branch for `DynamicSiteContent`.
- New tests must construct a real `DynamicSiteContent` (region dictionary keyed by the property names on `SiteSettings`: `ZaloUrl`, `MapsUrl`, `Ga4MeasurementId`, `SearchConsoleVerification`, each holding a `StringField`) and save it via `IApi.Sites.SaveContentAsync`, mirroring Manager's actual `SiteService.SaveContent` construction — not a POST through a Manager HTTP endpoint.
- Every test that creates a `Page`/`Post` deletes it in a `finally` block, mirroring the existing `SnapshotAsync`/`RestoreAsync` convention already used for settings fields.

**Never:**
- Don't build an HTTP/Manager-controller test harness (no auth setup exists for it) — construct `DynamicSiteContent` directly via `IApi`.
- Don't change the nested-ternary "one error at a time" reporting behavior — already triaged and rejected as out of scope in Story 1.6's review.
- Don't add new isolated-DB test infrastructure (respawn/transactions/per-test DB) — no such infra exists; use `finally`-block deletion instead.
- Don't rename or restructure any region/field on `SiteSettings.cs` — Manager already has live content saved under these exact keys.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Manager saves valid values | `DynamicSiteContent` with a valid Zalo/Maps URL and valid GA4 id | Save succeeds, no exception | N/A |
| Manager saves unsafe Zalo/Maps URL | `Regions["ZaloUrl"]` = `StringField{Value="javascript:alert(1)"}` | Save rejected | `ValidationException`, same message text as the typed path; cache evicted |
| Manager saves malformed GA4/Verification value | `Regions["Ga4MeasurementId"]` = `StringField{Value="G-X"}` (below length floor) | Save rejected | `ValidationException` |
| Manager saves with a field empty/absent | `StringField{Value=""}` or the region key missing from `Regions` | Save succeeds (treated as unset) | N/A |
| Test creates a `Page`/`Post` | any of the 3 affected test files | Page/Post is deleted in `finally` regardless of pass/fail | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Program.cs:137-172` -- `RegisterOnBeforeSave` hook; currently only branches on `model is SiteSettings`. Add a second `else if (model is DynamicSiteContent dyn)` branch here; reuse the existing `siteContentCache` eviction call and `ValidationException` messages verbatim.
- `src/TbTruongHoc.Web/Models/SiteSettingsValidation.cs` -- `IsSafeAbsoluteUrl`, `IsValidGa4MeasurementId`, `IsValidSearchConsoleVerification`: pure `string -> bool`, already usable against a raw region value. Do not modify.
- `src/TbTruongHoc.Web/Models/SiteSettings.cs` -- six single-field regions (`Phone`, `ZaloUrl`, `Address`, `MapsUrl`, `Ga4MeasurementId`, `SearchConsoleVerification`), each `[Region] public StringField <Name>`. Region `Id` in `DynamicSiteContent.Regions` equals the property name exactly.
- `Piranha.dll` (`Piranha.Services.ContentFactory`) -- confirms: a single-field region's value in `Regions` (an `ExpandoObject`, access via `(IDictionary<string, object>)dyn.Regions`) is the raw `StringField` instance itself, not further wrapped.
- `tests/TbTruongHoc.Web.Tests/SiteSettingsTests.cs` -- `SnapshotAsync`/`RestoreAsync` (~lines 494-509) is the cleanup convention to mirror; `SaveSettingsAsync` (~lines 480-486) is the existing typed-path save helper (do not remove, add tests alongside it); `CreatePublishedPageAsync` (~lines 465-478, private, called by 8 of 12 facts) creates pages never deleted — add `finally { await api.Pages.DeleteAsync(page.Id); }` at each call site.
- `tests/TbTruongHoc.Web.Tests/PerPageSeoFieldsTests.cs` -- 6 facts create `Page`/`Post`/`StandardArchive` rows inline with no cleanup at all; add matching `finally` deletion at each creation site.
- `tests/TbTruongHoc.Web.Tests/AnalyticsSearchConsoleTests.cs` -- own `CreatePublishedPageAsync` copy (~lines 211-224) used by 5 facts, never deleted; add the same cleanup.
- `IApi.Pages.DeleteAsync(Guid id)` / `DeleteAsync<T>(T model)` -- already exists on the Piranha `IApi`, ready to use, no new abstraction needed.
- Shared dev MariaDB (via `docker compose up -d mariadb`, per README) -- holds ~105 existing debris `Page` rows (slugs like `contact-test-a-...`, `maps-test-...`) predating this fix; delete these once as part of implementation.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Program.cs` -- add a `DynamicSiteContent` branch to the `RegisterOnBeforeSave` hook that extracts each region's `StringField.Value` and runs the same four validation checks -- closes the real bypass Manager's UI hits today.
- [x] `tests/TbTruongHoc.Web.Tests/SiteSettingsTests.cs` -- add facts that build a `DynamicSiteContent` (per the Boundaries pattern) with each of the four unsafe-value cases and assert `ValidationException`, plus one accepting-valid-values fact; add `finally` page deletion to existing `CreatePublishedPageAsync` call sites -- proves the fix and stops new debris.
- [x] `tests/TbTruongHoc.Web.Tests/PerPageSeoFieldsTests.cs` -- add `finally` page/post deletion at every creation site -- stops new debris from this suite.
- [x] `tests/TbTruongHoc.Web.Tests/AnalyticsSearchConsoleTests.cs` -- add `finally` page deletion at its `CreatePublishedPageAsync` call sites -- stops new debris from this suite.
- [x] One-time cleanup against the shared dev MariaDB -- delete the ~105 pre-existing debris `Page` rows (e.g. via a short throwaway script or REPL calling `IApi.Pages.DeleteAsync` for each matching slug) -- removes debris the fixed tests will no longer add to but did not create.

**Acceptance Criteria:**
- Given a value that fails validation submitted through a real `DynamicSiteContent` save (e.g. `javascript:alert(1)` in `ZaloUrl`, a truncated GA4 id), when save is attempted, then it is rejected with the same error behavior as today's typed path and no partial/poisoned save occurs.
- Given a valid value or an empty field submitted through a real `DynamicSiteContent` save, when save is attempted, then it succeeds exactly as today's typed path does.
- Given the fixed `SiteSettingsTests`/`PerPageSeoFieldsTests`/`AnalyticsSearchConsoleTests` suites, when `dotnet test` runs against the shared dev MariaDB, then no new `Page`/`Post` rows remain after the run.
- Given the shared dev database after this story lands, when its `Page` table is inspected, then the pre-existing ~105 debris rows are gone.

## Implementation Notes

- `Program.cs`'s `RegisterOnBeforeSave` hook now branches on the model shape: `model is SiteSettings` (the original typed path, behavior unchanged) and `model is DynamicSiteContent dyn`, which reads each region's raw `StringField.Value` via `(IDictionary<string, object>)dyn.Regions`. Both feed a single `RejectUnsafeValues` local function holding the four checks, the cache eviction and the `ValidationException` messages, so the two paths cannot drift. Region keys are referenced as `nameof(SiteSettings.ZaloUrl)` etc. so a rename can't silently desync them from the region ids.
- New `SiteSettingsTests` facts (6) exercise the real Manager-shaped path via `LoadDynamicContentAsync` (untyped `api.Sites.GetContentByIdAsync(siteId)`, the same call Manager's `SiteService.SaveContent` makes) plus `SetRegionValue`/`RemoveRegion` helpers, and each rejection fact asserts both the exact exception message and that the previously-saved safe value survived.
- **The cache eviction on the dynamic path is symmetry, not a live guard.** Decompiling `Piranha.Services.SiteService` shows `GetContentByIdAsync<T>` reads the `SiteContent_{id}` entry only when `T` is not `DynamicSiteContent`, and `OnLoadContentAsync` writes it under the same condition — so a dynamic load never populates or reads that entry and cannot poison it. Only the typed path can. The eviction is kept on both branches so a future editor can't remove it from the one that needs it; the code comment says so explicitly.
- An absent region behaves differently from an empty one and the test now pins that: a region removed from `Regions` is simply not part of the save payload, so Piranha leaves that field's stored value untouched. The matrix row only requires the save to be accepted rather than validated, which it is.
- Page/post cleanup added in `finally` blocks across `SiteSettingsTests`, `PerPageSeoFieldsTests` and `AnalyticsSearchConsoleTests`: creation results are hoisted into a nullable declared before the `try`, then deleted with `api.Pages.DeleteAsync(...)`/`api.Posts.DeleteAsync(...)` if non-null, so a mid-test failure still cleans up.
- **Pre-existing failures found and fixed (outside the original spec, approved by Phước mid-implementation):** `DockerComposeConfigTests` had 3 tests failing on `main` since commit `abb5a75` ("Simplify local dev"), which removed the `piranha-app` service and the Dockerfile from Compose but left the tests asserting that service existed. Rewrote them to guard what Compose actually still owns — mariadb's healthcheck, its `restart: unless-stopped` policy, and the `mariadb-data` volume — and dropped the `depends_on`/media-mount assertions that no longer have a subject. Story 1.6's "46/46 passed" predates `abb5a75`, so this had gone unnoticed.
- **A poisoned value really was in the dev DB.** The first full test run in this session failed 8 tests inside `RestoreAsync`, because the snapshot it captured held an invalid `ZaloUrl` — the live exploit's residue, saved through Manager while the bypass was open. Restoring it now correctly throws. Subsequent runs are clean: the failures overwrote the poison with valid values. This is incidental confirmation that the reported bug was real and had actually written to the database.
- Verified: `dotnet build TbTruongHoc.sln` — 0 errors (same pre-existing Piranha 12.0.0 NuGet advisory warnings prior stories noted). `dotnet test TbTruongHoc.sln` against real MariaDB — **52/52 passed** (46 pre-existing, of which 3 were repaired above, plus 6 new). Page/post row counts held steady at 105/15 across four consecutive full runs, confirming the suites no longer leave debris (each run previously added ~25 pages).
- **Debris cleanup done, with one deliberate exception.** Deleted 104 GUID-suffixed debris pages and 15 debris posts; kept `test-page` (created 2026-09-23 05:04, no GUID suffix — almost certainly Phước's own manual Manager page, not test output; Phước confirmed keeping it). Posts had to be deleted before pages: deleting a page cascades into `Piranha_Categories`, but `Piranha_Posts.CategoryId` references that table with `RESTRICT`, so a page-first delete fails while any post still points at one of its categories. Also cleared leftover junk `SiteSettings` values — `Phone`/`Address` from a crashed test run, and the `xxxxxxxx`/`yyyyyyyyyy` placeholder `SearchConsoleVerification` values. Final state: 1 page, 0 posts, all settings fields null; a further full `dotnet test` run on that clean database still passed 52/52, so the suites don't depend on leftover data.
- Manual Manager click-through with real admin credentials was not performed this session (same constraint Stories 1.5/1.6 noted). The tests call the real save path and real HTTP-rendered pages, but not through a logged-in Manager UI — Phước should confirm once that entering `javascript:alert(1)` into Zalo URL in Manager is now rejected.

## Spec Change Log

## Review Triage Log

Layers run: `blind-hunter`, `verification-gap`. The `edge-case-hunter` launch was interrupted by the user mid-run and not restarted — noted so a later pass knows that lens never reported.

- **[medium, patch]** (blind-hunter) No rejection fact asserted the `ValidationException` message, although the I/O matrix requires "same message text as the typed path" — all four used bare `Assert.ThrowsAsync` and discarded the exception, so the two branches' message literals could drift apart silently. Verified at `SiteSettingsTests.cs` lines 312/340/372/401. Fixed: each of the four facts now captures the exception and asserts the exact message.
- **[medium, patch]** (blind-hunter) The two hook branches duplicated all four message literals, so the drift above had a live source. Verified. Fixed by extracting a `RejectUnsafeValues(zalo, maps, ga4, verification)` local function that both branches call — one copy of the checks, the eviction and the messages, with each branch responsible only for reading its own values. Behavior for the typed path is unchanged.
- **[medium, patch]** (blind-hunter) The `SearchConsoleVerification` error message says "only letters, digits, '-' or '_'", but `SearchConsoleVerificationPattern` is `^[A-Za-z0-9_.=+-]+$` — Story 1.6's patch round relaxed the regex and left the message stale, and this change copied it into a second place. Real user harm: real Search Console tokens contain `.` and `=`, so the message tells a user their valid token is invalid. Fixed: message now lists `'.', '-', '_', '=' or '+'`.
- **[medium, patch]** (blind-hunter) `SnapshotAsync`/`RestoreAsync` covered only 4 of the 6 fields, so the two new accepting facts each needed a second restore call in `finally`; if the first throws, the second never runs and GA4/verification stay poisoned for every later test. Verified at `SiteSettingsTests.cs:809-823`. Fixed: snapshot/restore now cover all six fields via a `SiteSettingsSnapshot` record, and every double-restore is gone.
- **[medium, patch]** (blind-hunter + verification-gap) Rows were still created outside the `try` in four places, so the new cleanup does not cover a failure partway through creation: `AnalyticsSearchConsoleTests`'s sitemap fact (page A leaks if page B's creation throws) and all three `PerPageSeoFieldsTests` post facts (the blog archive page leaks if the post save throws). Verified by reading each site. Fixed: creations hoisted into the `try` behind nullables, with a shared `CreatePublishedBlogAsync`/`DeleteBlogAndPostAsync` pair replacing the three copy-pasted blog blocks.
- **[low, patch]** (verification-gap) Inside every `finally`, the page deletes ran before `RestoreAsync`, so a delete failure skips the settings restore and leaves that test's mutated values for the next test — the same cross-test pollution this story cleaned up. Verified at `SiteSettingsTests.cs:118-128` and `AnalyticsSearchConsoleTests.cs:93-105`. Fixed: restore now runs first in all ten `finally` blocks.
- **[low, patch]** (blind-hunter) `RemoveRegion` discarded the `bool` from `Remove`, so the matrix's "region key missing" case would silently degrade into "key that was never there" if the region id ever changed. Verified. Fixed: `RemoveRegion` now asserts the key was present.
- **[low, patch]** (blind-hunter) The "empty/whitespace is always accepted" boundary rule was only covered for `""` and an absent key, never whitespace, even though `IsNullOrWhiteSpace` is what implements it. Verified. Fixed: the empty/missing fact now also pushes `"   "` through `MapsUrl`.
- **[low, patch]** (blind-hunter) `Assert.Contains("mariadb-data:", compose)` in the rewritten compose test is vacuously satisfied by the mount line asserted two lines above, so the top-level `volumes:` declaration was not actually guarded — and an undeclared named volume silently becomes an anonymous one that `docker compose down` discards with the database. Verified. Fixed with a regex anchored on the top-level `volumes:` key.
- **[low, patch]** (blind-hunter) `using System.Collections.Generic;` added to `Program.cs` is redundant under `ImplicitUsings=enable`, and `Program.cs` otherwise lists only non-implicit usings. Verified in both csproj files. Removed. The same using in `SiteSettingsTests.cs` was left: that file already lists `System`, `System.Linq` and other implicit usings explicitly, so removing just one would break its own convention.
- **[low, patch]** (blind-hunter) The dynamic branch's comment claimed "same cache-eviction rationale as the typed branch", and the Implementation Notes claimed the rejection facts prove the eviction works. Both are wrong — see the row below. Fixed: the comment now states accurately that only the typed path can poison that cache entry and that the dynamic eviction is there to keep the two paths from diverging; the Implementation Notes claim was corrected.
- **[medium, defer]** (verification-gap) The dynamic branch's cache eviction is unobservable and its stated rationale was wrong. Confirmed by decompiling `Piranha.Services.SiteService`: `GetContentByIdAsync<T>` reads the `SiteContent_{id}` entry only when `!typeof(DynamicSiteContent).IsAssignableFrom(typeof(T))` (line 203) and `OnLoadContentAsync` writes it only when `!(model is DynamicSiteContent)` (line 457) — so a dynamic load never touches that entry and the mutated object is never the cached one. Deleting the eviction line would not fail any test. Kept deliberately for symmetry between the two branches and corrected in the comment; deferred rather than removed, because removing it is a judgment call about a security hook that deserves its own look.
- **[medium, defer]** (blind-hunter + verification-gap) Nothing automatically guards the "no debris rows left behind" acceptance criterion — it was verified by hand across six runs. The suggested fix (row counts captured and asserted in the shared collection fixture) is new test infrastructure with its own flakiness risk, not a direct correction, so it routes to defer rather than patch.
- **[medium, defer]** (verification-gap) The repo has no CI, so nothing runs this suite on push. Verified: no `.github/workflows`, no pipeline file. The diff itself is the evidence this matters — three compose tests were red on `main` from `abb5a75` through all of Story 1.6 before this story noticed. Deferred: CI needs a MariaDB service and these tests assume a shared seeded database, so it is its own piece of work.
- **[medium, defer]** (blind-hunter) No test drives Manager's actual HTTP save endpoint, and no manual Manager click-through was performed, so nothing observes what Manager renders when the hook throws — a field error, or a 500 with the edit form lost. The reconstruction the tests use was independently confirmed against decompiled `Piranha.Manager.Services.SiteService.SaveContent` by two separate passes, so the fix itself is sound; what is unverified is the user-facing half. Deferred: needs a Manager auth harness that does not exist, plus one manual check.
- **[low, rejected]** (blind-hunter) `Raw()` returns null when a region's value is present but is not a `StringField`, so an unrecognized region shape would fail open. Verified unreachable today: all six `SiteSettings` regions are single-field `StringField` regions, and both Piranha's `ContentFactory` and Manager's `SiteService.SaveContent` store a single-field region's value as the bare `StringField`. Same for the missing `else` arm — `SiteSettings` is the only site type. Rejected: the fix adds branches and logging to guard a state that was never shown to be reachable.
- **[low, rejected]** (blind-hunter) The dynamic branch validates any site content type whose regions happen to be named `ZaloUrl`/`MapsUrl`/`Ga4MeasurementId`/`SearchConsoleVerification`, rather than checking `dyn.TypeId`. Verified true, but `SiteSettings` is the only `SiteType` in the app, and a second one would need its own validation anyway. Rejected: adds a branch for a case that does not exist.
- **[low, rejected]** (blind-hunter) All six new facts run against Site A only, while the pre-existing suite tests site A/site B pairs. Verified true. The new branch has no cross-site logic — the cache key is already per-site (`SiteContent_{id}`) and validation reads only the submitted payload. Rejected: the fix is a set of new tests, not a correction.
- **[low, rejected]** (blind-hunter) The rewritten compose tests no longer assert the `3307:3306` port mapping that the README's connection string depends on. Verified true, but that mapping was never asserted before this change either, so this is a new test rather than a correction to the diff. Rejected.
- **[false]** (blind-hunter) "`Spec Change Log` is empty even though a mid-implementation scope change happened." The template reserves that section for step-04 loopback entries, and no loopback occurred; the compose repair is recorded in Implementation Notes, where approved scope changes belong. No defect.
- **[false]** (blind-hunter) "`sprint-status.yaml` says `in-progress` while the spec says `in-review`." The sprint file is advanced by the workflow's own status-sync steps, and step-05 is where it moves past `in-progress`; it was not stale at the time the diff was taken. No defect in the change.

## Design Notes

Extracting a raw value from `DynamicSiteContent` inside the hook (the hook's `model` parameter is typed `SiteContentBase`, not `dynamic`, so a cast is required before dictionary access):

```csharp
else if (model is DynamicSiteContent dyn)
{
    var regions = (IDictionary<string, object>)dyn.Regions;
    string? Raw(string key) =>
        regions.TryGetValue(key, out var r) && r is StringField f ? f.Value : null;

    var zaloUnsafe = !string.IsNullOrWhiteSpace(Raw("ZaloUrl")) && !SiteSettingsValidation.IsSafeAbsoluteUrl(Raw("ZaloUrl")!);
    // ...MapsUrl, Ga4MeasurementId, SearchConsoleVerification follow the same shape as the existing typed branch
}
```

Constructing the same shape in a test, mirroring how Manager's `SiteService.SaveContent` builds it (no Manager package dependency needed — only public `IApi`/Piranha core types):

```csharp
var dyn = await api.Sites.CreateContentAsync<DynamicSiteContent>(siteType.Id);
dyn.Id = siteId;
((IDictionary<string, object>)dyn.Regions)["ZaloUrl"] = new StringField { Value = "javascript:alert(1)" };
await Assert.ThrowsAsync<ValidationException>(() => api.Sites.SaveContentAsync(siteId, dyn));
```

## Verification

**Commands:**
- `dotnet build TbTruongHoc.sln` -- expected: 0 errors.
- `docker compose up -d mariadb` then `dotnet test TbTruongHoc.sln` (real MariaDB, per README) -- expected: all tests pass, including the new `DynamicSiteContent` bypass-regression facts; no new debris rows left behind (spot-check `Page` row count before/after).

**Manual checks (if no CLI):**
- After the one-time debris cleanup, confirm in Piranha Manager's page list that the `contact-test-a-...`/`maps-test-...` slugs are gone and no real content pages were affected.
