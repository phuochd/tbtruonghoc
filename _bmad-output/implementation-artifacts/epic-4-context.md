# Epic 4 Context: Site B — Bảng tham khảo cấu hình trống (should-have)

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Let Site B (trongdoitam.net) buyers see, and now also pick, product configuration options before they ask for a quote. For drums the options are size × loại 1/2/3 × bánh xe × sơn × vẽ mặt trống. Their choices arrive with the lead, so sales does not have to ask again. The epic started as a read-only drum reference table fixed to the Trống subpages (4.1, done). It has been rescoped into a reusable Piranha **config block** (4.2). Editors add the block to any supported page (archive, PDP, landing page) and choose an input type for each row. That serves drums and also other product lines such as thùng rượu and bồn tắm. It stays a lightweight selector and never becomes a configurator or a checkout: no price is computed or shown, and it always ends in the human quote-request / Zalo / phone path. The epic is a should-have. Pages without the block must work fully.

## Stories

- Story 4.1: Bảng tham khảo cấu hình trống (FR-11) — done
- Story 4.2: Block cấu hình dùng chung — editor chọn kiểu input (FR-11)

## Requirements & Constraints

- Never compute or display a price. No cart and no "Đặt mua ngay". Drums stay contact-for-quote. The block must show the disclaimer "Bảng tham khảo, không tính giá tự động".
- Row input types, chosen by the editor:
  - Chỉ hiển thị: read-only text, the 4.1 behaviour.
  - Text: a free text box.
  - Option: pick one.
  - Check: a single yes/no.
  - Multi-option: pick several.
- For Option and Multi-option rows, the editor enters the choices as a list, and they render in editor order.
- Every selection is optional, so a visitor can submit with nothing selected. Selections are carried into the page's quote form in a readable form, e.g. "Kích thước: 60cm; Loại: 2; Bánh xe: Có", so the stored lead records the configuration.
- Each page has **exactly one** quote form. If the host page has no form of its own (e.g. an archive), the block brings the shared quote form. If it already has one (PDP, landing page), the block feeds that form.
- An invalid row is omitted. A row is invalid if its label is blank, or if it is an Option/Multi-option row with no choices. A block with no valid rows renders nothing: no empty shell, heading or broken link.
- Row content is CMS-editable and supplied by the client. Never hardcode or invent option values. The Stitch mock rows are illustrative only.
- Progressive enhancement: without JavaScript the rows stay readable as a reference.
- Accessibility floor is WCAG 2.1 AA. Every input is labelled and keyboard-operable, and has a tap target of at least 44px. The block fits a 375px viewport with no sideways scroll. No hover-only affordances. The audience is older, less web-savvy, and uses phones and tablets first.
- All editor-entered text is HTML-encoded on output. The UI is Vietnamese only.

## Technical Decisions

- **Block, not region.** The config block is a Piranha block group with an optional heading, and its child items are the config rows. Editors add, reorder and remove it like the existing Gallery/Column blocks.
- **Where it can go.** Supported page types are at least ProductArchive (the Trống, Thùng rượu and Bồn tắm subpages), ProductPost (PDP) and LandingPage. Each view renders a block area at a fixed, documented position. On archives that position is after the grid/pager and before the trust-block, the same slot 4.1 used.
- **Replaces 4.1.** The block replaces 4.1's `DrumConfigTitle`/`DrumConfig` regions on `ProductArchive` and the `_DrumConfigReference` partial. No client content is expected, so there is nothing to migrate, but confirm that before removing them. Carry 4.1's tests over to the block so they still cover the table, disclaimer, form prefill, encoding and empty state.
- **Lead pipeline.** Reuse the Epic 1 quote-form pipeline: the `_QuoteRequestForm` partial, client-side fetch to `POST /api/leads`, and the allow-listed `FormType`. The `FormSubmission` schema is fixed shared columns: no JSON blob, no per-form tables, no new ad-hoc columns. Configuration text must fit the existing fields. Phone and Zalo come from each site's `SiteSettings` and are never hardcoded.
- **Pricing separation.** Only the LandingPage's own variant+price region shows real prices. The config block must not add price fields to any page type.
- **Styling.** Reuse the existing `drum-config-reference` treatment rather than forking a new table style. It uses the Mộc Trầm tokens:
  - `surface-sunken` (#EFE7D8) background, used sparingly so the block reads as a recessed reference tool
  - `border` outline and `rounded.md` corners
  - `body-sm` rows
  - a `primary`/`on-primary` CTA
  - a `caption` disclaimer

  PDP spec rows already share this row treatment.

## UX & Interaction Patterns

- Mobile-first. Inputs must stack or fit within 375px. Nothing may overflow the page gutter.
- Voice is calm and helpful, e.g. "Liên hệ để được tư vấn cấu hình phù hợp." No urgency or hype.
- Buyer journey: browse a product subpage or PDP, pick options in the block (optional), then submit the quote form with the selections prefilled. Phone/Zalo remain an immediate alternative.
- Quote-form behaviour is unchanged from Epic 1: inline per-field validation, an in-place success message, and entered values kept on failure with a phone/Zalo fallback.

## Cross-Story Dependencies

- 4.2 builds on and replaces 4.1's regions and partial, and inherits 4.1's tests.
- Depends on Epic 1 (lead pipeline, `SiteSettings`, form-type allow-list), Epic 2 (ProductArchive subpages, ProductPost PDPs, Mộc Trầm tokens, trust-block, quote-form styling) and Epic 3 (the LandingPage type and its inline form, which the block must feed instead of adding a second form).
- No other epic depends on Epic 4. Epic 2 pages must keep working with no block present.
