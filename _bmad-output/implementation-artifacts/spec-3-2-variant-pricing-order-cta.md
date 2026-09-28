---
title: 'Story 3.2: Variant pricing & "Đặt mua ngay" CTA on the landing page'
type: 'feature'
created: '2026-09-28'
status: 'done'
baseline_commit: '926bc28114c3634e5c199a7d234abbe59b46cbdc'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The `LandingPage` type (3.1) stores `Variants` (image, name, chip label, price) but never renders them, so a paid-ad visitor sees no variant and no price. That breaks FR-13: the only priced surface on Site B.

**Approach:** Render one card per CMS variant in a product-style grid: photo, variant-label chip, name, real price and a full-width per-card CTA. The CTA is an `<a href="#dat-hang">` and still works without JS. A small script prefills the inline form's "Sản phẩm quan tâm" with the chosen variant. The Epic 1 lead pipeline is unchanged (inline success, `formType=landing`).

## Boundaries & Constraints

**Always:**
- **Placement:** the variant section sits after the intro and before the content blocks. It renders only when at least one variant has a name or label. A variant whose name and label are both blank is skipped.
- **Card:** image (alt: media `AltText` → name → label → page title; `srcset` like the hero), chip = `Label` (rendered only when set), title = `Name` (falls back to `Label`), price = `Price` trimmed, in `price` type (18/700). All values are HTML-encoded. The grid reflows to any count and reuses `.sb-grid.sb-grid--products` (1-up <480px, 2-up ≥480px).
- **Per-card CTA:** `<a href="#dat-hang">` with `data-lp-variant="<prefill text>"`. Its touch target is ≥ 56px, the same as the fixed CTA. `aria-label` = "<CTA text> <variant name> – đến form đặt hàng". The card body itself is **not** a link, and it has no pressed or scale state.
- **Prefill:** new `wwwroot/assets/js/landing-page.js`. On a click of a `[data-lp-variant]` link, it sets the `#dat-hang [name=productOfInterest]` value and lets the anchor scroll. It does nothing once the form has been replaced by the success state. Without JS the anchor still jumps to the form, which stays prefilled with the page title.
- **Blank price decision (2026-09-28, Phước: a).** A variant with no price shows a muted "Liên hệ báo giá" line (`.sb-card__price--contact`), and its card CTA reads "Nhận báo giá". With a price, the CTA reads "Đặt mua ngay".
- **Prefill decision (2026-09-28, Phước: a).** `data-lp-variant` is `<heading> – <price>`, or just `<heading>` when the price is blank.
- **Section heading decision (2026-09-28, Phước: a).** A new optional `VariantsTitle` StringField region supplies the h2. It is trimmed and rendered only when set, with no fallback copy. Card titles are `h3` under an h2, and `h2` when there is no section title, so no heading level is skipped.
- Chip: `label` type, `rounded.sm`, a token colour pair with ≥ 4.5:1 contrast.
- The fixed bottom CTA, the form, the alternatives line and the `landing` form type stay exactly as in 3.1.
- Update the `Variants` region description and the doc comments (remove "not rendered yet / Story 3.2"). The seed stays without variants.

