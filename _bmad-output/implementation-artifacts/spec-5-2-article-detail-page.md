---
title: 'Story 5.2: Article detail page'
type: 'feature'
created: '2026-09-29'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '94262f6dff83baa6381d8ac5afa0db19e0b4ce81'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-5-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The 5.1 `BlogPost` page is only a title, a date and blocks. It has no breadcrumb, no readable article layout, and a dead end at the bottom. Readers finish an article with nowhere to go (FR-14, `article-detail-page`).

**Approach:** Enrich the site-agnostic `BlogPost` view:
- a breadcrumb "Trang chủ / Blog / [title]"
- a width-limited reading column with styled in-article subheads
- an end module: a "Bài viết liên quan" strip of 2–3 smaller blog cards when related posts exist, otherwise a plain "← Quay lại Blog" link to the listing

## Boundaries & Constraints

**Always:**
- Views and types stay site-agnostic, so Epic 7 reuses them.
- Related posts come from the same archive, are published only, exclude the current post, and are newest first, with at most 3.
- Related cards reuse `_BlogCard` and `.sb-card` at a smaller scale. The card title drops to `h3` under the module's `h2`.
- The back-link and the Blog breadcrumb item both point to the parent archive's permalink, and the link text uses the archive's title ("Blog").
- Tokens:
  - title: `heading-lg`
  - date: `caption`, `on-surface-muted`
  - body: `body`
  - `h2`/`h3` in the body: `heading-md`/`heading-sm`
  - module label: `label` typography in `secondary`
  - back-link: `primary`
- On desktop the reading column is width-limited (about 720px). Tap targets are ≥44px. There are no hover-only affordances.
- Keep the 5.1 behaviour: `_MetaTags`, the `ConfigBlock` skip, `ImageAltFallback`, and 404 for unpublished posts.
- Decision: "related" means the post shares **at least one Piranha tag** with the current post. A post with no tags always shows the back-link. Category plays no part.
- Decision: the metadata line shows the **date only** (`dd/MM/yyyy`). No author field.

**Never:**
- No invented or hardcoded article copy.
- No automated cross-site links.
- No like/view counters, share counters, carousels or account UI.
- No empty related-posts shell.
- No changes to `BlogArchive` listing behaviour or to the product types.
- No hand-rolled published/date filtering that duplicates Piranha's archive loader.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Related exist | Post A plus 4 other published posts that qualify as related | Module "Bài viết liên quan" with the 3 newest as cards; no back-link | N/A |
| No related | Post with no qualifying posts | No module markup at all; "← Quay lại Blog" links to the archive permalink | N/A |
| Only drafts qualify | Qualifying posts are unpublished | Treated as no related: back-link shown | N/A |
| Self excluded | Post A qualifies against itself | A never appears in its own strip | N/A |
| Breadcrumb | Any published post | `nav` with "Trang chủ" → `/`, "Blog" → archive, current title with `aria-current="page"` | N/A |
| Subheads | Body HTML block with `<h2>`/`<h3>` | Rendered inside the reading column with the heading-md/sm styles | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Views/Cms/BlogPost.cshtml` -- the 5.1 minimal view to enrich. It already has `_MetaTags`, the `ConfigBlock` skip and `ImageAltFallback`. Keep all of that.
- `src/TbTruongHoc.Web/Controllers/CmsController.cs:198-217` -- the `BlogPost` action. Load the related posts here and put them on the model; the view should not call the API.
- `src/TbTruongHoc.Web/Models/BlogPost.cs` -- add a non-persisted `IReadOnlyList<BlogPost> Related` property, the same way `BlogArchive.Archive` is non-persisted. No new regions.
- Related loading: call `_api.Archives.GetByIdAsync<BlogPost>(post.BlogId, 1, null, tagId, null, null, 4)` once per tag on the post. It filters to published posts natively. Then merge, de-duplicate by Id, drop self, sort by `Published` descending and take 3. `post.Tags` is a list of `Taxonomy` with `Id`. No tags means no calls.
- Archive title and permalink: `_api.Pages.GetByIdAsync(post.BlogId)`. `ProductPost.cshtml:26` does this for its eyebrow.
- `src/TbTruongHoc.Web/Views/Shared/_BlogCard.cshtml` -- the card title is a hardcoded `h2`. Make the heading level switchable (e.g. `ViewData["CardHeadingTag"]`, default `h2`) so the listing is unchanged.
- `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css` -- `.sb-article*` (~819-830), `.sb-grid--blog` (567-575), `.sb-card__date` (794). Add breadcrumb, reading column, body subhead, related and back-link rules here, using existing `--sb-*` tokens only.
- `tests/TbTruongHoc.Web.Tests/BlogListingTests.cs` -- `BlogBuilder.PostAsync` (419) sets `Category = "General"` and has no tags. Extend it with tags/category, and reuse `WithBlogAsync` and `GetAsync`.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Models/BlogPost.cs` -- add a non-persisted `Related` list.
- [x] `src/TbTruongHoc.Web/Controllers/CmsController.cs` -- in the `BlogPost` action, load the shared-tag related posts and the parent archive info.
- [x] `src/TbTruongHoc.Web/Views/Cms/BlogPost.cshtml` -- breadcrumb, header (title and metadata line), body inside the reading column, then the related module or the back-link.
- [x] `src/TbTruongHoc.Web/Views/Shared/_BlogCard.cshtml` -- switchable heading level, default `h2`.
- [x] `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css` -- styles for the breadcrumb, reading column, subheads, related strip (smaller cards: 1-up below 480px, 2-up, 3-up at ≥768px) and back-link.
- [x] `tests/TbTruongHoc.Web.Tests/ArticleDetailTests.cs` -- one test per matrix row. Also test that listing cards still render `h2`.

