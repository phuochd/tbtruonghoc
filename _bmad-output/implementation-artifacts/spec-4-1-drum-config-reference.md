---
title: 'Story 4.1: Drum configuration reference table (FR-11)'
type: 'feature'
created: '2026-09-28'
status: 'done'
baseline_commit: '5d207569f6ccebc88d35794e82d8f8d8de60c54d'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-4-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A drum buyer on a Trống subpage cannot see which configurations the workshop offers (size, loại 1/2/3, bánh xe, sơn, vẽ mặt trống) until they call. Epic 2 built the row styling for this reference, but the reference itself does not exist yet.

**Approach:** Add an optional, CMS-editable `drum-config-reference` to `ProductArchive` (the Trống subpages). It renders as a read-only table on surface-sunken, followed by the caption disclaimer "Bảng tham khảo, không tính giá tự động" and the contact CTA. When the region is empty, the page renders exactly as it does today.

## Boundaries & Constraints

**Always:**
- Everything is CMS-driven: no hardcoded rows, sizes, finishes or prices. The Stitch mock rows are illustrative only.
- Empty or half-empty content renders nothing: no section, heading, disclaimer or CTA shell.
- All editor text is HTML-encoded.
- Mộc Trầm `drum-config-reference` tokens: `surface-sunken` background, `border`, `rounded.md`, `body-sm` rows, `caption` disclaimer, `primary`/`on-primary` CTA with a ≥44px tap target. Label text uses `on-surface`, not muted (AA on sunken). Reuse the `.sb-spec*` visual language rather than inventing a new one.
- At 375px the table never widens the page.
- Placement: after the product grid and pager, before the trust-block (same order as the Stitch Trống-Chùa export).
- It is a real `<table>` with `<th scope="row">` labels, plus a visible `<h2>`.

**Decisions (2026-09-28, Phước):**
- **Table shape = B, one row per option dimension:** each row has a label ("Kích thước", "Loại", "Bánh xe", "Sơn", "Vẽ mặt trống", or whatever the editor types) and a value (the offered choices, free text). Both cells are required. Rows appear in editor order, 2 columns, with no sideways scroll on a phone. There is an optional heading field; when blank, it defaults to "Bảng tham khảo cấu hình trống".
- **End CTA = A, embedded quote form** under the disclaimer. Use the existing `_QuoteRequestForm` with `ProductOfInterest = "Tư vấn cấu hình – {archive title}"` (truncated to 200 characters) and the default formType `general`. Include `lead-form.js` only when the config renders.
- **Spec size:** kept whole (~1800 tokens) by user choice.

**Never:**
- Price computation or display, a selector/configurator that changes state, cart, "Đặt mua ngay", or urgency copy.
- Changing `ProductPost`/PDP spec rows, `_ProductCard`, `_QuoteRequestForm`, `lead-form.js`, `_Layout*`, Site A views or `Areas/Manager`.
- Seeding real table content (the client supplies it through the Manager).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| No config | Trống-Chùa archive, region empty | Page identical to today: grid, pager, trust-block; no `sb-config` markup | N/A |
| Full config | Title + ≥2 valid rows | Heading, table rows in editor order, disclaimer, CTA | N/A |
| Half-empty row | A row with a label but a blank value (or the reverse) | Row omitted; the others render | N/A |
| All rows invalid | Rows exist but none valid | Treated as "No config" | N/A |
| HTML in cell | `<b>x</b>` typed in a cell | Rendered as literal text | Encoded |
| Non-Trống archive | Thùng rượu archive, region empty | Unchanged | N/A |
| Long value | 375px, value of 200+ chars without spaces | Wraps inside its cell (`overflow-wrap:anywhere`); page has no sideways scroll | N/A |
| Heading blank | Rows valid, heading empty | `<h2>` = "Bảng tham khảo cấu hình trống" | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Models/ProductArchive.cs` -- host type. Add a list region + row class + a trimmed-valid-rows helper, mirroring `ProductPost.Specs`/`SpecRows` (`ProductPost.cs:43-72`) and `LandingPage.Variants`/`VisibleVariants`. The region `Description` tells editors that it is for the Trống pages, that it is a reference with no price, and which cells are required. The types update on startup via `ContentTypeBuilder...DeleteOrphans()` (Program.cs), with no migration.
- Piranha gotcha: a one-field region class collapses into the bare field. List regions of a multi-field class are fine (`IList<ProductSpecRow>`). A standalone `Title` + list needs to be two regions.
- `src/TbTruongHoc.Web/Views/Cms/ProductArchive.cshtml:61-83` -- insert the partial between the grid block's closing `}` and `_TrustBlock`. It must render even when `posts.Count == 0`.
- New `src/TbTruongHoc.Web/Views/Shared/_DrumConfigReference.cshtml` -- the component markup. Its model is `ProductArchive` or the row list. `<div class="container">` wrapper like `_TrustBlock`.
- CTA: `_QuoteRequestForm` + `QuoteRequestFormViewModel { ProductOfInterest }`. Copy the truncation and the `lead-form.js` script include from `ProductPost.cshtml:123,131`.
- `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css:903-938` -- `.sb-spec*` (reuse the tokens/row look); add `.sb-config*` after it.
- Tests: `tests/TbTruongHoc.Web.Tests/ProductCatalogTests.cs` `CatalogBuilder.ArchiveAsync` (L405), `GetHtmlAsync`/`Decode`/`Section` are `internal`, and `CraftsmanStoryTests.cs:699-722` builds catalogs too.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Models/ProductArchive.cs` -- config regions + row class + `ConfigRows` helper (valid rows only, trimmed).
- [x] `src/TbTruongHoc.Web/Views/Shared/_DrumConfigReference.cshtml` -- heading, table, disclaimer, CTA; renders nothing when there are no valid rows.
- [x] `src/TbTruongHoc.Web/Views/Cms/ProductArchive.cshtml` -- include it before `_TrustBlock` plus the `lead-form.js` script when the config renders.
- [x] `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css` -- `.sb-config*`, mobile-first, 2-column rows like `.sb-spec__row`.
- [x] `tests/TbTruongHoc.Web.Tests/DrumConfigReferenceTests.cs` -- one test per matrix row (the long-value row as a markup/CSS-class check), plus the ACs.

