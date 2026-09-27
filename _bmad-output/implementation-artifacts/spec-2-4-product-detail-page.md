---
title: 'Story 2.4: Product detail page per model'
type: 'feature'
created: '2026-09-27'
status: 'done'
baseline_commit: 'b6fbdd79629b1d5889f07f3ff5b5b863387bb5de'
route: 'dispatch'
review_loop_iteration: 1
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A product card links to Story 2.2's minimal `ProductPost` page, which has only a title, one image, an excerpt, a price and the form. A visitor can't see a model's gallery, SKU or specs, so the page isn't a real per-model product page.

**Approach:** Extend `ProductPost` with CMS fields for a SKU, extra photos and spec rows (label/value). Replace `ProductPost.cshtml` with the DESIGN.md `product-detail-page`, in this order: gallery, category eyebrow, title, "Mã: …", excerpt, price row, spec rows, content blocks, the prefilled quote form. Build the spec-row style as a reusable class that Epic 4's `drum-config-reference` can reuse.

## Boundaries & Constraints

**Always:**
- Everything is CMS-driven. Empty fields render nothing, with no label, placeholder or empty container.
- Gallery = `PrimaryImage` (if set) followed by the new photo list, skipping empty items. 0 photos → no gallery element. 1 photo → one framed image with no controls or indicators. It never auto-advances.
- Alt text = the media's `AltText` if set, otherwise `"{Title} – ảnh {n}"`, on every `<img>`.
- Eyebrow = the owning archive's title (same source as the card).
- The price row follows the card rule exactly: editor text, HTML-encoded, or muted "Liên hệ báo giá". The quote form is prefilled with the title, truncated to 200 characters (existing behavior).
- **Gallery decision (2026-09-27, Phước): main photo + thumbnail row** for 2+ photos. The thumbnails are `<a href="{large image url}">` links with `aria-label="Xem ảnh {n}"`, so without JS a tap opens the image. The new `sb-gallery.js` intercepts the tap and swaps the main `<img>` src/alt in place. It marks the active thumbnail with `aria-current="true"` and a primary ring. Thumbnails are ≥44px tap targets in a row that wraps. There are no arrows and nothing runs automatically.
- **Block-image alt decision (2026-09-27, Phước, option A): fix the shared block templates.**
  - `DisplayTemplates/ImageBlock` and `ImageGalleryBlock` output alt from the first non-blank of: the media's `AltText`, `ViewData["ImageAltFallback"]`, then the media's `Title`. The PDP sets `ImageAltFallback` to the product title.
  - `PostBlock`/`PageBlock` card images use the linked post's or page's title as alt.
  - This is the one sanctioned exception to "don't touch Site A views". Site A pages get the same alt behavior.
- Mộc Trầm tokens only: gallery on surface with a border and `radius-md`. Spec rows on surface-sunken with `body-sm`. SKU is caption/muted. Title is heading-lg (24/700).

