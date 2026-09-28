---
title: 'Story 2.5: Craftsman story page & trust-block'
type: 'feature'
created: '2026-09-28'
status: 'done'
baseline_commit: '65837cdd74167105b6314d343c88ad6ecef667b9'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Site B has no page for the craftsman Phạm Trí Trong, and no inline trust signal where a buyer decides. The epic's thesis is to "sell trust before product", but right now nothing carries that trust.

**Approach:**
- Add a standalone `CraftsmanStoryPage` page type (AD-2) holding:
  - photos, each with a caption
  - an optional video with an optional captions track
  - Piranha blocks for the prose
  - the short quote used by the trust-block
- The page shows the workshop address and Maps link from `SiteSettings`.
- A shared `_TrustBlock` partial finds the site's published story page and renders its quote plus a link to it. The block is embedded on the Trống hub, every `ProductArchive` page (the 5 Trống subcategories, Thùng rượu gỗ and Bồn tắm gỗ) and every PDP. This satisfies both the trust-block AC and the "linked from all 3 product lines" AC. The Site B footer also links to the story page.

## Boundaries & Constraints

**Always:**
- Everything is CMS-driven. Blank fields render nothing: no label, placeholder or empty box.
- **Story lookup:** the site's first `CraftsmanStoryPage` in sitemap order that is published and not future-dated. Hidden-from-nav pages count. None found → no trust-block and no footer link anywhere. The lookup is per site (Site A never shows it).
- **Trust-block:** `surface-sunken`, `border`, `radius-md`; quote in `body`, attribution in `caption`/muted, then a link "Câu chuyện nghệ nhân →" with an accessible name. The quote and attribution render only when both are non-blank. Otherwise the block is the link alone, in the same box. Text is HTML-encoded.
- **Placement:** on a PDP, directly before the quote form. On a hub or archive page, after the tile/product grid (after the pager) or after the blocks when there is no grid.
- **Story page order:** eyebrow (optional field) → h1 heading-lg → photos → video (whenever a video file is set) → blocks → workshop section (Address + Maps link through `ContactLinks.SafeUrl`, omitted when both are unset).
- **Photo decision (2026-09-28, Phước: option A, vertical grid).** Every photo is visible as a `<figure>`, `rounded-lg`, with its caption line under it (`caption`, muted). Layout is 1-up on mobile and 2-up from 768px. There is no JS gallery, no auto-advance and no lightbox or modal. Alt text = media `AltText` → caption → `"{Title} – ảnh {n}"`.
- **Video decision (2026-09-28, Phước: option A, self-hosted).** An mp4 is uploaded to the Media library and rendered as `<video controls preload="none">`. No autoplay, and no YouTube or any other third-party embed.
- **Captions decision (2026-09-28, Phước, renegotiated after build): the `.vtt` file is optional.** A video file alone renders the player. A `.vtt` file, when set, adds `<track kind="captions" srclang="vi" default>`; any other document in that field is ignored. Phước accepted the trade-off: a spoken video without captions fails WCAG 1.2.2, which overrides EXPERIENCE.md's "captions are a condition of shipping video" rule.
- **Seeding:** seed a draft story page on Site B (slug `cau-chuyen-nghe-nhan`, title "Câu chuyện nghệ nhân", attribution "Nghệ nhân Phạm Trí Trong"), idempotent per slug, following `ProductLineSeed`. No prose, quote or photos: that is the client's content.
- Mộc Trầm tokens only. Only the Site B views change, plus `Program.cs` for the `.vtt` media type and the seed call.