**Acceptance Criteria:**
- Given any archive render with the config, then there is no "Đặt mua", no price text from the config, no `<form action`, and the disclaimer text appears exactly once.
- Given the config renders, then the page has exactly one quote form, prefilled with "Tư vấn cấu hình – {archive title}". A submit goes through `/api/leads` unchanged. Without a config, there is no form and no `lead-form.js`.
- Given the full suite, when run, then all pre-existing tests pass.

## Implementation Notes

- Regions on `ProductArchive`: `DrumConfigTitle` (StringField) + `DrumConfig` (`IList<DrumConfigRow>`, fields "Hạng mục"/"Lựa chọn"); helpers `ConfigTitleText`, `ConfigRows`; `DefaultConfigTitle` const.
- The quote form's inputs/submit button had no Site B styling anywhere; `.sb-config__form` scopes primary/on-primary, ≥44px styling to this block only (PDP/landing forms unchanged).

## Spec Change Log

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V):

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| V1 | The "config before trust-block" assertion is vacuous: the trust-block never renders in `DrumConfigReferenceTests` | low | Pre-verified: the seeded story is a draft, so `trustAt` = -1 → patch (assert order in CraftsmanStoryTests with a published story). |
| B13 | `<table>` has no accessible name of its own | low | The section is labelled, the table isn't; a one-attribute fix → patch (`aria-labelledby`). |
| B5 | `.sb-spec` comment says Epic 4 reuses its classes; it doesn't | low | Stale comment; direct correction → patch. |
| B3 | Form styling is scoped to `.sb-config__form`; the PDP/landing forms render the same partial unstyled | low | Real. The unstyled Site B form predates 4.1, and a site-wide restyle changes pages outside this intent → defer. |
| B2/E2 | Config, disclaimer and form repeat on archive page 2+ | low | Real, but it matches the existing blocks and trust-block, which also repeat. It only happens with more than 12 drums in one subcategory, and the fix adds a branch → rejected. |
| B4 | No focus style on the new inputs or button | false | The global `.site-b :focus-visible` outline applies (same call as Story 2.4 B10). |
| B6/E1/V-o1 | Truncation: surrogate split, null title, untested, duplicated with the PDP | false | Piranha page titles are at most 128 characters, so the prefix plus the title stays under 200 and the branch is unreachable. A saved page's title is never null. |
| B7/V-o2 | `Non_Trong_Archive...` test duplicates the no-config test | low | True, but it documents its own matrix row at no cost → rejected. |
| B8 | `hasConfig` in the view and `rows.Count` in the partial are two gates for `lead-form.js` | low | There is one embedding page; a shared flag adds surface → rejected. |
| B9 | `ConfigRows` recomputed per access | low | Two small LINQ passes per request → rejected. |
| B10 | Disclaimer string in 3 places | low | The region Description is editor help, not render copy → rejected. |
| B11 | CSS test is string-based and brittle | low | The spec allows a markup/CSS-class check; the manual 375px check is in Verification → rejected. |
| B12 | No seeded sample rows | false | Seeding real content is a spec Never. |
| B1 | Spec and context files missing from the diff; sprint-status says in-progress | false | They were excluded from the review diff on purpose. in-progress is correct at this stage. |
| E3 | Input rule would stretch a future checkbox | false | The form has no checkbox/radio; speculative. |
| E4 | An editor can type a price into a value cell | false | That's editor content; the code shows no price and the region Description forbids prices. |

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test --artifacts-path tests/TbTruongHoc.Web.Tests/obj/art` (repo root) -- expected: all pass.
- `dotnet build` -- expected: 0 new warnings.

**Manual checks:**
- In the Manager, fill the config on "Trống chùa" and publish. At 375px and 1280px, check that the table sits on the sunken band, the disclaimer is under it, the form is prefilled and submits, and the page has no sideways scroll. Clear it and the section disappears.
