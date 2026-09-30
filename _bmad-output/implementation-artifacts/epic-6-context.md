# Epic 6 Context: Site A — Danh mục sản phẩm & Luồng sản phẩm (cần thi công / ship thẳng)

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

This epic builds the public face of Site A (tbtruonghoc.com, Ngọc Anh school equipment) in its own "Xanh Lục Bảo Rạng Rỡ" teal/amber system. It covers the page shell (nav with the "Sản phẩm" dropdown, floating contact chip, homepage hero, footer strip), the homepage, the aggregate "Danh mục sản phẩm" page with search and filters, 11 SEO category pages, and two product-detail-page variants. Shippable products get a shipping note and a "Yêu cầu báo giá" CTA that opens the general contact form. Installation-required products get the service area, a 3-step process strip, and the in-page "Đăng ký khảo sát miễn phí" survey modal, which warns about out-of-area locations but never blocks. The audience is B2B/institutional (principals, preschool directors, procurement staff) spending an institution's budget, so the site must read "chuyên nghiệp, đáng tin cậy": spacious, desktop-first and never promo-driven. Site A yields to Site B for build time. Its shared platform (Epic 1) and the catalog content model (proven on Site B in Epic 2) already exist.

## Stories

- Story 6.1: Khung trang Site A (nav, hero, footer) — Xanh Lục Bảo Rạng Rỡ
- Story 6.2: Trang chủ & trang tổng hợp danh mục (FR-6)
- Story 6.3: 11 trang danh mục sản phẩm (FR-6)
- Story 6.4: Trang chi tiết sản phẩm ship thẳng (FR-6)
- Story 6.5: Trang chi tiết sản phẩm cần thi công & form Đăng ký khảo sát miễn phí (FR-6, FR-8)

## Requirements & Constraints

- The 11 categories are: dù che nắng sân trường, nội thất mầm non, quần áo nghi thức & cờ đội, thiết bị âm thanh–máy chiếu, bảng tương tác, màn hình LED hiển thị, thiết bị văn phòng, phòng thí nghiệm Lý–Hóa–Sinh, bàn thí nghiệm, thiết bị–đồ dùng dạy học, thiết bị mầm non ngoài trời. Each one needs a unique URL and its own title, meta and content. Priority keywords include dù che nắng and dù che nắng sân trường. SKU/spec lists for the newer categories are not enumerated yet, so build CMS-driven templates and never hardcode products.
- **Adding categories must never need a code change.** This applies to the nav dropdown, the aggregate grid and any count copy: "Xem tất cả 11 nhóm sản phẩm" must not be a hardcoded "11".
- The survey form (FR-8) needs, at minimum, installation location/address, product of interest, name and phone, so a technician can schedule a survey from those fields alone. The service bound "Miền Bắc – Thanh Hóa" is shown near the CTA. An out-of-area location **warns but never blocks** (a resolved decision). Don't generalize this into "forms never validate".
- There is no cart, checkout or account anywhere. Every path ends in a phone call, Zalo, the general form or the survey form. Pages are Vietnamese only.
- Accessibility: WCAG 2.1 AA (assumed, not confirmed with the client).
  - Survey fields each have a visible label, a visible required marker and `aria-required`.
  - The warning banner uses an icon, a headline and body text, never color alone.
  - Every contact chip and CTA has an accessible label naming its destination, and every product photo has descriptive alt text.
  - The nav dropdown works fully from the keyboard: Enter/focus opens it, arrows or Tab move through it, Escape closes it.
  - Focus order follows visual order. On a PDP: nav → breadcrumb → gallery/info → specs → service-area/process-strip → footer.
- Core Web Vitals must not regress against the legacy site.
- Voice is professional, procedural B2B. No "Giảm giá"/discount badges, urgency or hype superlatives (that is the rejected legacy register). Say "Giao hàng toàn quốc" only on genuinely shippable products. Clients write the page prose in the CMS; developers don't.
- Trust claims must be true. A certification badge appears only when a real certification is on file. The photo badge "Hình ảnh thi công thực tế" appears only on genuine project photos, never on stock, placeholder or AI images.

## Technical Decisions

