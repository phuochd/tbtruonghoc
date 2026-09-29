---
title: 'Story 5.1: Site B Blog listing page'
type: 'feature'
created: '2026-09-28'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '07c6bcd2a89e9e8f0163591967f7c0a034cd1485'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-5-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Site B has no content section, so there is nowhere to publish craft, wood-care and Tết-gifting articles for SEO and trust (FR-14).

**Approach:** Add a site-agnostic `BlogArchive` page type whose items are a new `BlogPost` post type, using the same Archive+Post mechanism as the product catalog. Its listing shows paginated blog cards (thumbnail, title, date, excerpt), or "Bài viết đang được cập nhật" when empty. A startup seed puts one "Blog" archive (slug `blog`) on Site B.

## Boundaries & Constraints

**Always:**
- Use Piranha's native archive loading and `/page/{n}` pagination, 12 per page.
- SEO is Piranha's built-in `IMeta`/Slug via `_MetaTags`, on both types.
- Only published posts are listed. Standalone Pages never appear.
- Cards use `.sb-card`/`.sb-grid` and the gradient thumb placeholder. The whole card is one tap target: a stretched title link, with no separate CTA, price or eyebrow.
- The date is the post's `Published` date shown as `dd/MM/yyyy`, in muted caption style.
- Microcopy is Vietnamese: "Bài viết đang được cập nhật", "Trang trước"/"Trang sau".
- The seed is idempotent by slug and never modifies an existing page.
- Types and views have no Site B-specific code, so Epic 7 can reuse them on Site A.
- Decision: the seeded Site B `blog` archive is created **published** (visible, non-hidden, top-level), so the Blog nav link shows immediately with the empty state.
- Decision: the blog grid is **1-up below 480px** (same as `.sb-grid--products`), 2 from 480px, 3 at ≥768px, 4 at ≥1200px.

**Never:**
- No infinite scroll.
- No like/view counters, account icons or carousels.
- No hardcoded or invented article copy, and no seeded posts.
- No Site A seed (that is Epic 7).
- No related-posts module, breadcrumb or rich article layout (Story 5.2).
- No hardcoded Blog nav item: nav stays sitemap-driven.
- Do not change `StandardArchive`/`StandardPost` or the product types.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Listing | 3 published BlogPosts | 3 cards, newest first; each links to the post permalink; no pager | N/A |
| Paging | 13 published posts | Page 1 has 12 cards + "Trang sau"; `/blog/page/2` has 1 card + "Trang trước" + "Trang 2 / 2" | N/A |
| Empty | 0 published (drafts may exist) | Header + "Bài viết đang được cập nhật"; no grid, no pager | N/A |
| No image / excerpt | Post without PrimaryImage or Excerpt | Gradient placeholder frame; excerpt paragraph omitted | N/A |
| Seed rerun | `blog` already exists (edited by an editor) | Nothing created or changed | N/A |
| Post page | Visitor taps a card | Minimal post page on the site layout: H1 title, date, body blocks, own meta tags (5.2 enriches) | Unpublished → 404 like other types |

</frozen-after-approval>

## Code Map

