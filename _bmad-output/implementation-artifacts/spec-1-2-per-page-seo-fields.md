---
title: 'Per-page SEO fields (Title/Meta Description/Slug) — verification for Site A & Site B'
type: 'feature'
created: '2026-09-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 1.2 requires editors to set Title/Meta Description/Slug on any Page or Post, on either site, from Piranha Manager, with slugs staying safe to change post-publish. Investigation shows Piranha's `Page<T>`/`Post<T>` base (used by `StandardPage`, `StandardPost`, `StandardArchive`) already carries these fields, `Views/Cms/Page.cshtml`, `Post.cshtml`, and `Archive.cshtml` already render them via `WebApp.MetaTags(Model)` and the `MetaTitle ?? Title` fallback, Manager's `UseManager()` already exposes their edit UI, and `_Layout.cshtml`'s nav already resolves links through `WebApp.Site.Sitemap` (not hardcoded URLs) — so the platform capability exists, but no automated test proves it works end-to-end or survives a post-publish slug change.

**Approach:** Add integration tests only (no production code changes expected) to `tests/TbTruongHoc.Web.Tests/`, reusing the existing `PiranhaWebApplicationFactory` + `PiranhaAppCollection` pattern: seed a page via `IApi`, set Title/MetaDescription/Slug, publish, and assert the rendered `<title>`/`<meta name="description">`/URL match exactly; then change the slug post-publish and assert the page resolves at the new URL and the Sitemap-driven nav reflects it, with no broken internal link. If any gap is found (e.g. a missing fallback or an unwired view), patch the minimal production code needed to close it — do not restructure the existing per-view pattern.

## Boundaries & Constraints

**Always:**
- Test against both `StandardPage` and `StandardPost` on at least one of the two seeded sites (`tbtruonghoc` or `trongdoitam.net`) from Story 1.1 — do not invent a third site or a new content type for this story.
- Assert the slug-change behavior through Piranha's own resolution (`IApi`/`Sitemap`/routing), never by hardcoding an expected URL string that duplicates permalink-building logic.
- Reuse `PiranhaWebApplicationFactory`/`PiranhaAppCollection` exactly as the existing tests do — no second test host/bootstrap pattern.

**Never:**
- Never add SEO fields to the content-type models — `Page<T>`/`Post<T>` already provide them; adding duplicate fields would shadow Piranha's own.
- Never build custom slug-history/redirect logic in this story — that is Epic 8's Alias-based redirect scope (Story 8.1), not this one.
- Never touch `SiteSettings`, contact fields, or analytics — those are Stories 1.3/1.6.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Editor sets Title/Meta Description/Slug and publishes | New `StandardPage` (or `StandardPost`) with explicit `MetaTitle`, `MetaDescription`, `Slug` | Rendered `<title>` equals `MetaTitle`; `<meta name="description">` equals `MetaDescription`; page resolves at the edited slug's URL | N/A |
| Page/Post has no `MetaTitle` set | `MetaTitle` empty, `Title` set | Rendered `<title>` falls back to `Title` (existing `Page.cshtml`/`Post.cshtml` behavior) | N/A |
| Editor changes slug post-publish | Previously published page, slug changed via `IApi` | Page reachable at the new slug's URL; nav item generated from `WebApp.Site.Sitemap` reflects the new URL | Old slug no longer resolves (expected 404 — no redirect in this story's scope) |
| New Page/Post type used by a later epic | Any future type inheriting `Page<T>`/`Post<T>` | Inherits the same editable Title/Meta Description/Slug without new code | N/A |

</frozen-after-approval>

## Implementation Notes

