---
title: 'Per-site contact info (Phone/Zalo/Maps) for Site A & Site B'
type: 'feature'
created: '2026-09-20'
status: 'done'
route: 'dispatch'
baseline_commit: 'NO_VCS'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 1.3 requires every published page on Site A and Site B to show click-to-call, a Zalo entry point, and a link to Google Maps using that site's own contact details — one shared `SiteSettings` model that no view may hardcode or cross-mix between sites. No such settings model exists yet; `_Layout.cshtml` still ships Piranha's unmodified scaffold nav with no contact markup at all.

**Approach:** Add a `SiteSettings` SiteType (Piranha `SiteContent<SiteSettings>`, auto-discovered by the existing `ContentTypeBuilder`) holding Phone/ZaloUrl/Address/MapsUrl fields, one instance per `Site`, idempotently seeded alongside `SiteSeed`. Render it once from a new `_ContactBlock.cshtml` partial included in `_Layout.cshtml` (the single shared render point for every Page/Post/Archive), so it appears on every published page without touching per-type views. Keep the markup minimal/unstyled — the polished nav-chip (Site A) and sticky-contact-bar (Site B) visual designs belong to Epic 6 Story 6.1 and Epic 2 Story 2.1, which will consume these same `SiteSettings` values later.

## Boundaries & Constraints

