---
title: 'Story 6.4: Site A product detail page (shared PDP scaffold, shippable variant)'
type: 'feature'
created: '2026-10-01'
status: 'done'
baseline_commit: '569f756e20281b6bdc0f95ff8be3c49d67fb138c'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-tbtruonghoc-2026-09-18/mockups/key-chi-tiet-ship-thang.html'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** On Site A, a product (`ProductPost`) page renders Site B's PDP: `sb-` markup that Site A's layout has no styles for, an "Liên hệ báo giá" price line, and the Site B craftsman trust block. A Site A visitor gets no breadcrumb, no `price-block`, no shipping note and no Site A CTAs.

**Approach:** `CmsController.ProductPost` switches to a new `SiteAProductPost` view on Site A, the same way 6.3 does for the archive. The view builds the shared PDP scaffold (breadcrumb, gallery/info two-column layout, `price-block`, CTAs, `trust-badge` row, `photo-badge`, specs table) and the shippable variant (`shipping-note` + "Yêu cầu báo giá" opening the general form in an in-page modal, `formType=general`, product prefilled). 6.5 later adds the installation-required variant on top.

## Boundaries & Constraints

**Always:**
- Breadcrumb "Trang chủ / {hub} / {category} / {product}": the hub step uses 6.3's rule (published `ProductHubPage` parent of the category, else omitted), the category links to its permalink, and the product is plain text. It reuses `sa-breadcrumb` markup.
- Info column order: H1 (heading-lg) → "Mã: {sku}" if set → excerpt → `price-block` → `shipping-note` → CTA row → `trust-badge` row. The specs table "Thông số kỹ thuật" spans full width below the two columns, then the editor blocks (`ConfigBlock` skipped, as on 6.2/6.3). No Site B trust block.
- `price-block` (surface-tint/border-tint, rounded.lg): label "Giá bán". A price is set → the HTML-encoded free text in `price` 24/800 primary + note "Giá tham khảo — liên hệ để nhận báo giá ưu đãi khi đặt số lượng lớn." A blank price → "Liên hệ để nhận báo giá" in `price-fallback` 20/800 + note "Giá phụ thuộc số lượng và yêu cầu cụ thể — gửi yêu cầu để nhận báo giá." Same block and position in both states.
- `shipping-note` chip, exact text "Giao hàng toàn quốc — 3–7 ngày làm việc tùy khu vực", only on shippable products.
- CTA row: primary amber "Yêu cầu báo giá" (exact text). Then teal-outline "Gọi {phone}" (`tel:`) and "Zalo tư vấn" from `SiteSettings` via `ContactLinks`; each is omitted when unset. Each has an accessible name naming its destination.
- Trust chips and the photo badge are non-interactive `<span>`s with trust-bg/border/ink and rounded.full. A product (or category) with no trust claims has no row.
- Gallery: primary image + Photos as on Site B (`GalleryImages`, same alt rule: media AltText, else "{title} – ảnh n"). It reuses `sb-gallery.js` via its `data-sb-gallery*` hooks under `sa-pdp` classes. No photos → no gallery column, info goes full width, never a broken image.
- Layout 2 columns (gap 46px) at ≥1024px, stacked below that in the order gallery → info → specs. CTAs go full-width stacked below 768px.
- Contact data comes from `SiteSettings` only. Site A-only CMS fields carry a "(Site A)" title suffix and a "Site B không dùng trường này" description (the 6.2 precedent).
- **Decision (Q1):** the installation flag is **per product**: a `ProductPost` checkbox "Cần thi công (Site A)", default off (shippable). In 6.4 it only drops the shipping-note. 6.5 adds the rest.
- **Decision (Q2):** "Yêu cầu báo giá" is a `<button>` that opens an **in-page modal**: a native `<dialog>` holding `_QuoteRequestForm` (formType general, product prefilled). It has a title, a "Đóng" close button with an accessible label, closes on Esc and on a backdrop click, and focus returns to the CTA. Backdrop `rgba(18,56,50,.62)` with blur. Below 768px it is a full-screen bottom sheet. The success/error state stays inside the modal (lead-form.js). There is no no-JS fallback: the form already needs JS to submit. The opener is a generic `site-a` hook (`data-sa-modal-open="{dialog id}"`) so 6.5's survey modal can reuse it.
- **Decision (Q3):** a new product list "Ảnh thi công thực tế (Site A)" (`IList<ImageField>`). Its non-empty photos join the gallery after the regular ones, skipping media already shown, and only these photos carry the `photo-badge` "Hình ảnh thi công thực tế", on the main image and on the thumbnail. The badge follows the selected image (the main image's badge shows only while a genuine photo is selected).
- **Decision (Q4):** trust chips come from "Cam kết (Site A)", comma-separated (`SplitList`), on **both** the category and the product. A non-blank product value replaces the category's. At most 3 are shown.

**Never:**
- Change Site B's PDP output: `ProductPost.cshtml`, its partials, scripts and Site B CSS stay byte-identical, and every `ProductDetailPageTests` passes unchanged.
- Build the installation-required variant (service-area, process-strip, survey modal: 6.5), a cart/checkout, or hardcoded products/claims. "Giao hàng toàn quốc" never appears on an installation-required product.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Full shippable product | price "185.000đ / chiếc", sku, 2 photos, 3 specs | Breadcrumb 4 steps, gallery + thumbs, present price + bulk note, shipping-note, 3 CTAs, spec table | N/A |
| Blank price | Price "" | "Liên hệ để nhận báo giá" + its note in the same block | `<script>` in price/sku/spec encoded |
| No photos | no PrimaryImage/Photos | No gallery, no `<img>`; info full width | N/A |
| No phone / Zalo | SiteSettings blank | Only "Yêu cầu báo giá" | N/A |
| Category not under a hub | parent is StandardPage | "Trang chủ / {category} / {product}" | N/A |
| Installation-required product | "Cần thi công" ticked | No shipping-note and no "Giao hàng toàn quốc" anywhere; rest of scaffold unchanged | N/A |
| Trust chips | category "A, B", product "" / product "C" / 5 entries | A, B / C only / first 3 | Encoded |
| Genuine photos | 1 regular + 1 genuine (+ 1 genuine duplicating the regular) | 2 gallery images; badge only on the genuine one | N/A |
| Quote modal | click CTA, submit valid form | Dialog opens; lead stored formType general + product; success inside dialog | Failure keeps values in dialog |
| Site B product | any | Unchanged `sb-pdp` markup | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Controllers/CmsController.cs` l.168 `ProductPost`: after loading, `if (await IsSiteAAsync(model.SiteId))`. Load the category (`_api.Pages.GetByIdAsync<ProductArchive>(model.BlogId)`, which carries the Q1/Q4 fields) and its hub (`LoadParentHubAsync(category.ParentId)`, l.351). Return `View("SiteAProductPost", model)`.
- `src/TbTruongHoc.Web/Models/ProductPost.cs`: add non-region props for the controller-set category/hub (like `ProductArchive.ParentHub`), plus any Q1/Q3/Q4 per-product regions. Reuse `PriceText`, `SkuText`, `GalleryImages`, `SpecRows`.
- `src/TbTruongHoc.Web/Models/ProductArchive.cs`: Q1/Q4 category regions, following `Certifications`/`SplitList`.
- `src/TbTruongHoc.Web/Views/Cms/ProductPost.cshtml`: Site B. Don't modify; copy the gallery/alt/`LargeUrl`/blocks/`ImageAltFallback` structure from it.
- `src/TbTruongHoc.Web/Views/Cms/SiteAProductArchive.cshtml`: the breadcrumb markup. `Views/Shared/_SiteAHero.cshtml`: the `tel:`/Zalo via `ContactLinks.TelHref`/`SafeUrl` pattern.
- New `Views/Cms/SiteAProductPost.cshtml`.
- `src/TbTruongHoc.Web/wwwroot/assets/css/site-a.css`: tokens at the top, `.sa-btn--primary/--secondary` l.220, breadcrumb l.1289, quote form l.1534. Add `sa-pdp*`, `sa-price-block`, `sa-shipping-note`, `sa-trust-chip`, `sa-photo-badge`, `sa-spec-table`.
- `src/TbTruongHoc.Web/wwwroot/assets/js/sb-gallery.js`: don't modify. The Q3 badge-follows-selection needs a hook: either a `data-genuine` thumb attribute toggled by a small listener in `site-a.js`, or a Site A gallery script. Keep sb-gallery.js byte-identical either way.
- `src/TbTruongHoc.Web/wwwroot/assets/js/site-a.js` (already loaded by the Site A layout): add the generic `data-sa-modal-open` → `dialog.showModal()` opener plus backdrop-click close. `lead-form.js` (l.72, l.200) binds every `[data-quote-request-form]` and swaps in the success state; load it on the PDP.
- `tests/.../SiteAScriptTests.cs`, `SbGalleryScriptTests.cs`: the JS-test patterns for the modal opener and the badge toggle.
- `tests/TbTruongHoc.Web.Tests/SiteACategoryPageTests.cs`: Site A site/hub/archive/post builders and `GetHtmlAsync`, to reuse (shared dev DB: scope asserts to own ids). `ProductDetailPageTests.cs` is the Site B regression guard.

## Tasks & Acceptance

**Execution:**
- [x] `Models/ProductPost.cs`, `Models/ProductArchive.cs` -- Q1 flag, Q3 genuine-photo list + gallery merge, Q4 trust fields + resolved chip list, controller-set category/hub props.
- [x] `Controllers/CmsController.cs` -- Site A view switch + category/hub loading.
- [x] `Views/Cms/SiteAProductPost.cshtml` -- scaffold + shippable variant + quote `<dialog>` per Boundaries.
- [x] `wwwroot/assets/js/site-a.js` -- modal opener/close + genuine-photo badge toggle.
- [x] `wwwroot/assets/css/site-a.css` -- PDP + modal styles under Site A tokens, 1024/768 breakpoints, `:focus-visible` on links/CTAs.
- [x] `tests/.../SiteAProductPageTests.cs` (new) + script tests -- every I/O row, encoding, CTA hrefs/labels, trust/photo-badge rules, modal markup/opener, Site B unchanged.

**Acceptance Criteria:**
- Given a Site A product with SEO title/meta set, when requested, then it renders at its own URL with that `<title>`/meta.
- Given "Yêu cầu báo giá" is used, when the visitor submits the general form, then a `FormSubmission` with `formType=general` and that product's title in `product_of_interest` is stored.
- Given the full suite, when run, then all pre-existing tests pass.

## Implementation Notes

- A post has no SiteId, so the controller loads the category's `PageInfo` first and only loads the full `ProductArchive` + hub for Site A.
- Q4 + the "never on installation-required" rule: on a "Cần thi công" product, a trust claim containing "Giao hàng toàn quốc" (e.g. inherited from the category) is dropped before the max-3 cut (`ProductPost.TrustChipList`).
- Photo badge: `data-genuine="true"` on genuine thumbs; site-a.js listens for clicks on `[data-sa-pdp-gallery]` (bubbles after sb-gallery.js's thumb listener) and syncs the main badge to the `aria-current` thumb. sb-gallery.js untouched.
- Modal: one `initModal` per dialog id (close button, backdrop = click whose target is the `<dialog>`, `close` event returns focus + resets `aria-expanded`); no document-level listeners added.
- Razor always renders `data-*` attributes (a null value becomes `=""`, unlike `aria-*`), so the view renders single-photo and gallery variants as separate branches, thumbs carry `data-genuine="true|false"`, and site-a.js selects `[data-sa-pdp-gallery="true"]`. Found by the first test run (2 failures), fixed in the orchestrator; suite 444/444.
- Tests: `SiteAProductPageTests.cs` (HTTP renders + lead POST + model rules), `SiteAPdpScriptTests.cs` (Jint, real sb-gallery.js + site-a.js with bubbling stub).

## Spec Change Log

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V):

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| V1 | Render test checks `data-sa-pdp-gallery` presence, not `="true"` the JS needs | low | Pre-verified gap; test-only → patch. |
| B1/E5 | Phone sheet: tap on blank area below the form closes the modal | medium | CSS: dialog `height:100%`, panel unbounded, so the click target is the `<dialog>`. Values survive (DOM kept) but the close is surprising → patch (panel `min-height:100%`). |
| B2/E6 | Drag-select from input released on backdrop closes the modal | low | Real (click goes to the common ancestor), but values are kept and it's rare; the fix adds pointerdown tracking → rejected. |
| E7 | Clicking the dialog's scrollbar closes it | maybe-false | Needs a browser check of whether a scrollbar click dispatches `click`; at most low → rejected. |
| B3/E8 | CTA dead without JS / without `showModal` | false | Frozen Q2: no no-JS fallback (the form needs JS to submit anyway); `showModal` is in every supported browser. |
| B4/B5 | Shipping time and bulk-price note are hardcoded | false | Exact copy is frozen in Boundaries. |
| B6 | Existing products default to shippable | false | Frozen Q1: default off = shippable; prod seed has categories only, no products. |
| B7/E4 | Shipping-claim filter misses other wordings | low | Real, but editor fields require true claims and the fix widens matching heuristics → rejected. |
| B8/E3 | Own claims filtered to empty → category claims not used | false | Frozen Q4: a non-blank product value replaces the category's, which is exactly the current behavior. |
| B9 | Category loaded twice per Site A PDP | low | PageInfo first keeps Site B from loading the full archive; one cached read → rejected. |
| B10 | Draft preview uses the published category | low | Preview-only, same as 6.3 → rejected. |
| B11 | Dead `@if (multi)` inside the multi branch, odd indentation | low | Direct deletion → patch. |
| B12 | iOS `100%` vs `dvh`; `:has()` scroll lock | low | The sheet scrolls, so content stays reachable; the fix adds fallbacks → rejected. |
| B13/E9 | Reopening after success shows the success message | low | Same as every inline lead form (lead-form.js); a reset adds state → rejected. |
| E10 | Focus lost when the success swap removes the submit button | low | The success node is `role=status` (announced); the fix touches shared lead-form.js → rejected. |
| B14 | No PDP test for an unpublished hub | low | `LoadParentHubAsync` is covered by 6.3's tests and reused unchanged → rejected. |
| B15/E11 | 200-char title cut can split a surrogate/diacritic | low | Same as Site B's PDP; Vietnamese is NFC-precomposed → rejected. |
| B16 | Shipping-text asserts scan the whole page (shared DB) | low | Direct correction → patch (scope to `<main>`). |
| B17 | Script tests miss the two-openers and no-genuine cases | low | Two openers only arrive with 6.5 → rejected. |
| B18 | Thumbnail badge text 8.5px, ellipsized | low | Frozen Q3 puts the badge on the thumbnail; aria-label has the full text → rejected, flagged for the walkthrough. |
| E1 | Unpublished category → breadcrumb links to a 404 | maybe-false | Depends on whether Piranha routes posts under an unpublished archive; at most low (content-setup window) → rejected. |
| E2 | Category might not be a ProductArchive | false | `[PageTypeArchiveItem(typeof(ProductPost))]`: only ProductArchive holds ProductPosts. |

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test` (repo root) -- expected: all tests pass.
- `dotnet build` -- expected: no new warnings.

**Manual checks:**
- Dev samples at 375/820/1280px: a Site A product shows breadcrumb, price-block, shipping-note and CTAs; tab order nav → breadcrumb → gallery/info → specs → footer.
