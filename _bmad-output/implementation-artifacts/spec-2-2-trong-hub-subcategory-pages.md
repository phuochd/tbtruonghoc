---
title: 'Story 2.2: Trống hub & 5 subcategory pages with product-card grid'
type: 'feature'
created: '2026-09-27'
status: 'done'
baseline_commit: 'a0cb9318f44b286acd5760640d43aaa425e24005'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-trongdoitam-2026-09-17/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Site B has no product content model: the only types are the Piranha sample Standard page/archive/post. There is also no Trống hub, no subcategory pages and no `product-card`, so drum buyers cannot browse by drum type and the 5 SEO keyword surfaces don't exist.

**Approach:** Add three content types:
- a `ProductPost` post type with an optional free-form price;
- a `ProductArchive` page type (IsArchive, items restricted to `ProductPost`) that renders a paginated `product-card` grid;
- a `ProductHubPage` standalone page type that renders a `category-tile` grid built at render time from its non-hidden, published `ProductArchive` children in the sitemap, skipping children with 0 published posts.

The Trống hub is a `ProductHubPage` and its 5 subcategories are `ProductArchive` children, so Story 2.1's nav submenu picks them up automatically.

## Boundaries & Constraints

**Always:**
- Everything is CMS-driven. There are no hardcoded subcategory names, counts or links in views or code. Tile and card counts reflow to whatever the CMS returns.
- Card anatomy follows DESIGN.md `product-card`:
  - surface background, 1px border, rounded.md;
  - thumbnail = post PrimaryImage (with descriptive alt = post title), or the secondary→primary gradient frame when there is no image;
  - eyebrow = archive title (label, secondary);
  - title (heading-sm);
  - one-line description = post Excerpt (body-sm, muted, clamped to 1 line);
  - price row: Price when set (price 18/700, on-surface), otherwise muted body-sm "Liên hệ báo giá";
  - CTA "Xem chi tiết" (primary fill, cta, rounded.sm) with aria-label "Xem chi tiết {title}".
- Card body and CTA are two separate, equally valid tap targets to the post permalink. Use a stretched title link plus an independent CTA anchor. No nested anchors.
- Pressed state on `:active`: 2px inset primary ring + scale 0.98. No hover-only affordances.
- Product-card grid is 1-up below 480px, 2-up from 480px, 3 at ≥768px and 4 at ≥1200px; category-tile grid is 2-up at mobile, 3 at ≥768px, 4 at ≥1200px. Both use a 12px gap. Uses Mộc Trầm tokens from `site-b.css` only.
- Archive paging: 12 per page via the `pageSize` argument, with plain prev/next links (Vietnamese labels) only when TotalPages > 1. No infinite scroll.
- Hub tiles are category-tile anatomy: eyebrow "Trống" = hub title, archive title, archive Excerpt, whole tile is one link. The hub renders its own blocks above the tiles.
- "Published post count" means the archive's published posts only (use `Archives.GetByIdAsync(id, 1, …, pageSize: 1).TotalPosts`, not `Posts.GetCountAsync`).
- New views set `ViewData["Title"]` and render `_MetaTags` like `Cms/Page.cshtml` (per-page SEO from Story 1.2).
- The price is rendered HTML-encoded as the editor typed it (exact, range, or "từ X").

