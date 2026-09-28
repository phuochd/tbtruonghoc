# Epic 4 Context: Site B — Bảng tham khảo cấu hình trống (should-have)

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give drum buyers on Site B (trongdoitam.net) a read-only configuration reference (size × loại 1/2/3 × bánh xe × sơn × vẽ mặt trống) on the Trống subpages. They can use it to narrow down what they want before contacting the workshop. It is a reference aid, not a configurator. It never computes or shows a price, and it always ends in the same human quote-request / Zalo / phone CTA used everywhere else. It is a should-have. If it has not shipped, the Trống subpages must still work fully without it, and buyers configure by phone/Zalo as they do today.

## Stories

- Story 4.1: Bảng tham khảo cấu hình trống (FR-11)

## Requirements & Constraints

- The table is read-only. Its dimensions are size, loại 1/2/3, bánh xe, sơn, and vẽ mặt trống. There is no price computation, no price display, and no cart or checkout. Drums stay contact-for-quote site-wide.
- A visible disclaimer line sits beneath the table: "Bảng tham khảo, không tính giá tự động".
- The component ends in the existing `quote-request-form`/Zalo/phone CTA pattern.
- If it is built, the options shown must match what sales actually offers. Content must be CMS-editable and must never be hardcoded or invented. The Stitch mock rows (sizes, finishes, "Đại tự") are illustrative only, and the real rows come from the client.
- It is optional per page. A Trống subpage with no table content must render fully, with no empty shell, heading, or broken link, and route straight to the contact channels. Epic 2 must not depend on this epic.
- Vietnamese only. WCAG 2.1 AA is the floor. Tap targets must suit an older, less web-savvy, phone/tablet-first audience. No hover-only affordances.

## Technical Decisions

- **Placement is an open question:** the architecture capability map lists FR-11 as "reference table on Product Post", with the exact shape explicitly deferred to story planning. The epic/story and UX put the component on the **Trống subpages**, which are Archive pages with Product Post children (AD-2). Resolve the actual field/region shape and host page type during story planning.
- **Row styling already exists:** Epic 2's product-detail-page spec rows were built with the `drum-config-reference` row treatment (`surface-sunken` background, `body-sm` rows) so that Epic 4 could reuse it. Reuse it; don't fork a new table style. The PDP spec rows are conceptually one model's subset of this table.
- **Leads/contact:** reuse the Epic 1 quote-form pipeline (`_QuoteRequestForm` partial, `POST /api/leads`, allow-listed `FormType`) and the per-site `SiteSettings` phone/Zalo. Never hardcode contact data.
- **Design tokens (Mộc Trầm):** the `drum-config-reference` block uses `surface-sunken` (#EFE7D8) background, `border` outline, `rounded.md`, `body-sm` rows, a `primary`/`on-primary` CTA, and `caption` typography for the disclaimer. Use `surface-sunken` sparingly. It exists so this block reads as a recessed reference tool, not another product card.

## UX & Interaction Patterns

- Mobile-first. A multi-column table must stay readable on narrow phones, for example by scrolling horizontally inside its own container, never by overflowing the page gutter.
- Voice is calm and helpful, e.g. "Liên hệ để được tư vấn cấu hình trống phù hợp." No urgency or hype.
- The buyer journey is: browse a Trống subpage, optionally scan the reference table, open a model PDP (whose spec rows mirror the table's treatment), then configure off-platform by phone/Zalo, or through the quote form, from memory of the table.

## Cross-Story Dependencies

- Depends on Epic 1 (lead form pipeline, `SiteSettings`, per-page SEO) and Epic 2 (Trống hub + 5 subpages as Archive pages, Mộc Trầm tokens, PDP spec-row styling, quote-form styling).
- Sequenced after the Tết 2027 landing page (Epic 3), which is now done. No other epic depends on Epic 4.