- `Models/ProductArchive.cs`, `Models/ProductPost.cs` -- template for the new types: `[PageType(IsArchive=true)]`, `[ContentTypeRoute]`, `[PageTypeArchiveItem]`, `PageSize`, `PostArchive<T> Archive`. `ContentTypeBuilder.AddAssembly` in `Program.cs:294-297` auto-registers them.
- `Controllers/CmsController.cs:128-165` -- the `productarchive`/`productpost` actions to mirror: `_loader.GetPageAsync`, `_api.Archives.GetByIdAsync<T>(id, page, null,null,null,null, PageSize)`, `UnauthorizedAccessException` → `Unauthorized()`.
- `Views/Cms/ProductArchive.cshtml` -- header markup, `PageLink(n)`, the `sb-pager` block (75-88) and the `_MetaTags` head section to copy. Do not edit it.
- `Views/Shared/_ProductCard.cshtml` + `Models/ProductCardModel.cs` -- typed to `ProductPost`. Write a separate `_BlogCard.cshtml` rather than generalizing them.
- `wwwroot/assets/css/site-b.css` -- `.sb-grid` (538), `.sb-grid--products` <480px rule (555-563), `.sb-card*` (578-740), `.sb-pager*` (745-777). Add `--blog` modifiers only.
- `Data/ProductLineSeed.cs` -- seed pattern: Site B lookup via `SiteSeed.TrongDoiTamInternalId`, `GetBySlugAsync<PageInfo>`, SortOrder from sitemap count, `internal EnsureSeededAsync(api, siteId)` for tests. Seed calls are at `Program.cs:320-349`.
- `Views/Shared/_SiteBNav.cshtml:24` -- sitemap-driven nav. A published, non-hidden top-level page appears automatically, so leave it unchanged.
- `tests/TbTruongHoc.Web.Tests/ProductCatalogTests.cs` -- patterns for archive render/paging/empty tests and throwaway-site seed tests; helpers at 388-469 and 537-548.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Models/BlogArchive.cs` -- `[PageType(Title="Blog", IsArchive=true)]`, route `/blogarchive`, archive item `BlogPost`, `PageSize=12`, `PostArchive<BlogPost> Archive` -- the listing type.
- [x] `src/TbTruongHoc.Web/Models/BlogPost.cs` -- `[PostType(Title="Bài viết")]`, route `/blogpost`, no extra regions (body = blocks) -- the article type.
- [x] `src/TbTruongHoc.Web/Controllers/CmsController.cs` -- add `blogarchive` and `blogpost` actions mirroring the product ones.
- [x] `src/TbTruongHoc.Web/Views/Cms/BlogArchive.cshtml` -- header, blocks, card grid or the empty-state paragraph, pager.
- [x] `src/TbTruongHoc.Web/Views/Shared/_BlogCard.cshtml` -- thumbnail, stretched title link, `<time datetime>` date, excerpt.
- [x] `src/TbTruongHoc.Web/Views/Cms/BlogPost.cshtml` -- minimal post page: `_MetaTags`, H1, date, blocks.
- [x] `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css` -- `.sb-grid--blog`, `.sb-card__date`, `.sb-empty` styles using existing tokens.
- [x] `src/TbTruongHoc.Web/Data/BlogSeed.cs` + `Program.cs` -- idempotent Site B `blog` archive seed, called after `LandingPageSeed`.
- [x] `tests/TbTruongHoc.Web.Tests/BlogListingTests.cs` -- one test per matrix row, plus: the seed goes on Site B only, and a post's MetaTitle/description render.

**Acceptance Criteria:**
- Given the seeded Blog page is published, when Site B is browsed, then "Blog" is in the nav with zero posts.
- Given a BlogPost with MetaTitle and MetaDescription set, when its page and the listing are rendered, then the post page carries its own `<title>`/description and the listing carries the archive's.
- Given the full suite, when run, then all pre-existing tests pass.

## Implementation Notes

- `BlogPost` action returns `NotFound()` when the loader gives a null model (unpublished post, anonymous visitor). Without it the view hit a null model and returned 500. The existing `ProductPost` action has the same 500-on-draft behavior; it was left unchanged (out of scope, "do not change the product types").
- Blog excerpts use `.sb-card__desc` with a `.sb-grid--blog` override that clamps to 2 lines (product excerpts stay single-line).
- The post page gets small `.sb-article*` styles (title, muted date) alongside the listed CSS; 5.2 will enrich it.
- Blocks on both blog views skip `ConfigBlock` (as on the craftsman story page).

## Spec Change Log

## Review Triage Log

| # | Source | Finding | Verdict | Evidence | Route |
|---|--------|---------|---------|----------|-------|
| 1 | blind, edge (×2) | `BlogArchive` action dereferences a null model → 500 for an unpublished/unknown archive | medium | `CmsController.cs` BlogArchive sets `model.Archive` with no null check; `BlogPost` needed the same guard (implementer saw the 500 on posts) | patch |
| 2 | verif-gap, blind | Card `<img>` branch + alt fallback untested | medium | Pre-verified: no blog test sets `PrimaryImage` | patch |
| 3 | verif-gap, blind | `ConfigBlock` skip on both blog views untested | medium | Pre-verified: no blog test adds a `ConfigBlock` | patch |
| 4 | edge | Seed keys on slug only: an editor renaming the slug (e.g. to `tin-tuc`) gets a second *published* Blog in the nav on next restart | medium | `BlogSeed.cs` checks only `GetBySlugAsync("blog")`; unlike earlier seeds this page is live, so the duplicate is visible | patch |
| 5 | blind | 2-line clamp relies on `overflow:hidden` inherited from the 1-line product rule | low | `.sb-grid--blog .sb-card__desc` does not set overflow itself; one-line direct fix | patch |
| 6 | blind, edge | Archive ignores year/month/category/tag route values and clamps out-of-range `/page/n` → 200 duplicates | medium (SEO) | Same shape as the pre-existing `ProductArchive` action | defer |
| 7 | implementer note | `ProductPost` action returns 500 for a draft post (null model) | medium | Pre-existing; `CmsController.ProductPost` has no null guard | defer |
| 8 | blind | Paged listings share page 1's title/description; no rel prev/next in head | low | Real but same as product archives; fix adds new SEO surface | reject |
| 9 | blind | Card alt falls back to title, duplicating link text | low | Deliberate project convention (`_ProductCard` uses `alt=@post.Title`; "alt decision A") | reject |
| 10 | blind | Article page omits the card's image/excerpt | false | Frozen matrix defines the post page as title, date, blocks; 5.2 enriches | reject |
| 11 | blind | Startup nav test can return early on an edited dev DB | low | Documented, deliberate; throwaway-site seed test covers seed shape | reject |
| 12 | blind | `.sb-grid--blog` duplicates product 1-up/2-up rules; stale `.sb-grid` comment | low | Independent modifiers are intentional; comment is pre-existing | reject |
| 13 | blind | Unused `.sb-blog` class on `<main>` | low | Harmless hook for 5.2/Epic 7 | reject |
| 14 | edge | Seed on an empty site gets SortOrder 0 (start page) | low | Site B always has pages before this seed runs | reject |
| 15 | edge | `PrimaryImage` with deleted media renders a broken `<img>` | low | Same pattern as `_ProductCard`; unlikely | reject |
| 16 | edge | Pager builds `//page/2` if the archive is the start page | low | Blog is never the start page; same in `ProductArchive` | reject |
| 17 | blind | Missing tests: out-of-range page, draft preview | low | Out-of-range folds into #6; draft preview is Piranha's loader | reject |

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test --artifacts-path tests/TbTruongHoc.Web.Tests/obj/art` (repo root) -- expected: all pass.
- `dotnet build` -- expected: 0 new warnings.

**Manual checks:**
- In the Manager, add 2 posts to Blog on Site B (one with an image) and publish them. At 375px and 1280px the cards are fully tappable, there is no sideways scroll and the dates are muted. Unpublish both and the empty message shows.