- Confirmed the platform capability exists largely as investigation predicted: `StandardPage`/`StandardPost`/`StandardArchive` inherit `Page<T>`/`Post<T>`, the three `Views/Cms/*.cshtml` already do `ViewData["Title"] = MetaTitle ?? Title`, and `_Layout.cshtml`'s nav already reads `WebApp.Site.Sitemap`. One part of the Intent's premise turned out wrong: `@WebApp.MetaTags(Model)` was **not** safe as-is (see the production-code fix below).
- Added `tests/TbTruongHoc.Web.Tests/PerPageSeoFieldsTests.cs` (real MariaDB via the existing `PiranhaWebApplicationFactory`/`PiranhaAppCollection` pattern), covering: SEO fields render and resolve at the edited slug (Page + Post); `MetaTitle`-empty falls back to `Title` (Page + Post); a post-publish slug change moves the page/post to its new URL, with Piranha's own `GetSitemapAsync` reflecting the new permalink for the Page case; HTML-special-characters in Title/MetaDescription are correctly encoded (regression test for the bug below).
- Every test page/blog explicitly sets `SortOrder = 1` (a self-documented constant). Discovered mid-implementation: Piranha treats whichever top-level page has the lowest `SortOrder` for a site as that site's home page and forces its Sitemap `Permalink` to `"/"` regardless of its own `Slug` — every page created via `CreateAsync<T>()` defaults to `SortOrder = 0`, so two throwaway top-level test pages at the default would race for "home page" status and corrupt the slug-change assertion. A test-authoring correction, not a platform gap.
- The frozen I/O Matrix's third row still reads "Old slug no longer resolves (expected 404 — no redirect in this story's scope)" — that assumption turned out wrong (the old slug keeps serving the same content) and this workflow's rules do not permit editing frozen-after-approval content during implementation, so the row stands uncorrected as written. The tests deliberately do not assert either the 404 in that row or the actual current behavior, since the real Story 1.2 AC only requires the new slug to resolve and Sitemap-driven links to keep working (both asserted) — this note is the record of that discrepancy for whoever next revises the frozen block.
- **Production code fix (beyond the Intent's "tests only" plan):** built and ran a test with HTML-special characters (`&`, `<`, `>`, `"`) in Title/MetaDescription and found Piranha's own `WebApp.MetaTags(Model)` helper (`Piranha.AspNetCore`'s `PiranhaHtmlExtensions.MetaTags`, confirmed unfixed in both the installed 12.0.0 and upstream 12.2 source) builds its `<meta>` tags via raw string interpolation with **no HTML-encoding** of `MetaKeywords`/`MetaDescription`/`OgTitle`/`OgDescription`. A `"` in an editor-entered Meta Description — plausible in real Vietnamese marketing copy — breaks the `content="..."` attribute and corrupts the page's `<head>` on every page/post on both sites. Confirmed with the user this was worth fixing in this story rather than deferring. Fix: added `Views/Shared/_MetaTags.cshtml`, a faithful reimplementation of the same tags/conditions/fallbacks (verified line-by-line against upstream source) but built from ordinary Razor `@`-expressions, which HTML-encode by default; replaced `@WebApp.MetaTags(Model)` with `@await Html.PartialAsync("_MetaTags", Model)` in `Views/Cms/Page.cshtml`, `Post.cshtml`, and `Archive.cshtml`. No other behavior changed (robots index/follow, keywords, description, generator, og:type/title/image/description all preserved).
- Found and fixed a pre-existing test-isolation leak in Story 1.1's `SiteSeedIdempotencyTests.cs`: it mutated the `tbtruonghoc` site's `Hostnames` to `"manager-edited.example"` to test seed-idempotency but never restored it, so a later `dotnet test` run's `HostnameResolutionTests` (same shared `PiranhaAppCollection`, same real DB) could fail depending on accumulated state from earlier runs. Wrapped the mutation in a `try`/`finally` that restores the original value.
- Re-verified after all fixes: `dotnet test` passes 15/15 on a freshly reset dev database (`docker compose down -v && docker compose up -d`) and again on a second consecutive run without resetting it (repeatability). Rebuilt and restarted the `piranha-app` Compose service (`docker compose up -d --build piranha-app`) so the running dev container reflects the `_MetaTags.cshtml` fix too.
- Verification commands were run with `ConnectionStrings__piranha` set per `README.md`'s "Running tests" section (`server=localhost;port=3307;database=piranha;uid=piranha;password=<value from .env>`), against the Story 1.1 Compose stack (`docker compose up -d mariadb`).

## Review Triage Log

- **[patch]** Post slug-change was never tested (only Page). Verified: added `Changing_Post_Slug_After_Publish_Moves_The_Post`.
- **[patch]** Post `MetaTitle`-empty fallback was never tested (only Page). Verified: added `Published_Post_Without_MetaTitle_Falls_Back_To_Title`.
- **[patch]** Meta-description assertions used a weak substring `Assert.Contains(metaDescription, html)` that would not catch a malformed/missing `<meta>` tag. Verified and tightened to match the exact tag markup via a `MetaDescriptionTag(...)` helper — this same tightening is what surfaced the real encoding bug documented above.
- **[patch]** No coverage existed for HTML-encodable characters in Title/MetaDescription. Verified as a real, high-value gap — added `Published_Page_With_Html_Special_Characters_Is_Escaped_In_Title_And_MetaDescription`, which caught a genuine upstream Piranha defect (see Implementation Notes) and now guards the fix.
- **[patch]** `FindById`'s helper declared a non-nullable `SitemapItem` return type but returned `null!` on a miss. Verified; changed the return type to `SitemapItem?` and removed the suppression.
- **[patch]** The frozen block's own boundaries never anticipated touching `SiteSeedIdempotencyTests.cs` (a Story 1.1 file). Verified real; since frozen-after-approval content cannot be edited during implementation, addressed by documenting the scope crossing explicitly in Implementation Notes instead (see above) rather than editing the frozen Boundaries.
- **[patch]** (self-identified while fixing the above) The Implementation Notes originally claimed a frozen-block boundary was "dropped," which was inaccurate — the frozen block was never edited, only the tests' assertions were adjusted. Corrected the wording in Implementation Notes to describe the actual, permitted resolution (documenting the discrepancy rather than editing frozen content).
- **[human-observed, walkthrough]** Manual verification in the dev environment (`http://tbtruonghoc.local:8091/test-page` → renamed to `http://tbtruonghoc.local:8091/test-page-1`) found old-slug behavior is **not consistently either** of the two things this doc has claimed: immediately after the slug change the old URL (`/test-page`) 404'd, but on a later refresh it resolved again alongside the new URL (`/test-page-1`), both serving 200. This matches neither the frozen I/O Matrix row ("old slug no longer resolves — expected 404") nor this Implementation Notes' own correction ("the old slug keeps serving the same content") as a steady-state fact — it appears to flicker around the change, most likely due to Piranha's own in-memory cache (`options.UseMemoryCache()`, `src/TbTruongHoc.Web/Program.cs:27`) settling asynchronously after a slug-changing `SaveAsync`. `PerPageSeoFieldsTests.cs` does not catch this because it re-fetches via `IApi` immediately after save, never through HTTP with the timing a real Manager edit + browser refresh has. Not addressed in this iteration (test-only story, and root-causing/fixing cache-consistency behavior is a platform-level concern beyond this story's Boundaries) — recorded here as a known gap for whoever next revisits slug-change/redirect behavior (Story 8.1) or Piranha cache configuration.
