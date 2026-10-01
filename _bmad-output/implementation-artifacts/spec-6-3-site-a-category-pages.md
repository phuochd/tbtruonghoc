---
title: 'Story 6.3: Site A category pages (11 ProductArchive pages in the Site A shell)'
type: 'feature'
created: '2026-10-01'
status: 'done'
baseline_commit: 'c2984470f2bd32596c8bc26ef3429f629ef4a976'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-tbtruonghoc-2026-09-18/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** On Site A, a category (`ProductArchive`) page still renders Site B's view: a `sb-` header and grid, `sb-card` product cards, and the Site B craftsman trust block. There is no breadcrumb. Category pages are what visitors reach from Google ("dù che nắng sân trường") and from the dropdown, homepage and aggregate page.

**Approach:** Give Site A's `ProductArchive` its own view. `CmsController.ProductArchive` switches to it on Site A, the same way the 6.2 hub does. The view renders breadcrumb → header (H1 + excerpt) → the editor's blocks → a Site A product-card grid with the existing 12-per-page pager. Title, meta and content stay CMS-driven per page (Story 1.2 SEO fields, unique slug).

## Boundaries & Constraints

**Always:**
- Breadcrumb sits directly below the nav: "Trang chủ / {parent hub title} / {category title}". The hub step links to the parent page's permalink. A category with no `ProductHubPage` parent gets "Trang chủ / {category}". Links are primary, the current step is plain text, `aria-label="Breadcrumb"`, and it reuses 6.2's `sa-breadcrumb` markup and CSS.
- Product grid: published posts only (Piranha's archive query), sitemap/archive order, 12 per page. Prev/next links ("Trang trước"/"Trang sau") plus "Trang x / y" only when there is more than 1 page. A card's price is the editor's free text, HTML-encoded. Blank → "Liên hệ để nhận báo giá" in the same position. Image alt = product title; no image → a plain teal placeholder block, never a broken image. Grid columns 3/2/1 (≥1024/≥768/<768).
- Empty category reached by URL: header and blocks only, no grid, no pager, no "no products" text (same as Site B). Hiding from nav/home/aggregate is already `GetHubTilesAsync`'s rule; this story only adds a regression test for it.
- `ConfigBlock` is skipped on Site A category pages (as on the Site A hub). Other blocks render.
- Cards are flat: no shadows, no gradients, no discount/urgency copy.
- **Decision (Q1):** a new production startup seed `SiteACatalogSeed` runs right after `SiteAHomeSeed`. It creates a published top-level `ProductHubPage` "Sản phẩm" (`san-pham`, appended after the existing top-level pages), plus the 11 categories as **draft** `ProductArchive` children (`Published = null`), title + slug `san-pham/{slug}` only. It runs only when Site A has none of those 12 slugs (the `TrongCatalogSeed` pattern), so editor renames and deletions stick. The dev `SiteASampleSeed` uses the same titles/slugs. When the hub already exists, it publishes each sample category that is still a draft with no posts and adds its sample products (the last category is still published but left empty). An existing dev DB keeps working unchanged.
- **Decision (Q2):** Site A product card = one link over the whole card. White `surface`, `border-neutral`, `rounded.lg`, a 4:3 image, the title, then the price (or "Liên hệ để nhận báo giá") in primary. No button. The accessible name is the product title.
- **Decision (Q3):** no closing contact band. The page ends after the grid/pager.
- **Decision (Q4):** the spec is kept whole (about 2.2k tokens), accepted by the owner.

**Never:**
- Change Site B's category output: `ProductArchive.cshtml`, `_ProductCard.cshtml` and Site B CSS stay byte-identical, and Site B renders exactly as before.
- Hardcode category names, counts or products in views. Build the PDPs (6.4/6.5) or restyle the product detail page.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Category with products | Site A archive, 2 published + 1 draft post | Breadcrumb, H1, 2 cards, no pager | N/A |
| 13 products | page 1 / page 2 | 12 cards + "Trang sau"; page 2: 1 card + "Trang trước", "Trang 2 / 2" | N/A |
| Price blank vs set | "" / "từ 2.500.000đ" | "Liên hệ để nhận báo giá" / the text | `<script>` in price is encoded |
| No image | post without PrimaryImage | Teal placeholder block, no `<img>` | N/A |
| Empty category by URL | 0 published posts | Header + blocks; no grid/pager/text | N/A |
| Top-level category | parent is not a hub | "Trang chủ / {category}" | N/A |
| Site B archive | any | Unchanged `sb-` markup + trust block | N/A |
| Seed, empty Site A (only home) | first start | Published hub + 11 draft categories; nav "Sản phẩm" is a plain link | N/A |
| Seed, any of the 12 slugs exists | restart | Nothing created or modified | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Controllers/CmsController.cs` l.138 `ProductArchive` -- add `if (await IsSiteAAsync(model.SiteId)) return View("SiteAProductArchive", model);` after loading `Archive`. Put the parent hub (title, permalink, or null) in a view-model prop or ViewData. Get it from `model.ParentId` with `_api.Pages.GetByIdAsync<PageInfo>`, checking `TypeId == nameof(ProductHubPage)`.
- `src/TbTruongHoc.Web/Models/ProductArchive.cs` -- optional `[JsonIgnore]`-style non-region prop for the breadcrumb parent (like `Archive`). `PageSize` = 12 is reused.
- `src/TbTruongHoc.Web/Views/Cms/ProductArchive.cshtml` -- Site B's view. Don't modify; copy the pager/blocks structure (`PageLink`, `ConfigBlock` skip).
- `src/TbTruongHoc.Web/Views/Cms/SiteAProductHub.cshtml` -- `sa-catalog`, `sa-breadcrumb`, `sa-catalog__header` markup to reuse.
- New `Views/Cms/SiteAProductArchive.cshtml` and `Views/Shared/_SiteAProductCard.cshtml` (takes `ProductPost`; `post.PriceText` is null when blank).
- `src/TbTruongHoc.Web/wwwroot/assets/css/site-a.css` -- breadcrumb/catalog at l.1289+. Tokens at the top; breakpoints 768/1024. Add `sa-product-grid`, `sa-pcard`, `sa-pager`.
- `src/TbTruongHoc.Web/Services/ProductCatalog.cs` `GetHubTilesAsync` -- the existing empty/draft/hidden rule. Don't change it.
- `src/TbTruongHoc.Web/Data/TrongCatalogSeed.cs` -- the pattern for the new `Data/SiteACatalogSeed.cs`: an all-or-nothing slug guard, `EnsureSeededAsync(IApi)` resolving the site by internal id, and an `internal` per-site overload for tests. `SiteSeed.TbTruongHocInternalId`.
- `src/TbTruongHoc.Web/Data/SiteASampleSeed.cs` -- move `Categories` titles/slugs to `SiteACatalogSeed` (keep the sample products here). Change `EnsureSeededAsync` to fill draft, post-less sample categories when the hub exists. `Program.cs` l.360-367: register the new seed between `SiteAHomeSeed` (which only runs when the site has no pages) and the sample seed.
- `tests/TbTruongHoc.Web.Tests/SiteSeedIdempotencyTests.cs`, `ProductLineTests.cs` -- seed-test patterns (throwaway site).
- `tests/TbTruongHoc.Web.Tests/SiteAHomeCatalogTests.cs` -- the site/hub/archive builders (`CreateHubAsync`, `CreateArchiveAsync`, `GetHtmlAsync`, `WithSiteACatalogAsync`) to reuse. Shared dev DB: scope asserts to the test's own ids.

## Tasks & Acceptance

**Execution:**
- [x] `Controllers/CmsController.cs`, `Models/ProductArchive.cs` -- Site A view switch + breadcrumb parent.
- [x] `Views/Cms/SiteAProductArchive.cshtml`, `Views/Shared/_SiteAProductCard.cshtml` -- breadcrumb, header, blocks, grid, pager.
- [x] `wwwroot/assets/css/site-a.css` -- card, grid, pager styles under Site A tokens.
- [x] `Data/SiteACatalogSeed.cs` (new), `Data/SiteASampleSeed.cs`, `Program.cs` -- Q1 seed + dev sample adaptation.
- [x] `tests/.../SiteACategoryPageTests.cs` (new) -- every I/O row (incl. both seed rows), the empty category absent from the nav dropdown/aggregate, Site B archive unchanged.

**Acceptance Criteria:**
- Given two Site A categories with different SEO titles, when each is requested, then each resolves at its own slug with its own `<title>`/meta.
- Given a 12th category published in the Manager with products, when its URL loads, then it renders in this view with no code change.
- Given the full suite, when run, then all pre-existing tests pass.

## Implementation Notes

## Spec Change Log

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V):

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| V1/B4 | Breadcrumb hub filter (unpublished / non-hub parent) untested | low | Pre-verified gap; test only → patch. |
| V2 | Startup wiring/order of `SiteACatalogSeed` untested | low | Pre-verified gap; deleting or moving the `Program.cs` call passes every test → patch (startup + order tests). |
| B1/E1 | Crash mid-seed leaves a partial catalog forever | low | Needs a failure between consecutive saves at first start; the fix (transaction/partial-repair) adds branches → rejected. |
| E2 | A hub under another slug gets a second "Sản phẩm" hub | low | Production Site A has no hub today (only the home seed); a guard adds a branch → rejected. |
| E3 | Two instances seeding concurrently | false | Single-instance deploy (docker compose); same as every other startup seed. |
| B2/E4 | `/page/N` past the end returns 200 with header only | low | Real, same as Site B's archive view; the fix adds a controller guard and the URLs aren't linked → rejected. |
| B3 | Paged archive pages share title/meta, no canonical/prev-next | medium | Pre-existing: `_MetaTags` has no canonical on any page, and Site B has the same pager → defer. |
| B5 | Fresh production shows a published, empty hub | false | Frozen decision Q1 (accepted consequence). |
| B6 | No `srcset`, so the 600px image may look soft on phones | low | Same rendition as Site B's card; 600px ≈ 2× a 343px phone card; adds surface → rejected. |
| B7 | Image alt repeats the adjacent title | false | Frozen: "Image alt = product title" (epic a11y: descriptive alt on every product photo). |
| B8 | Breadcrumb aria-label in English, not `<ol>`, no JSON-LD | false | Reuses 6.2's frozen breadcrumb markup (`aria-label="Breadcrumb"`); JSON-LD isn't in the intent. |
| B9 | Hover styles have no `:focus-visible` twin | low | The generic outline exists, but a direct CSS fix → patch. |
| B10 | 12 slug lookups on every startup | low | Negligible (12 cached reads at boot) → rejected. |
| B11 | `DoesNotContain("sb-")` fragile; redundant assert | low | Direct correction → patch (`class="sb-`). |
| B12 | Stale comments in `Program.cs` / `SiteASampleSeed` | low | Direct correction → patch. |
| B13 | Spec/sprint files missing from the diff | false | Excluded on purpose; the spec is the claims file. |
| E5 | `~/` breadcrumb root wrong under a site path prefix | false | Sites resolve by hostname (no prefix); same as the 6.2 hub. |
| E6 | Dev seed loads a non-archive page at a category slug as `ProductArchive` | low | Dev-only, needs an editor page at that exact slug → rejected. |
| E7 | Card-extraction helper throws on a missing `</a>` | low | Masks the real assertion failure; direct fix → patch. |

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test` (repo root) -- expected: all tests pass.
- `dotnet build` -- expected: no new warnings.

**Manual checks:**
- Run the app with dev samples at 375/820/1280px: `/san-pham/du-che-nang-san-truong` shows breadcrumb, header, cards; the empty sample category is absent from the dropdown.