**Acceptance Criteria:**
- Given a published BlogPost, when it renders, then the title is the only `h1`, the metadata line is muted, and the body runs in the reading column.
- Given the full suite, when run, then all pre-existing tests pass, including `BlogListingTests`.

## Implementation Notes

- Walkthrough patch (2026-09-30): the shared `ImageGalleryBlock` template hard-cropped every photo to 1100x450 and ignored the per-image `Aspect` setting. It now resizes through the `ImageBlock` overload (`Original` = no crop), and both site stylesheets frame slides at 16:9 with `object-fit: contain` over a blurred copy of the same photo (CSS background, same URL), with dark-chip controls. 16:9 was chosen after comparing 4:3/16:9 x white/blurred mocks; the owner will require editors to supply photos suited to 16:9. This affects every page with a gallery on both sites, not only blog posts.

## Spec Change Log

## Review Triage Log

| # | Source | Finding | Verdict | Evidence | Route |
|---|--------|---------|---------|----------|-------|
| 1 | verif-gap, blind | Multi-tag merge (per-tag loop, de-dup, global re-sort, cap) never tested | medium | Pre-verified: every test's current post has ≤1 tag; list-instead-of-dict / first-tag-only / no re-sort all pass today | patch |
| 2 | blind, edge | No tie-breaker on `Published` ordering | low | Equal timestamps (e.g. Epic 8 bulk import) make the top-3 cut depend on tag iteration order; one-line `ThenBy` fix | patch |
| 3 | blind | Article header/body lack an `<article>` element | low | Only the related cards use `<article>`; wrapping is a direct markup fix | patch |
| 4 | blind | "←" in back-link and CSS "/" separator are announced by screen readers | low | Glyphs are inside the link text and in `::before` content; `aria-hidden` span and `content: "/" / ""` are direct fixes | patch |
| 5 | blind | Related/ParentArchive written onto a possibly shared cached post instance | low | `UseMemoryCache()` does not clone, so the instance is shared, but every request writes identical values (not draft-dependent) and they are not regions, so saves ignore them. Same pattern as `BlogArchive.Archive`/`ProductArchive.Archive`. A view-model refactor is not a direct fix | reject |
| 6 | blind, edge | Missing parent archive → no breadcrumb item, no back-link (dead end) | low | `Pages.GetByIdAsync` does not filter by publish state, and deleting an archive cascades its posts, so a null parent is effectively unreachable. The fix would be a new guard | reject |
| 7 | edge | Unpublished parent archive → breadcrumb/back-link point to a 404 | low | Only when an editor deliberately unpublishes Blog while its posts stay live; guard adds a branch | reject |
| 8 | blind, edge | One archive query per tag, loading full posts | low | Posts carry a handful of tags; each query returns ≤4 posts, and post models come from Piranha's memory cache | reject |
| 9 | blind, edge | Strip shows 1 card; Intent says "2–3" | false | The frozen Always rule sets only a max ("at most 3"), and the epic AC shows the module whenever related posts exist. "2–3" is the mock's typical count, not a minimum | reject |
| 10 | edge | 1–2 related cards at ≥768px leave empty grid tracks | low | Same fixed-track behaviour as the listing grid; `auto-fit` would stretch a single card to 960px, which is worse | reject |
| 11 | blind | "Same archive" not tested | low | Tags are per-archive `Taxonomy` rows (own `BlogId`), and the query takes `post.BlogId`, so there is no cross-archive path to regress | reject |
| 12 | blind | No BreadcrumbList/Article JSON-LD | low | New SEO surface, beyond the story | reject |
| 13 | blind | Removed 5.1 `.sb-article > .container` padding rule not manually checked | false | `.sb-article` is used only by `BlogPost.cshtml`, whose layout this story replaces | reject |
| 14 | blind | Body test finds column end via `sb-backlink` marker | low | Test-only fragility; only bites if the builder gains default tags | reject |

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test --artifacts-path tests/TbTruongHoc.Web.Tests/obj/art` (repo root) -- expected: all pass.
- `dotnet build` -- expected: 0 new warnings.

**Manual checks:**
- In the Manager, create 3 Site B posts that share a tag, plus 1 with no tags, and publish them all. Check at 375px and 1280px:
  - The related posts show their strip.
  - The unrelated post shows "← Quay lại Blog".
  - The body column is narrow on desktop.
  - There is no sideways scroll.
