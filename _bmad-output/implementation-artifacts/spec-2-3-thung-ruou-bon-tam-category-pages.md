---
title: 'Story 2.3: Thùng rượu gỗ & Bồn tắm gỗ category pages'
type: 'feature'
created: '2026-09-27'
status: 'done'
baseline_commit: '42d1ef31006fc1f681f92972341256a98c6d8740'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Site B's catalog only covers Trống. The other two lines, Thùng rượu gỗ trang trí and Bồn tắm gỗ, have no category pages, so visitors can't browse them and the two SEO surfaces don't exist.

**Approach:** Reuse Story 2.2's `ProductArchive` page type and `product-card` grid unchanged. An idempotent startup seed on Site B creates two top-level `ProductArchive` **drafts**, "Thùng rượu gỗ" (`thung-ruou-go`) and "Bồn tắm gỗ" (`bon-tam-go`). It also creates 3 draft variant posts under Thùng rượu gỗ. Once an editor publishes a page, it appears in the nav as a flat link after Trống.

## Boundaries & Constraints

**Always:**
- Everything is CMS-driven: views hardcode no titles, variants or links. Pages get a title and slug only, with no prose.
- The page seed works per slug. It creates a page only when Site B has no page with that slug and never modifies an existing page. Deleting a seeded page re-seeds it on the next start, same as the Trống hub.
- Seeded pages are appended after the existing top-level Site B pages (`SortOrder = sitemap.Count`, then +1). The seed runs after `TrongCatalogSeed`.
- Grid, card, pagination, SEO head and empty-archive rendering are exactly Story 2.2's behavior. No new CSS or view changes are expected.

**Never:**
- Real per-variant prices shown by default, "Đặt mua ngay", cart/checkout, fabricated content, lorem ipsum, broken-image icons.
- PDP content (2.4), the story page / trust-block and its links (2.5), the Site B homepage grid, the landing page (Epic 3).
- Changing `_SiteBNav.cshtml`, Story 2.2's hub tile rule, `StandardPage`/`StandardArchive`/`StandardPost`, Site A, `_Layout.cshtml`, or `Areas/Manager`.

**Decisions (2026-09-27, Phước):**
- **Q1 publish state:** both pages are seeded as **drafts** (`Published = null`). The editor publishes each page once it has real products. That publish action is how the "hide empty category" rule is carried out: no nav or code enforcement, consistent with 2.2 where the editor controls this.
- **Q2 Bồn tắm pre-content:** it **stays unpublished** until the client adds content. There is no "coming soon" block.
- **Q3 Thùng rượu pricing:** enforced **editorially only**. It renders like Trống: if an editor fills in "Giá", that price shows (AD-5). No hide-price flag.
- **Q4 variants:** the seed creates 3 **draft** `ProductPost`s under Thùng rượu gỗ, with titles only, in this order:
  - "Thùng rượu gỗ sồi – ngựa kéo"
  - "Thùng rượu gỗ sồi – 1 ngựa"
  - "Thùng rượu gỗ sồi – 2 ngựa"

  They are created only in the same run that creates the Thùng rượu gỗ page, so an editor's deleted or edited variants never come back.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Fresh Site B | Neither slug exists | Both top-level `ProductArchive` pages created, unpublished, with correct title and slug, sorted after existing top-level pages. Thùng rượu has exactly 3 unpublished posts with the titles above | N/A |
| Re-run | Both exist, one renamed | No new pages or posts; the edited title survives | N/A |
| One deleted | `bon-tam-go` missing | Only Bồn tắm re-created; Thùng rượu's posts untouched (no duplicates) | N/A |
| Drafts, anonymous | Seeded state, not published | Neither page is in the nav; the direct URL does not render the page to the public | Piranha default (404) |
| Thùng rượu published | Page + 3 posts published, Price blank | 3 cards, eyebrow "Thùng rượu gỗ", each with muted "Liên hệ báo giá" and linking to its own post; no "Đặt mua"; nav lists it after Trống | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Data/TrongCatalogSeed.cs` -- pattern to mirror: `GetBySlugAsync<PageInfo>(slug, siteId)` existence check, `GetSitemapAsync(siteId, onlyPublished: false)` for SortOrder, and an `internal EnsureSeededAsync(api, siteId)` overload for tests.
- `src/TbTruongHoc.Web/Program.cs:265-270` -- seed calls; add the new seed after `TrongCatalogSeed`.
- `src/TbTruongHoc.Web/Models/ProductPost.cs` -- `Posts.CreateAsync<ProductPost>()`; set `BlogId` = archive id, `Title`, leave `Published` null. Piranha generates the slug from the title.
- `src/TbTruongHoc.Web/Models/ProductArchive.cs`, `Views/Cms/ProductArchive.cshtml`, `Views/Shared/_ProductCard.cshtml`, `Views/Shared/_SiteBNav.cshtml` -- reused as-is.
- `tests/TbTruongHoc.Web.Tests/ProductCatalogTests.cs:240-380` -- seed tests on a throwaway site via the internal overload, render helpers (`GetHtmlAsync`, `HostnameOf`), and cleanup in `finally`; mirror them in the new test file.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Data/ProductLineSeed.cs` -- per-slug idempotent seed of the two draft top-level `ProductArchive` pages, plus the 3 draft variant posts when Thùng rượu is newly created.
- [x] `src/TbTruongHoc.Web/Program.cs` -- call `ProductLineSeed.EnsureSeededAsync(options.Api)` after `TrongCatalogSeed`, with a comment in the existing style.
- [x] `tests/TbTruongHoc.Web.Tests/ProductLineTests.cs` -- one test per matrix row. Seed tests run on a throwaway site; render tests publish on Site B and clean up.