**Never:**
- Trust-block and story-page links (2.5 wires the trust-block in when it lands). Landing-page pricing (Epic 3). The drum-config table itself (Epic 4).
- "Đặt mua ngay", cart, fabricated prices or specs, lorem ipsum, gradient or placeholder tiles for missing photos, auto-rotating carousels, modal/lightbox.
- Changing `_ProductCard`, `ProductArchive`, `_QuoteRequestForm`, `lead-form.js`, the Site A views, `_Layout.cshtml`, or `Areas/Manager`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Bare post | Title only | Eyebrow, h1, muted "Liên hệ báo giá", form. No `<img>`, no gallery, no SKU, no spec list | N/A |
| One photo | PrimaryImage only | Single framed `<img>` with alt text. No gallery controls or indicators | N/A |
| Many photos | PrimaryImage + 2 list photos, one list item empty | Main `<img>` = photo 1. 3 thumbnail links in order, each with alt text; the first is `aria-current` | Empty item skipped |
| Full data | SKU "TC-L2-160", 2 spec rows, price "từ 5 triệu" | "Mã: TC-L2-160" near the h1, 2 label/value rows, price shown in price style | Values HTML-encoded |
| Spec row half-empty | Row with a label but a blank value | Row omitted | N/A |
| Image block on PDP | ImageBlock (media without AltText) + PostBlock in `Blocks` | Block `<img>` alt = product title; PostBlock `<img>` alt = the linked post's title | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Models/ProductPost.cs` -- add regions next to `Price`: `Sku` (`StringField`, "Mã sản phẩm"), `Photos` (`IList<ImageField>`, "Ảnh sản phẩm", with a `Description` telling editors the card/share image is "Ảnh chính" and these photos show after it) and `Specs` (`IList<ProductSpecRow>`, "Thông số", `ListTitle = "Label"`, with a `Description` noting that a row needs both a label and a value to show). The new region class `ProductSpecRow` has `Label` and `Value` `StringField`s. Add trimmed-null helpers in the style of `PriceText`. Regions are additive; `ContentTypeBuilder...Build().DeleteOrphans()` in `Program.cs:232` updates the type on startup, with no migration.
- `src/TbTruongHoc.Web/Views/Cms/ProductPost.cshtml` -- rewrite. Load the eyebrow the way `Views/Cms/Post.cshtml:5` does (`WebApp.Api.Pages.GetByIdAsync(Model.BlogId)`, null-safe). Keep `_MetaTags`, `ViewData["Title"]`, the form partial and the `lead-form.js` include. Render `Model.Blocks` with the `ProductArchive.cshtml:33-40` loop. Image URLs use `WebApp.Media.ResizeImage(field, w)`; `field.Media?.AltText` supplies the alt text.
- `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css:780-795` -- replace the "Minimal product page" block with `.sb-pdp*` (gallery, header, SKU) and the reusable `.sb-spec` / `.sb-spec__row`. Keep `.sb-product__price(--contact)` shared with the card (lines 656-672), or rename both selectors consistently.
- `tests/TbTruongHoc.Web.Tests/ProductCatalogTests.cs:217-238` -- the existing PDP test asserts the old `<h1>` and "no `<img>` in main". Update or replace it. Reuse `WithCatalogAsync`, `PostAsync`, `GetHtmlAsync`, `Decode` and `Section`.
- `src/TbTruongHoc.Web/Views/Cms/DisplayTemplates/{ImageBlock,ImageGalleryBlock,PostBlock,PageBlock}.cshtml` -- the only `<img>` tags without alt (`Comment.cshtml` already has one). Add alt per the block-image decision. `ImageBlock`'s model is the field itself (`Model.Media`); gallery items are `((ImageBlock)item).Body.Media`. Keep the markup otherwise unchanged.
- Media in tests: upload a tiny PNG via `api.Media.SaveAsync(new StreamMediaContent{...})` and delete it in cleanup. Check `PerPageSeoFieldsTests`/`SiteSettingsTests` for an existing helper first.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Models/ProductPost.cs` -- add `Sku`, `Photos`, `Specs` + `ProductSpecRow` and the helpers `SkuText`, `GalleryImages` (PrimaryImage first, then non-empty photos) and `SpecRows` (both fields non-blank).
- [x] `src/TbTruongHoc.Web/Views/Cms/ProductPost.cshtml` -- the PDP layout per Intent/Boundaries and the gallery decision.
- [x] `src/TbTruongHoc.Web/wwwroot/assets/js/sb-gallery.js` -- a small IIFE in the style of `site-b-nav.js`. It handles thumbnail clicks per `[data-sb-gallery]`, swaps the main image src/alt, and moves `aria-current`. Include it only when there are 2+ photos.
- [x] `src/TbTruongHoc.Web/Views/Cms/DisplayTemplates/*.cshtml` -- alt on the 4 image-bearing block templates, per the block-image decision. `ProductPost.cshtml` sets `ViewData["ImageAltFallback"] = Model.Title` before the block loop.
- [x] `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css` -- the `.sb-pdp*` and `.sb-spec*` styles, mobile-first, with a 2-column layout (gallery | details) at ≥992px optional.
- [x] `tests/TbTruongHoc.Web.Tests/ProductDetailPageTests.cs` -- one test per matrix row, plus the AC checks below. Move the old 2.2 PDP test's still-valid assertions here and delete it from `ProductCatalogTests`. Add a `sb-gallery.js` script test that mirrors `SiteBNavScriptTests`, including an `altKey` modified-click case. Also:
  - Assert `sb-pdp__layout--gallery` is present with 1 or more photos and absent on a bare post.
  - Call the page-wide AC check in every PDP render test.
  - Clean up media in a `finally` that runs even if catalog cleanup throws.

**Acceptance Criteria:**
- Given any PDP, when it renders, then there is no "Đặt mua", `<form action`, cart link, or image without a non-empty `alt`.
- Given a PDP, when the form is submitted, then the lead's `productOfInterest` is the model title (existing pipeline, unchanged).
- Given the full suite, when run, then all pre-existing tests pass.

## Implementation Notes

Iteration 0 (reverted for the block-alt intent gap). Carry these forward:

- `GalleryImages` also skips an image whose media record is gone (`Media == null`), so a deleted upload never renders a broken `<img>`.
- Spec labels use `on-surface` (600), not muted: muted on surface-sunken is ~4.25:1, below AA for 14px.
- `ProductCatalogTests.CatalogBuilder` and its `Decode`/`Section`/`GetSiteAsync`/`HostnameOf` helpers became `internal` so `ProductDetailPageTests` reuses them.
- `ProductLineTests` (2.3) asserted the old bare `<h1>` on variant PDPs; updated to `<h1 class="sb-pdp__title">`.
- Main image carries the media's original width/height; `sb-gallery.js` swaps them from the thumbnail's `data-width`/`data-height`.

Iteration 1:

- The `ImageBlock` template's model is the block, so its media is `Model.Body.Media` (the Code Map said `Model.Media`).
- The block test uploads a 220×90 PNG, because the shared gallery and PostBlock templates crop to a fixed size and throw on a 1×1 source. This is pre-existing behavior and out of scope.
- `UpdatePostAsync` and the media cleanup each run in their own DI scope. A long-lived scope left stale block entries, and Piranha's delete then threw a concurrency error.
- On Site A, an image block whose media has neither alt text nor a title renders `alt=""`. This follows the decision chain, which has no page-title fallback there.

## Spec Change Log

- **Iteration 1 (intent_gap E1/E2).**
  - **Trigger:** the PDP block loop rendered shared image templates with no alt, which broke AC 1.
  - **Amended:** the human chose option A (frozen decision and matrix row). The Code Map and a task now cover the 4 DisplayTemplates. The pass-1 patch items are folded into the tasks and region descriptions.
  - **Avoids:** a PDP with an image block shipping `<img>` without alt.
  - **KEEP:** the iteration-0 design passed all 218 tests and should be re-derived the same way. That covers the `GalleryImages`/`SpecRows`/`SkuText` helpers, the `.sb-pdp*`/`.sb-spec*` CSS, `sb-gallery.js` with the modified-click passthrough, internal `ProductCatalogTests` helpers, the MediaBuilder tiny-PNG uploads, and the `ProductLineTests` `<h1 class="sb-pdp__title">` update.

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V). Code reverted for the intent_gap loopback. Iteration-0 diff saved in the session scratchpad as `story-2-4-iter0.patch`.

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| E1/E2 | The PDP block loop renders shared `ImageBlock`/`ImageGalleryBlock`/`PostBlock`/`PageBlock` templates, which emit `<img>` with no `alt` | medium | Confirmed: `DisplayTemplates/ImageBlock.cshtml` has no alt. Piranha 12 has no per-post-type block allow-list (only `UseBlocks`). This breaks the "every img has alt" rule whenever an editor adds an image block. Fix choice not settled by the spec → **intent_gap**. |
| B1 | Photos-only product (no PrimaryImage) → PDP gallery shows, but the card and og:image stay image-less | low | `_ProductCard`/`_MetaTags` read `PrimaryImage` only, and are out of bounds. → patch: region description telling editors the first photo goes in "Ảnh chính". |
| B13 | Editors get no hint that half-filled spec rows are dropped | low | → patch: region description on "Thông số". |
| V1/B6 | `altKey` / `defaultPrevented` early return is untested in `SbGalleryScriptTests` | low | Pre-verified. → patch: add the `{ altKey: true }` InlineData. |
| V2 | `sb-pdp__layout--gallery` is never asserted | low | Pre-verified. → patch: assert it is present for 1 and many photos, absent for a bare post. |
| B7 | Two spec-row tests skip `AssertPageWideRules` | low | Direct addition → patch. |
| B8/E5 | Media cleanup is skipped if catalog cleanup throws | low | Test-only, try/finally → patch. |
| B2 | Eyebrow isn't a link and there is no breadcrumb | false | DESIGN.md defines the eyebrow as a `label`. The nav covers navigation. |
| B3 | Eyebrow lookup loads the full page; the null-archive case is untested | low | Same pattern as `Post.cshtml`; the null path is guarded → rejected. |
| B4/B5 | No tests for photos-only-without-primary or for media alt on the main image | low | Covered by the shared `GalleryImages`/`AltFor` paths → rejected. |
| B9 | A thumbnail swap is not announced to screen readers | low | `aria-current` moves and the main image alt updates; a live region adds complexity → rejected. |
| B10 | Thumbnails have no hover or focus-visible style | low | The global `.site-b :focus-visible` outline applies; hover-only cues are banned → rejected. |
| B11 | The alt fallback differs from `_ProductCard` | low | `_ProductCard` is out of bounds (Never) → rejected. |
| B12 | No schema.org Product JSON-LD | false | Not in the intent or story ACs; not a defect. |
| E3 | Empty block → empty wrapper div | low | Same as the existing archive/page loops; the rule targets fields → rejected. |
| E4 | `sbGallery.init` called twice attaches duplicate listeners | false | Only `start()` calls `init`, once per root. |

Pass 2 (iteration 1 diff):

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| E1/B2 | PrimaryImage repeated in Photos renders the same photo twice | low | `GalleryImages` has no dedupe, and the region description invites mixing the two → patch (skip already-listed media Ids). |
| B1 | Swapping photos with different aspect ratios shifts the thumbnail row | medium | Main img is `height:auto` and the script swaps width/height → patch (fixed 4:3 contain frame for the multi-photo main image). |
| B8 | `:active` shadow hides the aria-current ring | low | CSS cascade → patch. |
| B10 | `ImageAltFallback` stays set after the block loop | low | It is set in `ProductPost.cshtml` and never cleared → patch (remove after the loop). |
| B11 | Main gallery image has no priority hint | low | Largest above-the-fold image; CWV must not regress → patch (`fetchpriority="high"`). |
| V1 | Gallery JS data-hooks are not asserted on the rendered view | low | Pre-verified → patch. |
| V2/B5 | PageBlock alt untested | low | Pre-verified → patch. |
| B4 | `<form action` check only catches `action` as the first attribute | low | Test-only → patch (regex). |
| B6 | Excerpt text and SKU/price encoding not asserted | low | → patch. |
| B7 | `defaultPrevented` early return untested | low | → patch (InlineData). |
| V3 | Media-Title fallback and off-PDP block alt untested | low | Pre-verified. Site A-facing, outside PDP ACs → defer (filed disposition). |
| E2/E3 | Editor-typed `<img>` without alt inside Html/Text/Markdown blocks | low | Real, but editor-authored markup. TinyMCE's image dialog carries a description field; sanitizing rich text is out of proportion → rejected, surfaced at presentation. |
| E4 | `sbGallery.init` twice duplicates listeners | false | carried — only `start()` calls `init`, once per root. |
| B3 | Thumbnail `aria-label="Xem ảnh n"` hides the image alt | low | The label text is fixed by the frozen gallery decision; the fix is a spec edit → rejected. |
| B9 | Alt rule written in 3 places | low | The PDP's per-index fallback differs by design; a shared helper is a refactor → rejected. |

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test --artifacts-path tests/TbTruongHoc.Web.Tests/obj/art` (repo root) -- expected: all tests pass.
- `dotnet build` -- expected: 0 new warnings.

**Manual checks:**
- In the Manager on Site B, open a Thùng rượu variant, add a SKU, 2 photos and 2 spec rows, then publish. Check at 375px and 1280px: the gallery leads, the SKU is near the title, the spec rows are on sunken bands, and the form is prefilled. With 1 photo, no controls appear.