**Never:**
- Hardcoded or invented prices or variant names. A cart, checkout, quantity, total, payment, login, countdown, carousel or modal.
- Changing `_QuoteRequestForm`, `lead-form.js`, `LeadsController`, `_LayoutTrongDoiTam.cshtml`, `ProductPost` or organic PDP pricing.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Variants filled | 2 variants, name+label+price+image | 2 cards in order: chip, title, price text, CTA "Đặt mua ngay" → `#dat-hang`, `data-lp-variant` = "<name> – <price>" | N/A |
| No variants | Empty list | No variant section, no empty grid | N/A |
| Blank variant | Name and label both blank | Card skipped; section absent when all are blank | N/A |
| Label only | Name blank, label "2 ngựa" | Title "2 ngựa", no separate chip | N/A |
| No price | Price blank | "Liên hệ báo giá" muted; CTA "Nhận báo giá"; prefill = heading only | N/A |
| Section title | `VariantsTitle` set / blank | h2 + h3 cards / no h2, h2 cards | N/A |
| No image | Image unset | No `<img>`; the wood-grain thumb frame shows | N/A |
| HTML in fields | Name `<b>x</b>` | Encoded in text, `aria-label` and `data-lp-variant` | N/A |
| CTA tapped | JS on | Form's product field = prefill text; page at `#dat-hang` | JS off → plain anchor jump |
| Submit from card | Valid lead | Stored `formType=landing`, product = prefill text, inline success | Failure keeps values + shows phone/Zalo |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Models/LandingPage.cs` -- add `LandingVariant` helpers in 3.1 style (`Trimmed`): `NameText`, `LabelText`, `PriceText`, `HeadingText` (name ?? label), `HasImage`, `AltText(pageTitle)`, `PrefillText`, `CtaText`. Add `LandingPage.VariantsTitle` region + `VariantsTitleText`, and `VisibleVariants` (skips blank ones). Update the region description and the XML docs.
- `src/TbTruongHoc.Web/Views/Cms/LandingPage.cshtml` -- insert `<section class="sb-lp__variants">` inside the first `.container` after the intro, with `<ul class="sb-grid sb-grid--products">`. Reuse `.sb-card`, `.sb-card__thumb` (image via `WebApp.Media.ResizeImage` 480/768 `srcset`, `loading="lazy"`), `.sb-card__body`, `.sb-card__title`, `.sb-card__price`, `.sb-card__price--contact`. Load `landing-page.js` next to `lead-form.js`. Update the header comment.
- `src/TbTruongHoc.Web/Views/Shared/_ProductCard.cshtml` -- markup reference only. Do not edit it.
- `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css` -- in the landing section (~1185-1322), add `.sb-lp__variants` (top margin `--sb-section-gap`), `.sb-lp__chip` and `.sb-lp__variant-cta` (the `.sb-card__cta` look, `min-height: var(--sb-lp-cta-h)`, `margin-top:auto`). Neutralise `.sb-card:active` for `.sb-lp__variant` (the card is not tappable). The tokens are at lines 16-45.
- `src/TbTruongHoc.Web/wwwroot/assets/js/` -- new `landing-page.js`: an IIFE with no dependencies, in `lead-form.js` style.
- `tests/TbTruongHoc.Web.Tests/LandingPageTests.cs` -- replace `Variants_And_Prices_Are_Not_Rendered_Yet` (~line 205) with rendering tests. Reuse `f.LandingAsync`, the TinyPng image helper, `GetHtmlAsync` and the page-wide asserts (every `<img>` has an alt, no "giỏ hàng", no iframe).

## Tasks & Acceptance

**Execution:**
- [x] `Models/LandingPage.cs` -- variant helpers + `VisibleVariants`, updated descriptions.
- [x] `Views/Cms/LandingPage.cshtml` -- variant section + script tag.
- [x] `wwwroot/assets/css/site-b.css` -- variant card, chip and CTA styles.
- [x] `wwwroot/assets/js/landing-page.js` -- prefill on CTA click.
- [x] `tests/.../LandingPageTests.cs` -- one test per matrix row (the CTA-tapped row: assert `data-lp-variant` and `href`; JS is not executed), plus the page-wide rules with variants present.

**Acceptance Criteria:**
- Given 4 CMS variants, when the page renders at 375px, then the cards stack 1-up, each price is visible, and no card CTA is covered by the fixed CTA once scrolled.
- Given the full suite, when run, then all pre-existing tests pass (the 3.1 "not rendered" test is replaced, not skipped).

## Implementation Notes

## Spec Change Log

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V).

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| V1/B4 | `landing-page.js` click, null-guard and no-`preventDefault` behavior is only asserted by source-string match | medium | Pre-verified; the repo runs scripts in Jint (`SiteBNavScriptTests`) → patch (Jint test). |
| B1/E1 | "heading – price" prefill > 200 chars fails `ProductOfInterest` `StringLength(200)` | low | Real limit, but variant names/prices are short editor text; the visitor sees the field error and can edit it; truncation adds a branch → rejected. |
| E2 | Stale product-field error after a new prefill | low | That field can only error on the 200-char limit (same unlikely case); clearing adds code → rejected. |
| E3 | Modifier-click (new tab) still overwrites the field | low | Harmless: it only sets this page's field to the chosen variant; guard adds branches → rejected. |
| B2 | No GA4 event per variant CTA | low | Page-level GA4 already exists (epic: measurement only); the variant is in every lead; new events add surface → rejected. |
| B3 | Prefill is silent for screen-reader users; typed value overwritten | low | The anchor moves the focus-navigation start to the form, and the field shows the value when reached; the overwrite is the explicit intent → rejected. |
| B5 | 375px/no-cover AC not automated; no CSS addresses overlap | false | 3.1's `main` bottom padding + `scroll-padding-bottom` let every card scroll clear of the fixed CTA; layout is a listed manual check. |
| B6 | Spec `in-review` vs sprint-status `in-progress`; empty log sections | false | Sprint status is synced by the workflow at presentation; empty sections are template-normal until used. |
| B7 | Card keeps inherited transition/hover | false | `:active` sets `transform`/`box-shadow` to none, so nothing animates; no `.sb-card:hover` rule exists. |
| B8 | Chip contrast unverified; chip precedes heading in DOM | low | #3A2E22 on #EFE7D8 ≈ 11:1 (claim holds); chip-before-title is the approved card order and is still read in sequence → rejected. |
| B9a | `QuoteCtaText` duplicates `DefaultCtaLabel` | low | Confirmed; direct correction → patch. |
| B9b | "Liên hệ báo giá" literal repeated vs organic views | low | Organic views already inline the literal (pre-existing pattern); unifying touches out-of-scope views → rejected. |
| B10 | Unprefixed `.sb-lp__chip` may lose to `.site-b p` rules | false | No `.site-b p` / `.sb-main p` rule exists in site-b.css. |

## Verification

**Commands:**
- `dotnet test --artifacts-path bin/test-art` (repo root, MariaDB up) -- expected: all tests pass.
- `dotnet build` -- expected: 0 new warnings.

**Manual checks:**
- Seeded page with 4 test variants at 375px and 1280px: the grid reflows, chips are legible, tapping a card CTA scrolls to the form with the product field prefilled, a submitted lead shows that product in the Leads Manager, and the success state is inline.