**Never:**
- The PDP content itself (gallery, spec rows, sku-code, trust-block): that is Story 2.4.
- Thùng rượu / Bồn tắm pages (2.3), the homepage category grid, and the story page (2.5).
- "Đặt mua ngay", cart/checkout, fabricated prices, or placeholder/lorem text.
- Changing `StandardPage`/`StandardArchive`/`StandardPost`, their views, Site A output, `_Layout.cshtml`, or `Areas/Manager`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Hub, mixed | Hub with 5 ProductArchive children; 2 have 0 published posts, 1 is hidden | 2 tiles, in sitemap order | N/A |
| Hub, all empty | Every child has 0 posts | Tile grid omitted entirely; hub blocks still render | N/A |
| Draft-only archive | Child has only unpublished posts | Counted as empty → no tile | N/A |
| Card with price | Price = "từ 2.500.000đ" | Price row shows that text in price style; no "Liên hệ báo giá" | N/A |
| Card without price | Price blank | Muted "Liên hệ báo giá" | N/A |
| Card no image | No PrimaryImage | Gradient frame, no `<img>`, no broken icon | N/A |
| Empty archive direct URL | ProductArchive with 0 posts | Page header + blocks; no grid, no pagination, no "no products" text | N/A |
| 13 posts | Archive with 13 published | Page 1 shows 12 cards + "Trang sau"; page 2 shows 1 + "Trang trước" | Out-of-range page → Piranha default |

**Decisions (2026-09-27):**
- **Pre-2.4 product page:** a minimal `ProductPost` view (title, image, excerpt, price row, then the quote form with `ProductOfInterest` = post title, truncated to 200 chars). Story 2.4 replaces the body.
- **Price field:** one free-text `StringField` labeled "Giá (để trống = Liên hệ báo giá)". There is no format validation, and it is rendered HTML-encoded.
- **Page creation:** an idempotent seed on the Site B site only creates the hub "Trống" (`ProductHubPage`) plus 5 `ProductArchive` children, in this order: Trường học, Lân, Đội, Lễ hội, Chùa.
  - Pages get a title and slug only (`trong`, `trong/truong-hoc`, `trong/lan`, `trong/doi`, `trong/le-hoi`, `trong/chua`), no prose, and are published.
  - The seed runs only when Site B has no page with slug `trong`. If that page exists, the whole seed is skipped, so an editor's edits and child deletions stick. It never modifies existing pages. Deleting the hub itself re-seeds it on the next start.
- **Spec size:** kept whole (~2300 tokens) by user choice.

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Program.cs:228-231` -- `ContentTypeBuilder.AddAssembly(...).Build().DeleteOrphans()`: new attribute types register automatically; SiteSeed calls at 254-260 (add any new seed here).
- `src/TbTruongHoc.Web/Models/StandardArchive.cs`, `StandardPost.cs` -- attribute pattern to mirror. Piranha 12: `[PageType(IsArchive=true)]`, `[PageTypeArchiveItem(typeof(ProductPost))]`, `[PageTypeRoute]`/`[PostTypeRoute]`, `IArchiveService.GetByIdAsync<T>(id, page, category, tag, year, month, pageSize)`, `PostArchive<T>.TotalPosts/TotalPages/CurrentPage`.
- `src/TbTruongHoc.Web/Controllers/CmsController.cs` -- hard-typed `archive`/`page`/`post` actions; add new actions (`producthub`, `productarchive`, `productpost` routes) rather than generalizing existing ones.
- `src/TbTruongHoc.Web/Views/Shared/_SiteBNav.cshtml` -- sitemap usage (`WebApp.Site.Sitemap`, non-hidden children); mirror for hub children. Sitemap items expose `PageTypeName`, `IsHidden`, `Published`.
- `src/TbTruongHoc.Web/Views/Cms/Page.cshtml` -- `_MetaTags` head section + block loop to mirror; quote-form + `lead-form.js` include pattern.
- `src/TbTruongHoc.Web/Views/Shared/_QuoteRequestForm.cshtml`, `Models/QuoteRequestFormViewModel.cs` -- reuse with `ProductOfInterest` (max 200 chars).
- `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css` -- tokens `--sb-*` (L13-34), `sb-` class prefix; no card/grid styles yet.
- `src/TbTruongHoc.Web/Data/SiteSeed.cs` -- `TrongDoiTamInternalId`, idempotent get-or-create pattern.
- `tests/TbTruongHoc.Web.Tests/SiteBShellTests.cs:495-575` -- `GetSiteAsync`, `CreatePublishedPageAsync`, `HostnameOf`, `GetHtmlAsync`; `PerPageSeoFieldsTests.cs:237-280` -- create archive + post, cleanup in `finally`.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Models/ProductPost.cs` -- post type + `Product` region with `Price` -- AD-5 price.
- [x] `src/TbTruongHoc.Web/Models/ProductArchive.cs` -- archive page type restricted to ProductPost; `PostArchive<ProductPost> Archive`.
- [x] `src/TbTruongHoc.Web/Models/ProductHubPage.cs` -- standalone page type; `IReadOnlyList<CategoryTileModel> Tiles` (title, excerpt, permalink).
- [x] `src/TbTruongHoc.Web/Services/ProductCatalog.cs` -- `GetHubTilesAsync(siteId, hubId)`: sitemap children → non-hidden, published, `ProductArchive`, TotalPosts > 0 -- one testable place for the hide rule.
- [x] `src/TbTruongHoc.Web/Controllers/CmsController.cs` -- `ProductHub`, `ProductArchive` (page param, pageSize 12), `ProductPost` actions.
- [x] `src/TbTruongHoc.Web/Views/Cms/ProductHub.cshtml`, `ProductArchive.cshtml`, `ProductPost.cshtml`, `Views/Shared/_ProductCard.cshtml`, `_CategoryTile.cshtml` -- markup per Boundaries.
- [x] `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css` -- page header, grid, card, tile, price, pagination, pressed state.
- [x] `src/TbTruongHoc.Web/Data/TrongCatalogSeed.cs` + Program.cs call after SiteSeed -- idempotent hub + 5 archives per Decisions.
- [x] `tests/TbTruongHoc.Web.Tests/ProductCatalogTests.cs` -- render tests for each matrix row + two-anchors-same-href, no "Đặt mua", tile order; seed idempotency (run twice → no duplicates; edited title survives).