**Always:**
- One `SiteSettings` instance per Piranha `Site`, read via `api.Sites` — never hardcode phone/Zalo/Maps values in any view.
- Zalo field stores the full `https://zalo.me/<number>` link as entered by the editor (zalo.me itself handles app-deep-link vs. web fallback) — do not build a separate app-scheme/web-fallback pair.
- Maps field stores a link-out URL to Google Maps (per UX spec: tapping opens Google Maps, not an inline embed) — do not add an iframe embed.
- HTML-encode all rendered values (mirror `_MetaTags.cshtml`'s hand-rolled-partial convention) — Story 1.2 found Piranha's own built-in helper unsafe here.
- Follow `SiteSeed.EnsureSeededAsync`'s idempotent get-or-create pattern for seeding default `SiteSettings` content — never overwrite Manager-edited values on restart.

**Never:**
- Never build the final styled nav-chip / sticky-contact-bar components (Epic 6 Story 6.1 / Epic 2 Story 2.1's scope) — this story only proves the data flows correctly to a minimal, functional render.
- Never touch `FormSubmission`, GA4/Search Console fields, or any Story 1.4/1.5/1.6 concern.
- Never add a custom Piranha Manager module — SiteType content gets Manager's built-in per-site settings edit UI automatically, same as PageType.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Editor fills Site A's SiteSettings | Phone/ZaloUrl/Address/MapsUrl set on Site A only | Every page on Site A renders Site A's values; Site B's page renders Site B's own (or empty if unset) — never Site A's | N/A |
| Visitor taps phone element | Rendered page, Phone set | Real `tel:<digits>` link (non-digit chars stripped for href, display text unchanged) | N/A |
| Visitor taps Zalo element | Rendered page, ZaloUrl set | `<a href="{ZaloUrl}">` opens directly, no rewriting | N/A |
| Visitor views Maps element | Rendered page, MapsUrl set | Link to that site's own Maps URL | N/A |
| Field left empty by editor | Phone/ZaloUrl/Address/MapsUrl unset | Element for that field is omitted from render, not a broken/empty link | N/A |
| Value contains HTML-special characters | e.g. Address with `&`, `"` | Rendered encoded, no attribute/markup breakage | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Models/StandardPage.cs` -- pattern to mirror: `[PageType]`-attributed class picked up by `ContentTypeBuilder`; use `[SiteType(Title=...)]` + `SiteContent<SiteSettings>` instead
- `src/TbTruongHoc.Web/Program.cs:66-69` -- `ContentTypeBuilder(...).AddAssembly(...).Build()` auto-discovers `[SiteType]` classes too — no extra registration call needed
- `src/TbTruongHoc.Web/Program.cs:78-80` -- existing `SiteSeed.EnsureSeededAsync` call site; add the new SiteSettings seed call immediately after
- `src/TbTruongHoc.Web/Data/SiteSeed.cs:62-72` (`EnsureSiteAsync`) -- idempotent get-or-create pattern to mirror for seeding default `SiteSettings` content per site (check `api.Sites.GetContentByIdAsync<SiteSettings>(siteId)` returns null before `CreateContentAsync`/`SaveContentAsync`; confirm exact `ISiteService` overloads via IDE — not yet used anywhere in this repo)
- `src/TbTruongHoc.Web/Views/Shared/_Layout.cshtml:39` -- insert `@await Html.PartialAsync("_ContactBlock")` right after `@RenderBody()`; this is the one shared render point for every Page/Post/Archive view
- `src/TbTruongHoc.Web/Views/Shared/_MetaTags.cshtml` -- convention to mirror: hand-rolled Razor partial with `@`-expressions (HTML-encode by default) instead of a Piranha built-in helper
- `tests/TbTruongHoc.Web.Tests/PiranhaWebApplicationFactory.cs`, `PiranhaAppCollection.cs` -- reuse as-is; add `[Collection(PiranhaAppCollection.Name)]` to the new test class
- `tests/TbTruongHoc.Web.Tests/SiteSeedIdempotencyTests.cs` -- pattern to mirror for a SiteSettings seed-idempotency test
- `tests/TbTruongHoc.Web.Tests/PerPageSeoFieldsTests.cs` -- pattern to mirror for an end-to-end HTTP-rendered assertion test; reuse its `SortOrder = 1` convention for any test page created

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Models/SiteSettings.cs` -- new `[SiteType]` class extending `SiteContent<SiteSettings>` with Phone/ZaloUrl/Address/MapsUrl fields -- the shared per-site settings model
- [x] `src/TbTruongHoc.Web/Data/SiteSeed.cs` (or a co-located `SiteSettingsSeed.cs`) -- idempotent get-or-create seed of an empty `SiteSettings` content instance for both sites -- so Manager's edit UI has something to open on first use
- [x] `src/TbTruongHoc.Web/Program.cs` -- wire the new seed call after line 80 -- keeps startup seeding centralized
- [x] `src/TbTruongHoc.Web/Views/Shared/_ContactBlock.cshtml` -- new partial: reads the current site's `SiteSettings`, renders `tel:` link, Zalo link, Maps link, Address text, each omitted if unset, all HTML-encoded -- the one render point that satisfies the AC
- [x] `src/TbTruongHoc.Web/Views/Shared/_Layout.cshtml` -- include the partial once after `@RenderBody()` -- makes it appear on every published page without touching Page/Post/Archive views
- [x] `tests/TbTruongHoc.Web.Tests/SiteSettingsTests.cs` -- new test class covering the I/O Matrix rows above (per-site correctness, tel: href, Zalo href, Maps href, empty-field omission, HTML-encoding) -- proves the AC end-to-end, mirroring `PerPageSeoFieldsTests.cs`'s real-MariaDB/real-HTTP pattern

**Acceptance Criteria:**
- Given SiteSettings filled in for both Site A and Site B, when any published page on either site renders, then it shows only that site's own phone/Zalo/Maps values — never the other site's, never hardcoded.
- Given a page with Phone set, when a visitor taps the phone element, then it is a real `tel:` link.
- Given a page with ZaloUrl set, when a visitor taps the Zalo element, then it opens that URL directly.
- Given a page with MapsUrl set, when a visitor views the Maps element, then it links to that site's own location.

## Implementation Notes

- All five production/code tasks implemented as specified: `SiteSettings.cs` (`[SiteType]` + `[Region]`-attributed `StringField`s, mirroring `StandardPage.cs`), `SiteSettingsSeed.cs` (idempotent per-site get-or-create, mirroring `SiteSeed.EnsureSeededAsync`), the `Program.cs` wiring after the existing `SiteSeed` call, `_ContactBlock.cshtml` (hand-rolled, HTML-encoded by default, tel: href built via `Regex.Replace(phone, "[^0-9]", "")` with display text left untouched, each element and the whole block omitted when unset), and its inclusion in `_Layout.cshtml` right after `@RenderBody()`. `dotnet build TbTruongHoc.sln` succeeds with 0 errors.
- Confirmed via reflection against the installed Piranha 12.0.0 DLLs (this repo pins 12.0.0, not the 12.2.0 the epic context prose states) that `ContentTypeBuilder` auto-discovers `[SiteType]` classes, and the exact signatures of `ISiteService.GetContentByIdAsync<T>`/`CreateContentAsync<T>`/`SaveContentAsync<T>` and `ISiteHelper.GetContentAsync<T>()` used — this resolves the Code Map's own noted uncertainty about confirming `ISiteService`'s overloads.
- `SiteSettingsTests.cs` (7 facts covering every I/O Matrix row plus seed idempotency) initially could not be executed because Docker Desktop's backend service failed to start in the sandbox; the user started Docker Desktop and `docker compose up -d mariadb` then worked.
- First real run against MariaDB (`dotnet test`) found a genuine production bug, not caught by inspection: `SiteSettingsSeed.EnsureSeededAsync` threw `System.MissingFieldException("Can't save content for a site that doesn't have a Site Type Id.")` on every startup, which crashed the shared `PiranhaWebApplicationFactory` and cascaded to 18/22 tests across all 5 test classes, not only the 6 new ones. Root cause (confirmed by decompiling the installed Piranha 12.0.0 DLLs with ilspycmd): `Piranha.Repositories.SiteRepository.SaveContent<T>` requires `Site.SiteTypeId` to be set before content can be saved for that site, and `SiteSeed.cs` (Story 1.1, untouched) never set it since it predates this model. `ContentTypeBuilder.GetSiteType` confirmed the registered `SiteType.Id` for `SiteSettings` defaults to `typeof(SiteSettings).Name` (no explicit `Id` given in the `[SiteType]` attribute).
- Fix (`SiteSettingsSeed.cs` only, no change to `SiteSeed.cs`/`SiteSettings.cs`): before creating/saving `SiteSettings` content for a site, set `site.SiteTypeId = typeof(SiteSettings).Name` and persist it via `api.Sites.SaveAsync(site)` if not already set. The existing `GetContentByIdAsync<SiteSettings>(site.Id) == null` idempotency check remains the correct "not yet seeded" signal both before and after `SiteTypeId` is assigned.
- Also fixed one test-assertion bug (not a product bug) in `Phone_Renders_As_Real_Tel_Link_With_Digits_Only_Href_And_Unchanged_Display_Text`: it compared against the raw, un-encoded phone text, but Razor's `HtmlEncoder.Default` (used by the `@`-expressions in `_ContactBlock.cshtml`) also encodes `+`, unlike `WebUtility.HtmlEncode` used elsewhere in the suite. The fixture phone (`+84 (090) 123-...`) tripped this; the assertion now encodes its expected value through the same encoder Razor actually uses.
- Final verification, run twice independently (once by the implementing subagent, once directly in this session) against real MariaDB via `docker compose up -d mariadb` + `dotnet test TbTruongHoc.sln`: **22/22 passed, 0 failed** — `DockerComposeConfigTests` 4/4, `HostnameResolutionTests` 3/3, `PerPageSeoFieldsTests` 7/7, `SiteSeedIdempotencyTests` 1/1, `SiteSettingsTests` 7/7. All 4 pre-existing test classes confirmed still passing — the fix did not regress anything.
- After the review's patch round, re-ran `dotnet test TbTruongHoc.sln` independently: **23/23 passed** (adds the new mixed-state test).
- Performed the spec's second manual Verification command: `docker compose up -d --build piranha-app`. App started cleanly against the real (non-test) dev database with no startup errors — confirms `SiteSettingsSeed` works outside the throwaway test DB too. `/manager/login` returns HTTP 200. Could not complete a full browser-driven login (no Manager credentials available in this session and no browser tool), so instead verified directly in the dev database: `Piranha_Sites` shows both `tbtruonghoc` and `trongdoitam-net` with `SiteTypeId = 'SiteSettings'` correctly set; `Piranha_SiteFields` has exactly the 4 expected field rows (Phone/ZaloUrl/Address/MapsUrl) scoped to each site's own `SiteId`; `Piranha_SiteTypes` has the `SiteSettings` CLRType registered. This is the same data shape Manager's built-in per-site settings UI reads for any `[SiteType]` — strong indirect evidence the edit UI will show and save these fields, though an actual logged-in click-through was not performed. Stopped the `piranha-app` container afterward (was only started for this check).

## Spec Change Log

## Review Triage Log

- **[patch]** `_ContactBlock.cshtml`: when Phone is non-blank but contains no digits (e.g. an editor types "N/A"), the digit-only `telDigits` is empty but the outer check only tests `phone` non-blank, so it renders `href="tel:"` — a dead link — instead of omitting the phone element. Verified: the inner `@if` only guards on `phone`, never on `telDigits`. Low: rare editor-input edge case, trivial fix.
- **[patch]** `_ContactBlock.cshtml`: `ZaloUrl`/`MapsUrl` render straight into `href="@url"` with no scheme check. HTML-encoding protects the surrounding markup but not the URI scheme — a Manager-entered `javascript:`/`data:` value would still execute on click (stored self-XSS via an editor account), and a relative/malformed value would render a broken link. Verified: no `Uri`/scheme validation exists anywhere in the partial. Medium: real stored-XSS-adjacent risk even though it requires Manager edit access, and the fix is a trivial scheme allow-list.
- **[patch]** `_ContactBlock.cshtml`: `Regex.Replace(phone, "[^0-9]", "")` strips a leading `+` along with all other non-digits, so a Phone entered in international format (e.g. `+84 90 123 4567`) produces `href="tel:840901234567"` — missing the `+` that signals international dialing, which can dial incorrectly on some devices. Verified by reading the regex and the AC's own `Phone_Renders_As_Real_Tel_Link_...` test fixture, which uses a `+84` example. Medium: breaks the core click-to-call AC for any internationally-formatted number; fix is a one-line adjustment to preserve a leading `+`.
- **[patch]** `SiteSettingsTests.cs`: `Phone_Renders_As_Real_Tel_Link_...` builds its expected HTML via `HtmlEncoder.Default` (matching Razor's actual encoder) while `Address_With_Html_Special_Characters_Is_Encoded_Without_Breaking_Markup` builds its expected HTML via `WebUtility.HtmlEncode` instead — inconsistent within the same test class for the same rendering path. Verified: the verification-gap layer empirically confirmed the two encoders agree on `&`/`<`/`>`/`"` (the Address test's characters) but diverge on `+`, so there is no current test failure, only a latent risk if a future Address fixture includes a `+`. Low: no live bug, but worth fixing for consistency/future-proofing; trivial encoder swap.
- **[patch]** `SiteSettings.cs`: the four `[Region]` fields (`Phone`/`ZaloUrl`/`Address`/`MapsUrl`) carry only a `Title`, no `Description`, even though `SiteSettingsSeed` deliberately seeds them empty for an editor to fill in blind — there's nothing in Manager telling the editor the expected format (local vs. international phone, full Zalo link vs. handle, Maps share link). Verified `Piranha.Extend.RegionAttribute` has a `Description` property (confirmed via reflection against the installed Piranha 12.0.0 DLL), so this is directly fixable. Low: real editor-facing gap, trivial fix.
- **[patch]** `SiteSettingsTests.cs`: only the "all four fields empty" state is tested (`Unset_Fields_Are_Omitted_Not_Rendered_As_Broken_Links`); there is no test for the more realistic mixed state (e.g. Phone+Address set, Zalo/Maps blank) that would catch a future regression where the `@if` blocks stop being independently gated. Verified: no such test exists in the file. Low: real coverage gap for a plausible future regression; trivial to add.
- **[patch]** `_ContactBlock.cshtml`: Zalo/Maps links have no `target="_blank" rel="noopener noreferrer"`, so tapping either navigates the visitor away from the page they were browsing (losing their place mid-lead-capture) in the same tab. Verified: no `target`/`rel` attributes present. Low: real but minor UX friction; trivial fix, and not excluded by the spec's "minimal/unstyled" framing (that governs visual polish, not link-opening behavior).
- **[false]** `SiteSettingsSeed.cs`: edge-case-hunter flagged that `site.SiteTypeId != SiteSettingsTypeId` would silently overwrite an existing, unrelated `SiteTypeId`. Refuted: `grep` for `[SiteType]` across `src/` shows `SiteSettings` is the only `[SiteType]`-attributed class in the entire app, and nothing else ever sets `Site.SiteTypeId` — the overwrite branch is currently unreachable.
- **[false]** `_ContactBlock.cshtml:20`: edge-case-hunter flagged `WebApp.Site` could be null on a non-site route, causing a `NullReferenceException`. Refuted: `_Layout.cshtml`'s own pre-existing nav markup already dereferences `WebApp.Site.Sitemap` unconditionally on every render this partial could ever reach; the one view that opts out of `_Layout` (`Views/Setup/Index.cshtml`) sets `Layout = null`. Not a new risk introduced by this diff.
- **[false]** `SiteSettingsTests.cs`/`SiteSettings.cs`: blind-hunter flagged raw Zalo/Maps URLs rendered as visible link text as an accessibility/UX gap. Refuted as a defect against this story: the frozen Intent explicitly scopes this story to "keep the markup minimal/unstyled — the polished nav-chip (Site A) and sticky-contact-bar (Site B) visual designs belong to Epic 6 Story 6.1 and Epic 2 Story 2.1," and `_ContactBlock.cshtml`'s own doc comment restates this. Working as intended.
- **[false]** `SiteSettings.cs`/`SiteSettingsTests.cs`: blind-hunter flagged the Manager-UI-edits-this-automatically claim in `SiteSettings.cs`'s doc comment as untested. Refuted: the spec's own `## Verification` section already scopes this to a manual check (`docker compose up -d --build piranha-app` + "manually confirm Site A/B's Manager 'Settings' edit UI opens and saves the four fields"), not automated coverage — performed separately, see Implementation Notes.
- All seven `[patch]` findings above were fixed by re-engaging the implementing subagent (dead tel: link, URL scheme validation, leading-`+` preservation, encoder consistency, Region field `Description`s, mixed-state test, `target`/`rel` on external links). Re-verified independently in this session: `dotnet test TbTruongHoc.sln` → **23/23 passed, 0 failed** (the 22 from the first pass plus the new mixed-state test).
- **[false]** `SiteSettingsSeed.cs`: blind-hunter flagged that the idempotent-seed logic is built on decompiled, undocumented Piranha internals with no guard against a future Piranha upgrade changing that behavior. Refuted as a current defect: confirmed correct against the pinned Piranha 12.0.0 (the only version this repo builds against) by decompilation and by a full passing real-MariaDB test run (22/22); the risk is speculative future-upgrade fragility, already documented in the code's own comments, not a bad outcome this diff currently produces.
- **[false]** `Program.cs`: blind-hunter flagged the diff showing the entire file rewritten instead of a small hunk. Refuted: this repo has no VCS, so `{diff_file}` was hand-assembled by diffing the current file against a manually retyped "before" snapshot for review purposes; the line-ending mismatch that produced a full-file diff is an artifact of that reconstruction, not a change made to the actual file (confirmed by direct inspection: only the `SiteSettingsSeed` call and its comment were added).
- **[patch]** `_ContactBlock.cshtml`: found during human walkthrough (Phước, 2026-09-20), not the automated review loop. Entering `javascript:alert(1)` into Phone strips down to a single incidental digit (`1`, from `(1)`) and rendered `href="tel:1"` — not an XSS (the href is always rebuilt from digits-only, never the raw string, and the display text is HTML-encoded), but a nonsensical tel: link from garbage input that the existing "empty digits = unset" check didn't catch. Fix: require `>= 7` digits (`MinPhoneDigits`) before treating Phone as set, otherwise omit it like the empty case. Also hardened two pre-existing tests (`Each_Site_Renders_Only_Its_Own_Contact_Values`, `Mixed_State_Renders_Only_The_Fields_That_Are_Set`) whose random Guid-suffix phone values could occasionally fall under the new 7-digit floor, plus added `Phone_With_Only_A_Stray_Digit_In_Garbage_Text_Is_Treated_As_Unset`. Re-verified: `dotnet test TbTruongHoc.sln` → **24/24 passed, 0 failed**.
- **[patch]** Requested by Phước during walkthrough (2026-09-20): defense-in-depth for Zalo/Maps - reject an unsafe (non-http/https) scheme at **save** time too, not only at render time, so a future view that reads `SiteSettings.ZaloUrl`/`MapsUrl` directly (bypassing `_ContactBlock.cshtml`'s `IsSafeAbsoluteUrl` filter) can't reintroduce the scheme-injection risk. Extracted the check into a shared `SiteSettingsValidation.IsSafeAbsoluteUrl` (used by both `_ContactBlock.cshtml` and the new hook) and registered `App.Hooks.SiteContent.RegisterOnBeforeSave` in `Program.cs` (must be registered on `SiteContentBase`, not `SiteSettings` - confirmed by decompiling `SiteService.SaveContentAsync<T>`, which invokes the hook as `App.Hooks.OnBeforeSave((SiteContentBase)model)`, so a hook registered for `SiteSettings` specifically would never fire). Throwing `ValidationException` before `_repo.SaveContent` runs means nothing persists, and `SiteApiController.SaveContent` already catches `ValidationException` and surfaces `ex.Message` as a Manager error toast (confirmed by decompiling `Piranha.Manager.dll`) - no new UI work needed.
  - **Follow-up bug found while testing the fix, same session:** `SiteService.GetContentByIdAsync<T>` returns the live cached instance from `ICache` (not a fresh copy), and both Piranha Manager's own `SiteService.SaveContent` and this repo's `SaveSettingsAsync` test helper mutate that instance in place *before* calling `SaveContentAsync` - so a rejected save (hook throws) still left the unsafe value sitting in the in-memory cache, even though the database row was never touched. Any reader hitting the cache during that window (including `_ContactBlock.cshtml` itself, though its own `IsSafeAbsoluteUrl` filter still caught it) would have seen the rejected value until the next successful save or process restart - which would have silently defeated the save-time block's whole purpose. Fix: the hook now evicts the `SiteContent_{id}` cache key (via `Piranha.Cache.ICache`, resolved from `app.Services`) before throwing, so the next read falls back to the still-valid DB row. Added `Save_Rejects_Unsafe_Zalo_Url_Scheme_And_Does_Not_Persist` and `Save_Rejects_Unsafe_Maps_Url_Scheme_And_Does_Not_Persist` (each does one safe save, one rejected unsafe save, then asserts a fresh `GetContentByIdAsync` still returns the safe value) - both initially failed on the cache-poisoning bug, then passed once the eviction was added. Re-verified: full `dotnet test TbTruongHoc.sln` → **26/26 passed, 0 failed**.
  - **Also found, unrelated to this fix:** while testing, the shared dev/test MariaDB (same database `docker compose up -d mariadb` and `dotnet test` both use, per README) was found to hold 182 leftover pages from this repo's full test-suite history (`SiteSettingsTests`, `PerPageSeoFieldsTests`, etc. all create real pages via `CreatePublishedPageAsync`/equivalent but never delete them) - visible in Manager's page list as clutter alongside real content. Logged as a deferred-work item (test suite should clean up the pages it creates, or use an isolated test site/DB) rather than fixed here - out of Story 1.3's scope.

## Design Notes

No existing content type in this repo has custom fields yet (`StandardPage`/`StandardPost`/`StandardArchive` are all empty base-class extensions), so there's no local `[Region]`/`Field` example to point to. Piranha's standard shape for this:
```csharp
[SiteType(Title = "Site settings")]
public class SiteSettings : SiteContent<SiteSettings>
{
    [Region(Title = "Phone")]
    public Piranha.Extend.Fields.StringField Phone { get; set; }
    // ZaloUrl, Address, MapsUrl follow the same one-field-per-region shape
}
```

## Verification

**Commands:**
- `dotnet test` -- expected: all tests pass, including the new `SiteSettingsTests.cs`, against `docker compose up -d mariadb` per README's "Running tests" section
- `docker compose up -d --build piranha-app` -- expected: dev container reflects the new partial; manually confirm Site A/B's Manager "Settings" edit UI opens and saves the four fields