- **Content shape:**
  - Each category is an Archive page with Product Post children.
  - The homepage and the aggregate "Danh mục sản phẩm" page are standalone Pages, excluded from archive listings.
  - The aggregate page (and the dropdown) get their category list by querying the site's Archive pages at render time, never from a hand-curated list.
  - Category → product is two levels deep, with no sub-category nesting.
- **Reuse, don't fork, the shared Product Post type.** It has an optional free-form price field; blank means contact-for-quote. The Site A PDP renders that field as the two-state `price-block`: a real price in `price` typography with a bulk-order note, or "Liên hệ để nhận báo giá" in `price-fallback` typography in the same position. This is decided per product, not per category. **Gap:** the planning docs don't say where the installation-required vs. shippable flag lives (on the category or on the product). Decide this in 6.4/6.5 and keep it CMS-editable.
- **Leads:** both forms reuse Epic 1's pipeline: the shared `FormSubmission` table, client-side fetch submission, email notification and the Leads Manager. Never use a form post with a redirect, and never use Piranha Comments.
  - The general form ("Yêu cầu báo giá") prefills `product_of_interest`.
  - The survey form writes `form_type = 'survey'` and fills `location_address` and `is_outside_service_area`. A test that a non-default `formType` is saved verbatim was deferred until this story; add it in 6.5.
  - Success replaces the form (or the modal content) inline. On failure, entered values are kept and phone/Zalo are offered.
- **Contact data:** the phone/Zalo in the contact chip and footer come from the per-site `SiteSettings`, never hardcoded. `tel:` and the Zalo deep link must be real links with a web fallback.
- **Per-site shell:** Site B has its own layout. Site A needs its own shell and tokens with **no shared palette, typeface or component styling with Site B's Mộc Trầm system**. Shared *behavior* (form pipeline, gallery/image blocks, product model, SEO fields) is fine to reuse; restyle it under Site A tokens.
- **Design tokens:**
  - Typeface: Mulish 400/600/700/800 (Vietnamese subset) with a system fallback stack.
  - Colors: bg `#FCFEFD`, surface `#FFFFFF`, primary/banner `#0E7A6C`, text `#123832`, text-muted `#4F6E68`, border-neutral `#E3ECE9` (default hairline), cta `#E3A63B` with cta-text `#2B1B02`, trust-bg/border/ink `#DFF6EE`/`#B7E7D9`/`#0E7A6C`, surface-tint/border-tint `#EFF8F6`/`#BFE3DA` (price-block, service-area, process-strip, shipping-note), warning-bg/border/ink `#FFF7E8`/`#F0D7A0`/`#6B4E10`, error `#C0511F` (confirmed only as the required asterisk).
  - Type roles: display 34/800 (hero only), heading-lg 25/800 (PDP H1), body 13.5, price 24/800, price-fallback 20/800, cta 14/700.
  - Spacing: gutter 40px, content max 1200px, section-gap 48px, card-gap 20px, field-gap 14px.
  - Radii: 6/8/10/12, plus full for pills.
  - Only three shadows exist: contact chip, dropdown panel and survey modal (with an `rgba(18,56,50,.62)` backdrop). Cards and blocks are flat, with no shadows.
  - The only gradient is the hero photo overlay.
- The Site A DESIGN.md/EXPERIENCE.md pair wins over the Stitch export on any conflict. Known Stitch defects to fix in code, not re-render: the missing homepage desktop nav, the duplicated tablet nav on the aggregate page, and the shippable CTA text, which must read exactly "Yêu cầu báo giá".

## UX & Interaction Patterns

- **nav:** a solid teal bar with the "NGỌC ANH" wordmark and flat links: Trang chủ · Giới thiệu · Sản phẩm · Tin tức · Liên hệ.
  - "Sản phẩm" opens a single-column dropdown of all categories, **never a mega-menu**. It ends in a divider and the primary-colored "Xem tất cả … nhóm sản phẩm →" link. It opens on hover with a click fallback and closes on click-away.
  - The floating `contact-chip` (hotline + Zalo) is always visible at the right edge and never scrolls away.
- **Mobile/tablet (below desktop):**
  - Logo plus hamburger. The hamburger opens a full-screen teal sheet sliding in from the right, with a close ×. "Sản phẩm" expands inline as an accordion listing every category.
  - The contact chip stays visible as a small icon outside the menu.
  - Grids go 2 columns on tablet and 1 on phone.
  - PDPs stack gallery → info → price → service-area → process-strip (can go vertical) → full-width stacked CTAs.
  - The survey modal becomes a full-screen bottom sheet on phones.
  - No mobile mock was ever reviewed. Confirm visually.
