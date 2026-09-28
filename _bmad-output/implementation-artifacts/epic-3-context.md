# Epic 3 Context: Site B — Landing page trả phí Thùng rượu gỗ (deadline Tết 2027)

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

This epic gives Site B (trongdoitam.net) a reusable paid-ads landing page type and ships its first instance for the decorative wine-barrel (thùng rượu gỗ) line. It must be live and taking paid-ad traffic before Tết Âm Lịch 2027 (~mid-February 2027). This is the only fixed external deadline in the project, and it is why Site B is built before Site A. The target visitor is a Tết gift buyer clicking a Google Ads ad. She needs to see what each barrel variant looks like and what it costs. Then she needs to act on the same page, without being pushed into a phone-only sales process that would kill ad conversion. The landing page is a single-purpose conversion page. It has no site nav and no sticky contact bar. It is the only place on Site B that shows real per-variant prices. Every "Đặt mua ngay" still ends in a lead (inline quote form, Zalo, or phone). The order is finalized off-site. There is no cart and no payment.

## Stories

- Story 3.1: Landing Page Type & template trả phí (FR-12)
- Story 3.2: Giá theo biến thể & CTA "Đặt mua ngay" (FR-13)

## Requirements & Constraints

- The landing page type must be clearly separate from organic Site B category and product pages. It is left out of primary navigation but still works as a standalone URL for direct and ad traffic.
- It must be reusable: editors can create a landing page for a future campaign with no code change.
- There must be at least one wine-barrel landing page before Tết 2027. Success means it is live and receiving paid-ad traffic before Tết.
- Every variant shows its own real, visible price next to a variant-label chip. The variants listed are gỗ sồi, ngựa kéo, 1 ngựa, 2 ngựa. **Flag:** the sources disagree. Some say "4 variants". The organic category page (Epic 2) treats gỗ sồi as the material, with 3 variants: ngựa kéo, 1 ngựa, 2 ngựa. Keep the variant list CMS-driven with no fixed count, and confirm the real list and prices with the client. Never make up or hardcode prices. An early Stitch export invented prices, and that must not ship.
- "Đặt mua ngay" goes to the inline quote-request form on the same page, or to Zalo/phone. It never leads to a cart, checkout, or payment step (NFR-4 applies here despite the visible prices). There are no accounts or login UI.
- Successful submits use the same inline success state as the Epic 1 form: no redirect, no modal, no page reload. On failure, the form keeps the entered values and offers phone/Zalo.
- There is no cart recovery, exit-intent popup, or retargeting behavior. The persistent CTA is the only way the page re-engages a visitor.
- Running or budgeting ad campaigns is out of scope. This epic only makes sure the page and measurement exist.
- The page is Vietnamese only. WCAG 2.1 AA is the assumed accessibility floor. Core Web Vitals must not regress, which matters for ad landing quality.

## Technical Decisions

- **Content shape:** the Landing Page is a **standalone Page type**. It is never an Archive and never an Archive child, and it is excluded from every archive listing and from nav. It is a model type next to Product and BlogPost.
- **Pricing split (AD-5):** the Landing Page Type has **its own repeatable variant+price region** for structured multi-variant pricing, such as label, price, and CTA per card. This is separate from the Product Post's single optional flexible price field. Never model a catalog product as a Landing Page just to show a price. Never add real prices to organic Product Posts to "match" the landing page. Organic wine-barrel PDPs stay contact-for-quote, and the two surfaces must never be merged.
- **Chrome hook:** the Site B layout already supports `ViewData["HideSiteChrome"] = true`. It hides the nav, footer, and sticky contact bar but keeps the design tokens, analytics, and cookie consent. The landing template should use this hook rather than a new layout.
- **Leads:** reuse the Epic 1 pipeline: the `_QuoteRequestForm` partial, client-side fetch to `POST /api/leads`, the shared `FormSubmission` table, email notification, and the Leads Manager. Prefill `product_of_interest` with the chosen variant. `FormType` is allow-listed (`general`/`survey`; anything else falls back to `general`). If landing leads need their own type, the allow-list must be extended on purpose.
- **Contact data:** phone and Zalo come from the per-site `SiteSettings`. Never hardcode them.
- **Analytics:** the landing page must keep the per-site GA4 and consent gate from Epic 1, because ad traffic has to be measurable.
- **Design system:** full Mộc Trầm: tokens, Be Vietnam Pro, `gutter` 16/24px, and the card-grid conventions (product-style grids 1-up below 480px, 2-up from 480px). The `landing-page-cta` token block uses a `primary` background, `on-primary` text, `cta` typography, `rounded.sm`, `price` typography (18/700) for prices, and `label` typography for the variant chip. The CTA has a larger touch target than the standard product-card CTA. `rounded.full` is allowed for chips and the Tết-2027/"Mới" badges. Keep it visually separate from Site A.

## UX & Interaction Patterns

- **Shell:** no nav, no 3-segment sticky bar, no escape route to the rest of the site (by design). Brand recognition comes from the tokens alone.
- **Variant cards:** one card per variant, each with a photo, a variant-label chip, a real price, and one full-width "Đặt mua ngay" CTA. The CTA replaces the sticky bar as the page's single persistent action. Reflow to however many variants the CMS returns.
- **CTA flow:** tapping a card's CTA opens or scrolls to the inline quote-request form, prefilled with that variant. Zalo and phone stay available as alternatives. Use no modal-on-modal stacking.
- **Form:** labeled fields (name, phone, product of interest, message). Show error text under each field on blur and on submit, never as color alone.
- **Accessibility:** each CTA's accessible label names its variant and destination. Every product photo gets descriptive alt text. Tap targets are sized for an older, less web-savvy audience.
- **Voice:** calm and specific about craft. No urgency or hype such as "Mua ngay kẻo lỡ! 🔥" or countdowns. "Đặt mua ngay" is allowed here only because a real price is shown. The client writes page prose in the CMS.
- **Banned:** cart or checkout, login, auto-rotating carousels, modal-on-modal.

## Cross-Story Dependencies

- Epic 3 depends on Epic 1 (Site B `Site` record, per-page SEO fields, `SiteSettings`, the lead form pipeline, GA4 and consent) and on Epic 2 (Mộc Trầm tokens, the layout's `HideSiteChrome` hook, the product-card grid conventions, the quote-form styling).
- Story 3.1 comes first. It adds the Landing Page Type, including the empty or structural variant+price region, the chrome-less template, and the nav exclusion. Story 3.2 fills in the variant-card rendering, real prices, and the CTA-to-inline-form wiring on top of it.
- The organic Thùng rượu gỗ category page and variant PDPs (Story 2.3/2.4) are the SEO counterpart. They must stay contact-for-quote and must not duplicate the landing page.
- Real variant names, prices, and photos come from the client. Content readiness, not just code, drives the Tết deadline.
