# Epic 6 Context: Site A — Danh mục sản phẩm & Luồng sản phẩm (cần thi công / ship thẳng)

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Build the public face of Site A (tbtruonghoc.com, Ngọc Anh school equipment) in its own "Xanh Lục Bảo Rạng Rỡ" teal/amber system. The epic covers the page shell (nav with "Sản phẩm" dropdown, floating contact chip, homepage hero, footer strip), the homepage, the aggregate "Danh mục sản phẩm" page with search and filters, 11 SEO category pages, and two product-detail-page variants. Shippable products show a shipping note and a "Yêu cầu báo giá" CTA that opens the general contact form. Installation-required products show the service area, a 3-step process strip and an in-page "Đăng ký khảo sát miễn phí" survey modal that warns on out-of-area locations but never blocks. The audience is B2B/institutional (principals, preschool directors, procurement staff), so the site must read professional and trustworthy, never promo-driven.

## Stories

- Story 6.1: Khung trang Site A (nav, hero, footer) — Xanh Lục Bảo Rạng Rỡ
- Story 6.2: Trang chủ & trang tổng hợp danh mục (FR-6)
- Story 6.3: 11 trang danh mục sản phẩm (FR-6)
- Story 6.4: Trang chi tiết sản phẩm ship thẳng (FR-6)
- Story 6.5: Trang chi tiết sản phẩm cần thi công & form Đăng ký khảo sát miễn phí (FR-6, FR-8)

## Requirements & Constraints

- Each of the 11 categories has a unique URL with its own title, meta and content. SKU/spec lists for newer categories are not enumerated, so templates are CMS-driven; never hardcode products.
- Adding a category must never need a code change: nav dropdown, aggregate grid and count copy ("Xem tất cả … nhóm sản phẩm") all derive from the CMS.
- A category with 0 published products is hidden from the dropdown, homepage grid and aggregate page.
- Survey form (FR-8) minimum fields: location/address, product of interest, name, phone. That must be enough for a technician to schedule a survey. Out-of-area (outside Miền Bắc – Thanh Hóa) **warns but never blocks**. This is the only resolved non-blocking validation rule; don't generalize it.
- No cart, checkout or account anywhere. Every path ends in phone, Zalo, the general form or the survey form. Vietnamese only.
- Trust claims must be true. Certification badges appear only for real certifications on file. The photo badge "Hình ảnh thi công thực tế" goes only on genuine project photos, never stock, placeholder or AI imagery. "Giao hàng toàn quốc" appears only on genuinely shippable products.
- Accessibility (WCAG 2.1 AA, assumed): survey fields have visible labels, a visible required marker and `aria-required`. The warning banner uses icon, headline and text, never color alone. CTAs and contact chips have accessible labels naming their destination. Product photos have descriptive alt text. Focus order on a PDP: nav → breadcrumb → gallery/info → specs → service-area/process-strip → footer.
- Core Web Vitals must not regress against the legacy site.

## Technical Decisions

- **Content shape:** each category is an Archive page with Product Post children, two levels deep, with no sub-categories. The homepage and the aggregate page are standalone Pages. The aggregate page and the dropdown query the site's Archive pages at render time.
- **Pricing:** reuse the shared Product Post's optional free-form price field. The two-state `price-block` is decided per product, not per category. A real price renders in `price` typography with a bulk-order note. A blank price renders "Liên hệ để nhận báo giá" in `price-fallback` typography in the same position. An installation-required product with a price is valid. Never model a product as a Landing Page to show a price.
- **Installation vs. shippable flag:** the planning docs don't say where this lives (category or product). Decide it in 6.4/6.5 and keep it CMS-editable.
- **Leads:** reuse Epic 1's pipeline: the shared `FormSubmission` table, client-side fetch, email notification and Leads Manager. Never use a form post with redirect, and never use Piranha Comments.
  - The general form prefills `product_of_interest`.
  - The survey writes `form_type = 'survey'` plus `location_address` and `is_outside_service_area`. A test that a non-default `formType` is saved verbatim was deferred to 6.5.
  - On success, a confirmation replaces the form or modal content inline. On failure, entered values are kept and phone/Zalo are offered.