- **Hero:** at ≥768px with photos available, show the photo carousel with a bottom-weighted teal gradient. With no photos, or below 768px **always**, show the solid-teal `hero-fallback` with the same heading and CTAs and no controls, so nav and hero read as one band. Never render a broken or empty hero. Carousel auto-advance is unspecified and left as an implementation default.
- **Homepage:** hero → 6 curated `category-tile`s (3 columns on desktop) + "Xem tất cả" → `trust-stat` band (e.g. 500+ trường, 20 năm) **below** the grid, informational only → footer-strip.
- **Aggregate page:** breadcrumb → header → `category-search` → full CMS-driven tile grid plus a visibly distinct placeholder tile.
  - Search is a text input that filters the grid live on the client.
  - Filter chips are multi-select pills. They match the trust-badge look when active. A category may appear under several chips, since the grouping is loose and by use-case.
- **breadcrumb:** appears on category pages, PDPs and the aggregate page, never on the homepage. Format: Trang chủ / Sản phẩm / [danh mục] / [sản phẩm]. Primary-colored links, current step in plain text, caption size.
- **Empty category (0 published products):** hidden from the dropdown, homepage grid and aggregate page. It is never shown as an empty page. This rule is extrapolated from Site B's.
- **PDP (both variants):** 2 columns (gallery/info, 46px gap). Gallery, then H1, specs table, `price-block`, `trust-badge` mini-trust chips (informational only) and the photo badge where genuine.
  - Shippable: `shipping-note` chip "Giao hàng toàn quốc — 3–7 ngày làm việc tùy khu vực". Primary CTA "Yêu cầu báo giá" opens the general form. No service-area or process-strip.
  - Installation-required: `service-area` block with the heading "Khu vực phục vụ: Miền Bắc – Thanh Hóa" and a nested warning-colored note that out-of-area requests are still accepted. Then the `process-strip` Khảo sát → Hợp đồng → Thi công, and the primary CTA "Đăng ký khảo sát miễn phí".
- **CTA hierarchy (never invert):** the primary button is solid amber and always the highest-intent action. The secondary button is a teal outline for Gọi ngay · Zalo tư vấn.
- **survey-form-modal:**
  - Opens as a centered in-page overlay with a dimmed/blurred backdrop, never as a navigation. It is the only modal and never stacks another on top.
  - Fields: sản phẩm quan tâm (prefilled from the page, read-only, surface-tint styled), then địa điểm/địa chỉ*, họ tên*, số điện thoại*, then a full-width amber submit button.
  - Location outside the service area: an amber `warning-banner` appears between địa điểm and họ tên, and the field border tints amber. Submit stays enabled and submission succeeds. The service-area check is live, against free text or a structured field (implementation's choice).
- **footer-strip:** a thin teal bar with the brand name in bold white on the left and a Zalo/phone/email line in on-primary on the right.

## Cross-Story Dependencies

- Depends on Epic 1:
  - Site A `Site` record and per-page SEO fields.
  - `SiteSettings` contact data.
  - The shared form, `FormSubmission` (with its reserved `form_type`), email notification and Leads Manager.
- Epic 2 already introduced the Product Archive/Post model and a PDP gallery. Reuse the model and behavior, but not Site B's visuals.
- 6.1 (tokens, shell, nav, dropdown, contact chip, footer, CTA styles) comes before everything else.
- 6.3's category Archives feed 6.2's homepage and aggregate grids and 6.1's dropdown. All three must stay in sync and share the empty-category hiding rule.
- 6.4 builds the shared PDP scaffold (price-block, trust-badge, photo-badge, breadcrumb, alt text). 6.5 adds the installation variant and the survey modal on top of it, and wires `formType: "survey"`.
- Epic 7 (Site A blog) reuses this shell and fills the "Tin tức" nav link.
- Epic 8 needs Site A's final category/product URLs frozen before creating same-domain redirects. Examples: the legacy `bo-dong-phuc-nghi-thuc-doi` and `cac-loai-co` pages, which rank organically; migrate their intent-matching content into the quần áo nghi thức & cờ đội category.