**Never:**
- A "video coming soon" box, an empty player or an iframe embed. Auto-rotating carousels, modal-on-modal, lorem ipsum, invented quotes.
- Changing `_QuoteRequestForm`, `lead-form.js`, `_ProductCard`, Site A views, `_Layout.cshtml` or `Areas/Manager`.
- A trust-block or story link on Site A. A breadcrumb, or a product-line tile strip on the story page.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| No story page | Site B, story page draft/absent | PDP/archive/hub/footer: no `sb-trust` and no story link | N/A |
| Story, quote set | Published, quote + attribution | Trust-block with `<blockquote>`, attribution and link on PDP, archive and hub; the footer link is present | Values encoded |
| Story, no quote | Published, quote blank | Trust-block = the link only, no `<blockquote>` | N/A |
| Bare story page | Title only | h1, no `<img>`, `<video>` or workshop section | N/A |
| Photos | 2 photos, one caption, one blank item | 2 `<figure>`s in order, one `<figcaption>`, alts per the rule | Blank item skipped |
| Video without captions | Video set, captions blank (or a non-.vtt document) | `<video controls preload="none">`, no `<track>`, no autoplay | Non-.vtt ignored |
| Video + captions | Both set | `<video controls preload="none">` + `<track kind="captions">`, no autoplay | N/A |
| Site A | Any page | No trust-block, no story link | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Models/ProductHubPage.cs` / `ProductArchive.cs` -- pattern for the new `Models/CraftsmanStoryPage.cs`: `[PageType(Title = "Câu chuyện nghệ nhân")]`, `[ContentTypeRoute(Route = "/craftsmanstory")]`. Regions:
  - `Eyebrow` (`StringField`)
  - `Photos`: `IList<StoryPhoto>`, a region class with `Image` (`ImageField`) and `Caption` (`StringField`)
  - `Video` (`VideoField`) and `VideoCaptions` (`DocumentField`, optional, .vtt only)
  - `Quote` (`TextField`) and `QuoteAttribution` (`StringField`)
  - Trimmed-null helpers in the style of `ProductPost.PriceText`/`GalleryImages`, which skip items whose `Media == null`.
- `src/TbTruongHoc.Web/Controllers/CmsController.cs` -- add a `[Route("craftsmanstory")]` action, following `ProductHub`.
- `src/TbTruongHoc.Web/Services/ProductCatalog.cs` -- style reference. Add `Services/CraftsmanStory.cs` (scoped, registered next to `ProductCatalog` at `Program.cs:80`) with `GetPublishedAsync(Guid siteId)`, which returns `(Permalink, Quote, Attribution)` or null. Walk `GetSitemapAsync(siteId)` (published only) and check the type through `PageInfo.TypeId`, as `GetHubTilesAsync` does.
- `src/TbTruongHoc.Web/Views/Cms/ProductPost.cshtml` -- insert `_TrustBlock` before `<div class="container sb-pdp__form">`. `ProductArchive.cshtml` / `ProductHub.cshtml` -- insert it at the end of `<main>`.
- `src/TbTruongHoc.Web/Views/Shared/_SiteBFooter.cshtml` -- the story link. Views get the site id from `WebApp.Site.Id` and inject the service with `@inject`.
- `src/TbTruongHoc.Web/Program.cs:232` -- register `.vtt` (`text/vtt`) in `App.MediaTypes.Documents` before `ContentTypeBuilder`. Piranha 12 registers `.mp4` but not `.vtt`. Check that `/uploads/*.vtt` is served (static files content-type provider). After line 277, add `CraftsmanStorySeed.EnsureSeededAsync` in `Data/CraftsmanStorySeed.cs`, a copy of the `ProductLineSeed` pattern.
- `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css` -- add `.sb-story*` and `.sb-trust*` near `.sb-spec` (~line 907), plus a footer link style near `.sb-footer` (~line 941).
- `tests/TbTruongHoc.Web.Tests/ProductDetailPageTests.cs`, `ProductCatalogTests.cs` -- reuse `CatalogBuilder`, the MediaBuilder PNG upload, `GetHtmlAsync`, `Decode`, `Section`, and the per-test DI scopes (see the 2.4 notes). Shared dev DB: create the story page per test and delete it in `finally`. Tests must not depend on the seeded draft.

## Tasks & Acceptance

**Execution:**
- [x] `Models/CraftsmanStoryPage.cs` -- type, regions and helpers.
- [x] `Services/CraftsmanStory.cs` + `Program.cs` -- lookup service, DI, `.vtt` type, seed call.
- [x] `Data/CraftsmanStorySeed.cs` -- draft seed.
- [x] `Controllers/CmsController.cs` + `Views/Cms/CraftsmanStory.cshtml` -- story page per the order and rules above, with `_MetaTags`, and `ImageAltFallback` set/removed around the block loop as in the PDP.
- [x] `Views/Shared/_TrustBlock.cshtml` + the 3 Cms views + `_SiteBFooter.cshtml` -- embed.
- [x] `site-b.css` -- styles, mobile-first.
- [x] `tests/TbTruongHoc.Web.Tests/CraftsmanStoryTests.cs` -- one test per matrix row, a seed idempotency test, and the page-wide AC check on every render.

**Acceptance Criteria:**
- Given any story page or trust-block render, then every `<img>` has a non-empty alt, and there is no `autoplay`, `<iframe` or "Đặt mua".
- Given the full suite, when run, then all pre-existing tests pass.

## Implementation Notes

- `.vtt` needed a `StaticFileOptions` content-type mapping in `Program.cs` too: ASP.NET Core 404s unknown extensions. The test fails without it.
- Trust-block attribution uses `on-surface` at caption size rather than muted: muted on surface-sunken is ~4.25:1, below AA (same call as the 2.4 spec labels).
- `CraftsmanStory` returns null for any site other than Site B (InternalId guard), memoized per request.
- Tests temporarily unpublish any published story page on Site B and restore it in `finally`.

## Spec Change Log

- **Post-build renegotiation (2026-09-28).**
  - **Trigger:** Phước asked for the `.vtt` to be optional (it was a hard requirement for showing the video).
  - **Amended:** frozen captions decision, story-page order line, matrix row "Video without captions", Code Map, manual check.
  - **Code:** `HasCaptionedVideo` split into `HasVideo` + `HasVideoCaptions`; the `<track>` is conditional; the 2 no-caption tests now expect a player without a track.
- **Walkthrough finding (2026-09-28): video upload limit.**
  - **Trigger:** a 0.3 GB mp4 failed to upload in the Manager ("Server responded with 0 code"). Kestrel caps request bodies at ~28.6 MB and multipart forms at 128 MB, so any real self-hosted video was blocked.
  - **Decision (Phước):** raise the limit to 1 GB for the Manager media upload only (`POST /manager/api/media/upload`); every other request, including the public lead forms, keeps the defaults.
  - **Code:** `Services/MediaUploadLimits.cs` + an `app.Use` before `UsePiranha`; the Video region description recommends 720p (~20–60 MB); `MediaUploadLimitsTests`.
  - **Deploy note:** a reverse proxy or IIS in front of the app has its own limit (nginx `client_max_body_size`, IIS `maxAllowedContentLength`) that must also allow 1 GB.

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V).

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| B1/E5 | Lookup loads `PageInfo` for every sitemap item on every Site B request (footer) | low | Real; memory cache softens it. `SitemapItem.PageTypeName` holds the type Title → patch (prefilter by the type's title from `App.PageTypes`). |
| B2/E1 | Any document (PDF…) passes as captions, so the video shows uncaptioned | medium | `HasCaptionedVideo` checks only `Media != null`; `DocumentField` accepts PDF/DOCX → patch (require `.vtt`). |
| B3 | Maps link `aria-label` doesn't contain the visible "Xem bản đồ" (WCAG 2.5.3) | low | Confirmed in the view → patch. |
| B4/E2 | Renaming the seeded slug re-seeds a duplicate draft | low | Same accepted behavior as `ProductLineSeed` (2.3); the fix adds a type scan → rejected. |
| E3 | Slug held by another page type → seed silently skipped | low | Unlikely; guard adds a branch → rejected. |
| E4 | Faulted lookup memoized → footer 500s | false | A DB failure already fails the page load itself; the memo doesn't change the outcome. |
| E6 | Story nested under an unpublished parent is not found | low | Unlikely; guard adds complexity → rejected. |
| E7 | `StaticFileOptions` override discards other mappings | false | The only configurer in `src/` (grep). |
| B5/E8 | `CraftsmanStoryPage` can be created on Site A | low | Unlikely editor action; the guard adds a branch → rejected. |
| B6/V-other | Tests unpublish real Site B story pages; a crash leaves them unpublished | low | Local dev DB only; restored in `finally` → rejected. |
| B7 | `.vtt` serving not tested through a real Media upload; the test writes to wwwroot | low | Test host stores uploads elsewhere; the manual check covers it → rejected. |
| B8 | Video has no poster, dark box until played | low | Standard native player; a poster field adds surface → rejected, surfaced at presentation. |
| B9 | Story photos have no `srcset` | low | Same single-size pattern as the PDP; adds complexity → rejected. |
| B10 | No length guidance on the quote | low | Region-description wording only → patch. |
| B11 | The seeded page joins the main nav once published | low | EXPERIENCE.md IA lists only product-line pages + footer as entry points → patch (`IsHidden = true` in the seed). |
| B12 | `_TrustBlock` comment says "Trống hub" but it renders on every product hub | low | Direct correction → patch. |
| B13 | Diff excludes the spec and sprint-status | false | Intentional: the reviewed diff covers `src`/`tests` only. |
| B14 | Awkward re-wrap in the `ProductPost.cshtml` header comment | low | Direct correction → patch. |
| V1 | Trust-block on an empty archive or tile-less hub is never tested | low | Pre-verified → patch (test). |
| V2 | Lookup of a nested story page is untested | low | Pre-verified → patch (test with `ParentId`). |

## Verification

**Commands:**
- `dotnet test --artifacts-path bin/test-art` (repo root, MariaDB up) -- expected: all tests pass.
- `dotnet build` -- expected: 0 new warnings.

**Manual checks:**
- In the Manager, publish the seeded story page with 2 photos, a quote and a video with a .vtt file. Check at 375px and 1280px: the story page, the trust-block on a Trống subcategory page and a PDP, and the footer link. Remove the .vtt and confirm the video still plays, without captions.