**Acceptance Criteria:**
- Given the hub with ≥1 non-empty subcategory, when opened on Site B, then every non-empty, non-hidden subcategory appears as a tile linking to its permalink, and the nav submenu lists the subcategories under the hub.
- Given a subcategory page at 375px, when rendered, then cards sit 1-up and both the card body and the CTA navigate to the same URL.
- Given the full test suite, when run, then all pre-existing tests still pass.

## Implementation Notes

- Price is a single-field region `ProductPost.Price` (StringField, titled "Giá (để trống = Liên hệ báo giá)"), not a `Product` region class: Piranha's ContentFactory collapses any one-field region class into the bare field, so a `ProductRegion { Price }` class fails at `Posts.CreateAsync` with a type-conversion error.
- Piranha 12's route attribute is `[ContentTypeRoute]` (there is no `PageTypeRoute`/`PostTypeRoute`); routes are `/producthub`, `/productarchive`, `/productpost`.
- Tile eligibility checks the child's loaded `TypeId == "ProductArchive"` rather than the sitemap's `PageTypeName`.
- The seed is skipped when any of the 6 seed slugs (hub + 5 children) already exists on the site, so a renamed hub never re-seeds into colliding child slugs; an internal `EnsureSeededAsync(api, siteId)` overload is used by tests.
- The seed appends the hub after existing top-level Site B pages (`SortOrder = sitemap.Count`); on a Site B with no pages at all it becomes the start page (permalink `/`).

## Spec Change Log

