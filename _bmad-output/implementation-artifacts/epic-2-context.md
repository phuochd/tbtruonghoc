# Epic 2 Context: Site B — Danh mục sản phẩm & Câu chuyện nghệ nhân

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

This epic builds the organic public face of Site B (trongdoitam.net, Đọi Tam heritage woodcraft) in its own "Mộc Trầm" visual system: the shared page shell (nav + always-visible sticky contact bar), the Trống hub with its 5 SEO subcategory pages, the Thùng rượu gỗ and Bồn tắm gỗ category pages, a per-model product-detail page ending in the embedded quote form, and the craftsman (Phạm Trí Trong) story page plus an inline trust-block. Site B sells trust before product — buyers (temple/communal-house caretakers, Tết gift buyers) skew older and less web-savvy and mostly convert by phone/Zalo — so every surface must make the craftsman visible and a human reachable in one tap. Site B is prioritized over Site A because of the fixed Tết Âm Lịch 2027 (~mid-Feb 2027) deadline; the paid wine-barrel landing page (Epic 3), drum config table (Epic 4), and blog (Epic 5) all reuse this epic's shell and components.

## Stories

- Story 2.1: Khung trang & thanh liên hệ cố định Site B (Mộc Trầm)
- Story 2.2: Trống — hub & 5 trang phân loại (FR-9)
- Story 2.3: Trang danh mục Thùng rượu gỗ & Bồn tắm gỗ (FR-9)
- Story 2.4: Trang chi tiết sản phẩm (product-detail-page) cho từng model (FR-9)
- Story 2.5: Trang câu chuyện nghệ nhân & trust-block (FR-10)

## Requirements & Constraints

- Trống (all types unified under one brand), thùng rượu gỗ trang trí, and bồn tắm gỗ each get their own SEO page(s) with line-specific title/meta/content and distinct URLs. Trống = one hub + 5 subpages: Trường học, Lân, Đội, Lễ hội, Chùa. Priority keywords: trống lễ hội, trống chùa, trống trường học.
- Each model/variant (drum loại, wine-barrel variant, bathtub line) gets its own product-detail page with own photos/specs/URL/SEO.
- **Organic Site B pages are contact-for-quote.** Drum, bathtub, and organic wine-barrel pages show muted "Liên hệ báo giá"; real per-variant pricing lives only on the Epic 3 landing page. Never fabricate prices (an early Stitch export did — must not ship). CTA copy must not promise a transaction ("Đặt mua ngay" only where a real price is shown).
- Craftsman story page: photos required at launch, video optional (only with captions), workshop address; linked from all 3 product-line pages — never buried in a generic About page.
- Bồn tắm gỗ has no content/photos/pricing yet: must never publish with broken images or lorem ipsum — either a labeled "coming soon" state or stays unpublished.
- Every page ends in, or offers, a contact path; no cart/checkout, no accounts anywhere (NFR-4).
- Vietnamese only, no i18n. WCAG 2.1 AA floor (assumed, unconfirmed with client). Core Web Vitals must not regress vs. legacy baseline.
- Real model/loại names and counts per drum subcategory are not enumerated — build templates CMS-driven, don't hardcode SKUs.

## Technical Decisions

- **Content shape (AD-2):** each product category (the 5 Trống subcategories, Thùng rượu gỗ, Bồn tắm gỗ) is an Archive page with Product Post children. Trống hub, craftsman story page, and homepage are standalone Pages excluded from any archive listing. Hub/category lists should be queried from Archive pages at render time, never hand-curated link lists.
- **Pricing (AD-5):** Product Post carries an optional flexible price field (blank / exact / range / "từ X"); "Liên hệ báo giá" CTA is always present. Never model a catalog product as a Landing Page to show a price. Landing Page's variant+price region belongs to Epic 3.
- **Contact data:** sticky-contact-bar and nav hotline read phone/Zalo/Maps from the per-site `SiteSettings` (built in Epic 1) — never hardcoded.
- **Quote form:** reuse Epic 1's shared form + `FormSubmission` storage + client-side fetch submission + email notification; the PDP prefills `product_of_interest` with the model name. Don't build a new form pipeline.
- **Design tokens (Mộc Trầm):** single typeface Be Vietnam Pro (or equivalent with full Vietnamese support) for every role; colors background `#F5EFE6`, surface `#FFFFFF`, surface-sunken `#EFE7D8`, on-surface `#3A2E22`, muted `#7A6A56`, primary `#8B5A2B` (actions only), secondary `#A97142` (labels/eyebrows only), border `#DCCFB8`, border-subtle `#EAE1CD`, error `#A63B2E` (form validation only). Type roles enlarged for readability: price 18/700, phone-number 20/700, cta 16/600, body 16, heading-lg 24. 4px spacing scale; gutter 16px mobile / 24px desktop; card-gap 12px; section-gap 32px. Radii sm 6 (buttons), md 10 (cards/forms), lg 14 (story block). Only shadow: sticky bar `0 -4px 14px rgba(58,46,34,0.10)`; cards are flat with 1px border.
- **Strict visual separation from Site A:** no shared palette, typeface, or component styling with Site A's teal/amber Mulish system, despite the shared CMS.
- The Site B DESIGN.md/EXPERIENCE.md pair wins over any Stitch mock on conflict.