- **Contact data:** phone, Zalo and Maps come from the per-site `SiteSettings` and are never hardcoded.
- **Visual separation:** Site A has its own shell and tokens, sharing no palette, typeface or component styling with Site B's Mộc Trầm. Shared *behavior* (form pipeline, gallery, product model, SEO fields) is reused and restyled under Site A tokens.
- **Key tokens:**
  - Typeface: Mulish 400–800.
  - Colors: primary `#0E7A6C`, cta `#E3A63B` / cta-text `#2B1B02`, text `#123832`, muted `#4F6E68`, border `#E3ECE9`.
  - surface-tint/border-tint `#EFF8F6`/`#BFE3DA`, used by price-block, service-area, process-strip and shipping-note.
  - warning bg/border/ink `#FFF7E8`/`#F0D7A0`/`#6B4E10`. error `#C0511F`, used only for the required asterisk.
  - Type: price 24/800, price-fallback 20/800, heading-lg 25/800 (PDP H1).
  - Cards and blocks are flat. Shadows only on the contact chip, dropdown and survey modal (backdrop `rgba(18,56,50,.62)`).
- The Site A DESIGN/EXPERIENCE specs win over the Stitch export. The shippable CTA text must read exactly "Yêu cầu báo giá".

## UX & Interaction Patterns

- **CTA hierarchy (never invert):** the primary button is solid amber and is the highest-intent action. The secondary button is a teal outline for Gọi ngay · Zalo tư vấn.
- **Breadcrumb** on category pages, PDPs and the aggregate page, never on the homepage. Format: Trang chủ / Sản phẩm / [danh mục] / [sản phẩm]. Primary-colored links; the current step is plain text.
- **Hero** (finalized in the walkthrough): full-bleed photo carousel with no overlay. Text and CTAs sit in a frosted, blurred box. Below 768px the solid-teal fallback always shows. Never a broken or empty hero, and no auto-rotation.
- **PDP, both variants:** 2 columns (gallery/info). Gallery, H1, specs table, `price-block`, then `trust-badge` chips (informational only) and the photo badge where genuine.
  - **Shippable:** `shipping-note` chip "Giao hàng toàn quốc — 3–7 ngày làm việc tùy khu vực". Primary CTA "Yêu cầu báo giá" opens the general form. No service-area and no process-strip.
  - **Installation-required:** `service-area` block "Khu vực phục vụ: Miền Bắc – Thanh Hóa" with a warning-colored note that out-of-area requests are still accepted. `process-strip` Khảo sát → Hợp đồng → Thi công. Primary CTA "Đăng ký khảo sát miễn phí".
- **survey-form-modal:**
  - Opens as an in-page overlay with a dimmed/blurred backdrop, never a navigation. It is the only modal; nothing stacks on top.
  - Fields: sản phẩm quan tâm (prefilled, read-only, surface-tint), then địa điểm/địa chỉ*, họ tên*, số điện thoại*, then a full-width amber submit.
  - When the location is out of area, an amber `warning-banner` appears between địa điểm and họ tên and that field's border tints amber. Submit stays enabled.
  - The check runs live on free text or a structured field (implementer's choice).
  - On phones the modal becomes a full-screen bottom sheet.
- **Mobile:** grids stack (2 columns on tablet, 1 on phone). PDPs stack gallery → info → price → service-area → process-strip → full-width stacked CTAs.

## Cross-Story Dependencies

- Depends on Epic 1: Site A `Site` record, per-page SEO fields, `SiteSettings`, form pipeline and `FormSubmission` (with `form_type`), email notification and Leads Manager.
- Reuses Epic 2's Product Archive/Post model and gallery behavior, but not Site B's visuals.
- 6.1 shell and tokens underpin everything. 6.3's category Archives feed 6.2's grids and 6.1's dropdown, which all share the empty-category rule.
- 6.4 builds the shared PDP scaffold: price-block, trust-badge, photo-badge, breadcrumb, alt text. 6.5 layers the installation variant and the survey modal on top and wires `formType: "survey"`.
- Epic 7 reuses this shell for "Tin tức". Epic 8 needs Site A's category/product URLs frozen before redirects. The legacy `bo-dong-phuc-nghi-thuc-doi` and `cac-loai-co` pages map into quần áo nghi thức & cờ đội.