**Acceptance Criteria:**
- Given the seeded pages are published by an editor with published variants, when a visitor opens `/thung-ruou-go` on Site B, then it shows a product-card grid of those variants, all contact-for-quote.
- Given Bồn tắm gỗ is left as seeded, when Site B is browsed anonymously, then it appears nowhere (nav or page).
- Given the full test suite, when run, then all pre-existing tests still pass.

## Implementation Notes

- Variant posts use category "General", because Piranha requires a category on every post. It is the same value the 2.2 tests use.
- The render tests (`WithSiteBPagesAsync`) run against the startup-seeded Site B pages and restore each page's and post's `Published` and `Price` in `finally`. `Published_Thung_Ruou_...` expects the 3 seeded variant titles to still exist, so renaming or deleting them in the dev Manager breaks the test.
- Build and test ran with `--artifacts-path` under `tests/TbTruongHoc.Web.Tests/obj/`, because the running dev server locks the default output. The script tests' `FindRepoRoot()` needs the output folder to be inside the repo.

## Spec Change Log

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V):

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| V1 | Startup call order (ProductLineSeed after TrongCatalogSeed) untested; the nav-order test passes on stored dev data regardless | medium | Pre-verified gap. Pinning it needs a fresh-DB test host, which the single persistent-DB factory doesn't provide → defer (per filed disposition). |
| E1/B3 | `SortOrder = sitemap.Count` misplaces the page if sibling SortOrders have gaps | false | Piranha's page repository shifts siblings on insert, delete and move, so top-level SortOrders stay contiguous 0..n-1; this is the same pattern TrongCatalogSeed uses. |
| E2/B2 | Crash between the archive save and the variant saves → variants are never created | low | Needs a DB failure mid-startup; repairing it needs a marker/guard. Same verdict as 2.2 B1/E2 → rejected. |
| E3 | Editor changes the Thùng rượu slug → the next start seeds a duplicate draft archive + 3 drafts | low | Real, but only a draft (never public). Slug-based identity is the frozen per-slug decision → rejected; surfaced to the user at presentation. |
| E4 | Two instances seeding concurrently | low | Single-instance deployment; same as the existing seeds → rejected. |
| E5 | A seed exception crashes startup | low | Same fail-loud behavior as SiteSeed/TrongCatalogSeed → rejected. |
| E6/B11 | `Cards` test helper throws an opaque range exception on an unclosed article | low | Direct correction → patch (assert `end >= 0`). |
| B4 | Docstring says edited/deleted variants never come back, but deleting the whole Thùng rượu page re-creates them | low | Direct doc correction → patch. |
| B1 | The empty-category hide rule is editorial and not enforced | false | Frozen decision Q1: the editor's publish action is the rule. |
| B5/B6 | No test for the Thùng-rượu-deleted branch, Bồn tắm published, or a missing Site B | low | Paths are shared with the covered fresh-seed and render tests; adding tests is more than a correction → rejected. |
| B7/V2 | Variant creation order is claimed but not asserted; display order follows publish dates | low | Order is invisible until publish, and then determined by publish date → rejected. |
| B8 | Hardcoded English category "General" | low | Categories aren't rendered or linked on Site B pages; editors can rename it → rejected. |
| B9 | Variant slugs checked only for uniqueness, not readability | low | Piranha's slugifier strips diacritics and punctuation; no đ in these titles → rejected. |
| B10 | Render tests mutate the shared Site B | low | Same pattern as the 2.2 tests, with restore in `finally` → rejected. |
| B12 | `GetAsync` doesn't dispose HttpClient/request/response | low | Test-only; mirrors existing helpers → rejected. |
| B13 | Unrelated `.gitignore` change | false | Requested by the user this session. |
| B14 | Spec/tracking files are missing from the diff | false | Intentional: the diff covers code only; the spec goes to the edge-case layer separately. |

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test` (repo root) -- expected: all pass, including ProductLineTests.
- `dotnet build` -- expected: 0 new warnings.

**Manual checks:**
- In the Manager on Site B, both pages and the 3 Thùng rượu drafts exist. After publishing the page and one variant, `/thung-ruou-go` renders the card at 375px (1-up) and 1280px, and the nav shows the link after Trống.