- 2026-09-27 (walkthrough, Phước): product cards were ~165px wide at 375px in the 2-up grid, too small for the photo and description. Product-card grid changed to 1-up below 480px and 2-up from 480px (`.sb-grid--products`). Category tiles are text-only and stay 2-up per DESIGN.md. Supersedes the "2-up at mobile" Boundary and the 375px acceptance criterion.

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V):

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| V1/B3 | Seed create branch never exercised; children (titles, slugs, order, type, published, parent) and hub SortOrder unasserted | medium | Pre-verified gap: dev DB already has `trong`, so every run takes the skip branch → patch (fresh-seed test on a throwaway site via internal siteId overload). |
| V2 | Hub `TypeId == ProductArchive` filter untested | low | Pre-verified gap; every test child is a ProductArchive → patch (StandardArchive child with a published post gets no tile). |
| B2/E1 | Editor renames hub slug → next start re-seeds; children still hold `trong/*` slugs → slug collision on child save → startup crash (or duplicate hub) | high | Real: Piranha stores full child slugs independently of the parent. Decision says seed runs *only when* no `trong` page exists (necessary, not sufficient), so also skipping when any seed slug exists is consistent → patch + test (rename hub slug, re-run, no throw, no new pages). |
| B1/E2 | Partial seed (crash between hub and children saves) never repaired | low | Real but needs a DB failure mid-startup; repairing contradicts the frozen skip-whole-seed decision → rejected. |
| E3 | Seed exception crashes startup | low | Same fail-loud behaviour as SiteSeed/SiteSettingsSeed; the realistic trigger (B2/E1) is patched → rejected. |
| E5 | Draft preview of an unpublished hub shows no tiles (sitemap loaded with onlyPublished: true) | low | Real for editors previewing before publish; trivial (`onlyPublished: false`, children already filtered by Published) → patch + test. |
| B4/E6/E7 | Header excerpt `Html.Raw` vs encoded excerpt in tiles/cards | low | Real inconsistency; direct correction → patch (encode in hub + archive headers). |
| B11 | `CategoryTileViewModel` lives in ProductCardModel.cs, away from `CategoryTileModel` | low | Findability; direct move → patch. |
| B9 | Missing tests: future-dated-only post, non-ProductArchive child, PDP price encoding, 200-char truncation, card with image | low | Non-ProductArchive = V2 (patch); future-dated-only post trivial → patch; others rejected (image needs media upload; truncation/encoding share code paths already tested). |
| B5 | PDP drops CMS blocks, no link back to category | false | Frozen decision enumerates the pre-2.4 page (title, image, excerpt, price, form); Story 2.4 replaces the body. |
| B6 | Nav lists empty categories; empty archive is a dead end | false | Design Notes make hidden-flag the editor's control; matrix defines empty-archive render; sticky contact bar is always present, so no dead end. |
| B7/E4 | Page 2+ duplicate title/meta; out-of-range page returns clamped 200 | low | Matrix: "Out-of-range page → Piranha default"; duplicate title only past 12 products → rejected. |
| B8 | Two extra DB calls per hub child per render | low | ≤5 children; the published check is needed once E5 loads the unpublished sitemap → rejected. |
| B10 | Thumbnail alt duplicates title; PDP image has no width/height | low | Spec Always mandates alt = post title; CLS on a temporary page → rejected. |
| E8 | 200-char truncation can split a grapheme | low | Needs a >200-char title in decomposed Unicode → rejected. |
| E9 | Price is a bare region, not a `Product` region | false | Documented in Implementation Notes (Piranha collapses single-field region classes); no consumer reads a `Product` region. |
| — | Seed on an empty Site B makes the hub the start page ("/") | false | Not a bad outcome: with no other page, the hub is the sensible home; it moves back to `/trong` once an editor places a homepage first. |

## Design Notes

Hub tiles come from the sitemap (not an `Archives` query across the site) so the hub owns its subcategories through the page tree, the same tree the nav uses. A future "Danh mục sản phẩm" hub reuses `ProductHubPage` with Thùng rượu/Bồn tắm archives as children. A hidden sitemap item is the editor's way to pull a subcategory from both nav and hub.

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test` (repo root) -- expected: all pass incl. ProductCatalogTests.
- `dotnet build` -- expected: 0 new warnings.

**Manual checks:**
- On `trongdoitam.local`, open the hub and one subcategory at 375px and 1280px: tiles/cards reflow correctly, the pressed ring shows on tap, and there are no broken images.