## UX & Interaction Patterns

- **nav:** fixed 56px surface bar, wordmark "Trống **Đọi Tam**" (second word in secondary), flat links + Trống submenu (5 subpages). Mobile: hamburger → full-height single-level sheet; hotline shown tablet/desktop only. No login/account icon anywhere.
- **sticky-contact-bar:** Gọi ngay (`tel:`) / Chat Zalo (deep link + web fallback, primary fill) / Bản đồ (Maps, new tab); fixed bottom on every organic page at every breakpoint, never auto-hides; absent only on the landing-page template. Keyboard reachable without trapping focus ahead of main content.
- **Grids (revised after Story 2.2 walkthrough):** `product-card` grids are **1-up below 480px, 2-up from 480px**, widening to 3–4 on tablet/desktop — 2-up at 375px (~165px cards) was too cramped for photo + description. Text-only `category-tile` grids stay 2-up on mobile. Card anatomy never changes across breakpoints; grids reflow to however many items the CMS returns (never assume 3). `card-gap` 12px.
- **product-card:** thumbnail frame uses the secondary→primary gradient as placeholder/frame treatment; eyebrow (secondary), title, one-line description (muted), price row, primary CTA. Whole card body and CTA button are both tap targets to the same PDP. Pressed state on touch-down: 2px inset primary ring + 0.98 scale. No hover-only affordances. Every card/tile CTA and sticky-bar segment needs an accessible label naming its destination.
- **Thùng rượu gỗ organic category:** lists the confirmed gỗ sồi variants (ngựa kéo, 1 ngựa, 2 ngựa) as product-cards → per-variant PDPs; it is the organic/SEO counterpart to the paid landing page, never a duplicate of it, and stays contact-for-quote.
- **Bồn tắm gỗ:** no content yet — keep its category and PDP minimal/generic (don't invent variant names); acceptable states are a clearly labeled "coming soon" block or unpublished, never broken images or lorem ipsum.
- **Empty states:** a category/grid with 0 published items is hidden from its parent entirely, never rendered empty.
- **product-detail-page:** gallery leads, degrades to single photo with no arrows/placeholder tiles; eyebrow + heading-lg title + small sku-code (e.g. "Mã: TC-L2-160"); spec rows use the drum-config-reference row styling (surface-sunken, body-sm) filtered to this model; price row; trust-block; ends in embedded quote form. Descriptive alt text on every photo.
- **quote-request-form:** labeled fields (name, phone, product of interest, message); error text under the field on blur and on submit; success replaces form inline; failure keeps values and offers phone/Zalo.
- **craftsman-story-block:** photo carousel/grid (manual only — never auto-advancing), rounded-lg, caption line; video slot omitted entirely when no asset. Lightbox (if any) never stacks another modal.
- **trust-block:** short Phạm Trí Trong quote on surface-sunken, embedded on Trống subcategory pages and every PDP; supplements the story page.
- **Banned:** cart/checkout, login UI, auto-rotating carousels, infinite scroll (use pagination/"load more"), modal-on-modal.
- Voice: calm, specific craft detail, no hype superlatives or urgency framing. Page prose is authored by the client in the CMS, not by developers.

## Cross-Story Dependencies

- Depends on Epic 1: Site B `Site` record, per-page SEO fields, `SiteSettings` contact data, shared quote form + `FormSubmission` + email notification.
- Story 2.1 (tokens, shell, nav, sticky bar) precedes all other Epic 2 stories and is reused by Epics 3 (landing page drops nav/sticky bar but keeps tokens), 4, and 5.
- Story 2.2 establishes the Product Post type and product-card grid (including the 1-up <480px breakpoint) reused as-is by 2.3 and linked to 2.4's PDP; 2.4's trust-block slot and 2.5's trust-block component must meet (whichever lands second wires it in).
- Story 2.5's story page must be linked from the Trống hub, Thùng rượu gỗ, and Bồn tắm gỗ pages built in 2.2/2.3.
- PDP spec rows borrow the row treatment of `drum-config-reference`, which itself ships later (Epic 4, should-have) — build the row style in 2.4 so Epic 4 can reuse it, not the other way round.
- Final Site B URL/slug structure must be frozen before Epic 8 creates cross-domain redirects from legacy tbtruonghoc.com trống URLs.
