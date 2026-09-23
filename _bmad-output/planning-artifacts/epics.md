---
stepsCompleted: [step-01-validate-prerequisites, step-02-design-epics, step-03-create-stories]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-tbtruonghoc-2026-09-17/prd.md
  - _bmad-output/planning-artifacts/prds/prd-tbtruonghoc-2026-09-17/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-tbtruonghoc-2026-09-18/ARCHITECTURE-SPINE.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tbtruonghoc-2026-09-18/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tbtruonghoc-2026-09-18/EXPERIENCE.md
  - _bmad-output/planning-artifacts/ux-designs/ux-trongdoitam-2026-09-17/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-trongdoitam-2026-09-17/EXPERIENCE.md
---

# Ngọc Anh Multi-Site Rebuild — Phase 1 (tbtruonghoc + trongdoitam.net) - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Phase 1 of the Ngọc Anh multi-site rebuild — Site A (tbtruonghoc.com, institutional school equipment) and Site B (trongdoitam.net, Đọi Tam heritage woodcraft) — built on one shared Piranha CMS instance, decomposing the requirements from the PRD, both sites' UX Design contracts, and the Architecture Spine into implementable stories. Site C (trongngocanh.com) is explicitly out of scope for this phase.

## Requirements Inventory

### Functional Requirements

**Shared / cross-site foundations**

FR-1: Content editors can set title tag, meta description, and URL slug on every page, on both Piranha instances, without developer involvement.

FR-2: Every page on both sites displays click-to-call, a Zalo chat entry point, and Google Maps, using the correct contact details/location for that site's business line.

FR-3: A lightweight contact/quote-request form is embeddable on any product/category page on either site; every submission is retrievable after the fact with all submitted fields intact.

FR-4: Each site reports to its own Google Analytics property and is separately verified/submitted in Search Console, with its own sitemap and indexing status from launch.

FR-5: Site A and Site B (and Site C, later) link to each other only through contextual, editorial links embedded in relevant content — never a site-wide footer/sidebar link block (link-scheme/PBN risk mitigation).

**Site A — tbtruonghoc (thiết bị trường học)**

FR-6: Each of the 11 confirmed Site A categories gets its own page with its own target keyword set, resolving to a unique URL with category-specific title/meta/content. `[ASSUMPTION: exact SKU/spec list for the newly-scoped-in categories not yet enumerated — PRD Open Question 1.]`

FR-7: Site A blog/news section for longer-form, advice-style SEO content, discoverable from navigation and indexable independent of product pages.

FR-8: "Đăng ký khảo sát miễn phí" (free survey request) form for installation-required categories, distinct from the general contact form — minimum fields: installation location/address, product of interest, callback name + phone. A visitor entering a location outside Miền Bắc–Thanh Hóa sees a non-blocking warning but can still submit.

**Site B — trongdoitam.net (đồ gỗ thủ công Đọi Tam)**

FR-9: Trống (all types, unified), thùng rượu gỗ trang trí, and bồn tắm gỗ each get their own SEO-optimized page(s) with line-specific title/meta/content and distinct URLs. `[ASSUMPTION: bồn tắm gỗ is a from-scratch line with no existing content/pricing/photos — PRD Open Question 9.]`

FR-10: A craftsman/heritage story page tells the Đọi Tam workshop/craftsman story (photos at minimum, video should-have), linked from all three Site B product-line pages — not buried in a generic About page. `[ASSUMPTION: video asset availability/timeline unconfirmed — PRD Open Question 3.]`

FR-11 (should-have): A drum configuration reference (size, loại 1/2/3, bánh xe, sơn, vẽ mặt trống) as a read-only reference table/lightweight selector, always ending in a quote request, never a checkout flow. Sequencing against the Tết 2027 deadline is an open question (PRD Open Question 4).

FR-12: A dedicated paid-ads landing page type, distinct from organic Site B category/product pages, excluded from primary navigation, reusable for future campaigns. At least one landing page must exist for the thùng rượu gỗ line before Tết Âm Lịch 2027 (~mid-February 2027).

FR-13: Landing pages display specific, real per-item/variant pricing (unlike Site B's organic product pages, which stay contact-for-quote). Each wine-barrel variant (gỗ sồi, ngựa kéo, 1 ngựa, 2 ngựa) shows its own price, with a "Đặt mua ngay" CTA routing to a contact form/Zalo/phone — no cart or online payment.

FR-14: Site B blog/content section covering craft/heritage topics, wood-care advice, and Tết gifting guides — same Archive+Post pattern as FR-7 (own per-page SEO, indexable, discoverable from navigation, separate from the product catalog).

### NonFunctional Requirements

NFR-1: Page load speed/Core Web Vitals (LCP, CLS, INP) must not regress on either site, measured against a baseline captured from the legacy tbtruonghoc.com site before Site A/Site B launch (SM-C2). Capturing this legacy baseline is a required pre-launch acceptance step.

NFR-2: Phone/Zalo contact volume/rate must not decrease on either site as organic SEO investment increases (SM-C1) — SEO/content growth must not come at the expense of the existing lead-generation channel.

NFR-3: Both sites are Vietnamese-only, domestic-market-only — no multi-language/internationalization support.

NFR-4: No shopping cart or online checkout/payment anywhere in v1, on either site, including the wine-barrel landing page despite its visible pricing.

NFR-5: WCAG 2.1 AA is the assumed accessibility floor site-wide on both sites `[ASSUMPTION: not directly elicited/confirmed with the client on either site — flagged for triage in both UX contracts' Open Items.]`.

NFR-6: Automated daily database backup (mariadb-dump, mysqldump-compatible) of the shared MariaDB database, retained 14 days, stored on the CentOS host — protects both sites at once since they share one database (AD-6).

NFR-7: Local dev/prod parity — Docker Compose local dev must pin the MariaDB image tag to 10.11 (matching production exactly), never `:latest`, so a migration/SQL-dialect gap cannot hide locally and surface only in production.

NFR-8: Editorial cross-site linking (FR-5) must never scale into an automated, sitewide, or reciprocal link exchange between the two commonly-owned domains — a known Google link-scheme/PBN risk pattern.

### Additional Requirements

- **No starter/greenfield template beyond the official scaffold**: project is bootstrapped from the `piranha.mvc` Piranha.Templates scaffold (12.0.0) — relevant to Epic 1 Story 1.
- **Single Piranha CMS instance, native multi-site (AD-1)**: Site A and Site B are two `Site` records inside one Piranha installation, one database, one deployment, one media library, one Manager login — never split into per-site deployments, and a second Piranha instance must never be stood up "for Site B." `Site` records (hostnames, `IsDefault`) are created once during initial project scaffold/setup.
- **Content shape convention (AD-2)**: every danh mục category and both blogs are Archive pages + Post children (native pagination/categories/tags). Landing pages, the craftsman story page, the homepage, and the aggregate category hub page are standalone Pages, excluded from any Archive listing. The aggregate "Danh mục sản phẩm" hub page must source its category list dynamically by querying the site's Archive pages at render time — never a hand-curated or hardcoded link list.
- **Lead capture: custom storage, not Piranha Comments (AD-3)**: FR-3 and FR-8 submissions write to one shared custom EF Core `FormSubmission` table (fixed schema: `id, site_id, form_type, name, phone, product_of_interest, message, location_address` nullable, `is_outside_service_area` bool nullable, `created_at`) — never Piranha's built-in Comment/PostComment model. A custom Piranha Manager extension module ("Danh sách khách để lại thông tin") lists/details every submission. An email notification fires on every new submission. Both submission endpoints are client-side (fetch/AJAX) — never a traditional form-post-with-redirect.
- **Redirects via Piranha's native Alias (AD-4)**: all 301/302 redirects (same-domain Site A restructuring and cross-domain legacy trống URLs → trongdoitam.net) use `IApi.Aliases`/`AliasRouter`, scoped (`SiteId`) to the site where the OLD URL lived. Cross-domain aliases are created only after Site B's final URL/slug structure is frozen.
- **Pricing model split (AD-5)**: the Product Post Type carries an optional, flexible price field (blank / exact value / range / "từ X"); the Landing Page Type separately carries its own repeatable variant+price region for structured multi-variant paid pricing. A catalog product must never be modeled as a Landing Page instance merely to display a price.
- **Per-site settings singleton**: a single `SiteSettings` SiteType (one instance per Piranha `Site`) holds GA4 measurement ID/Search Console verification (FR-4) and phone/Zalo/Maps contact details (FR-2) — every view reads from it, neither is ever hardcoded per-view.
- **Stack**: .NET 8, Piranha CMS 12.0.0, Piranha.Templates (`piranha.mvc` scaffold) 12.0.0, Piranha.Data.EF.MySql 12.0.0 (Pomelo, `MariaDbServerVersion`), MariaDB 10.11 LTS, CentOS Stream 9 self-hosted. `.NET 8 reaches End of Support 2026-11-10 — track Piranha's net10.0 support and upgrade as soon as it ships; flagged as a near-term operational risk, not fixed in phase 1 scope.`
- **Legacy content migration**: content for all categories/lines on both sites must be migrated and rewritten to SEO standard from the legacy tbtruonghoc.com content as the starting dataset (delivery task, not an end-user-facing FR) — see addendum.md's legacy nav/sub-category audit and the two proven organic-search pages (bo-dong-phuc-nghi-thuc-doi, cac-loai-co) whose intent-matching content should be preserved in rewritten copy.
- **Build sequencing**: Site B is prioritized ahead of Site A because of its fixed external Tết Âm Lịch 2027 (~mid-February 2027) deadline; Site A proceeds in parallel where capacity allows but yields to Site B when the two compete for build time. This is a priority ordering, not a fixed schedule — it should shape epic/story sequencing.

### UX Design Requirements

**Site A (tbtruonghoc.com) — "Xanh Lục Bảo Rạng Rỡ" design system**

UX-DR1: Implement the Site A design token set from DESIGN.md — Mulish typeface (400/600/700/800, Vietnamese subset, system-font fallback stack), the teal+amber color palette (primary `#0E7A6C`, cta `#E3A63B`, plus the full banner/trust/surface-tint/warning token families), the spacing scale (`gutter` 40px, `section-gap` 48px, `card-gap` 20px), and the radius scale (`sm` 6px – `xl` 12px).

UX-DR2: Build the `nav` component — solid teal banner bar on every page, wordmark + flat link list, a "Sản phẩm" dropdown (never a mega-menu) listing all 11 categories and ending in a "Xem tất cả 11 nhóm sản phẩm →" link; a persistent floating `contact-chip` (hotline + Zalo) at the nav's right edge on every page, never scrolled away.

UX-DR3: Build `hero-carousel` and `hero-fallback` for the homepage — full-bleed photo carousel with gradient overlay when photography exists; solid-teal fallback banner (same heading/CTA, no controls) when it does not. `hero-fallback` renders unconditionally at phone width (<768px) regardless of photo availability.

UX-DR4: Build `category-tile` — homepage shows a curated 6 of 11 categories plus "Xem tất cả"; the aggregate "Danh mục sản phẩm" page renders the full, CMS-driven category count plus a visibly distinct placeholder tile, with optional per-category certification badges shown only when a real, verifiable certification is on file.

UX-DR5: Build the aggregate "Danh mục sản phẩm" page with `category-search` — a live client-side text search plus toggleable, multi-select filter chips grouping categories loosely by use-case (a category may appear under more than one chip).

UX-DR6: Build the two product-detail-page variants: installation-required (adds `service-area` block stating "Khu vực phục vụ: Miền Bắc – Thanh Hóa" plus a `process-strip` 3-step Khảo sát → Hợp đồng → Thi công visual; primary CTA "Đăng ký khảo sát miễn phí") and shippable (no service-area/process-strip; adds a `shipping-note` chip "Giao hàng toàn quốc — 3–7 ngày làm việc tùy khu vực"; primary CTA "Yêu cầu báo giá" routing to the general contact form).

UX-DR7: Build `price-block` with two CMS-conditional states in the same block position/styling — a real price (`typography.price`) or "Liên hệ để nhận báo giá" (`typography.price-fallback`) — evaluated per product regardless of category (an installation-required product with a set price, or a shippable product with none, are both architecturally valid).

UX-DR8: Build `survey-form-modal` (FR-8) as an in-page overlay (not a page navigation) with a dimmed/blurred backdrop — fields: sản phẩm quan tâm (prefilled/read-only, linked from page context), địa điểm/địa chỉ*, họ tên*, số điện thoại*. A non-blocking `warning-banner` (amber, never red) appears between địa điểm and họ tên when the entered location falls outside Miền Bắc–Thanh Hóa; the affected field's border also tints amber; submit stays enabled and functional.

UX-DR9: Build `breadcrumb` on every category page, PDP, and the aggregate page (absent on the homepage) — plain text trail, `primary`-colored links for every step except the current page.

UX-DR10: Build `trust-badge` (PDP mini-trust chips, e.g. "Bảo hành chính hãng") and `trust-stat` (homepage numeric stat band, e.g. "500+ trường đã lắp đặt") — both non-interactive/informational only; `trust-stat` placed below the category grid, not above the fold.

UX-DR11: Build `photo-badge` ("Hình ảnh thi công thực tế") for PDP gallery images — applied only to genuine, non-stock, non-AI-generated project photography.

UX-DR12: Implement the sitewide `cta-buttons` hierarchy — primary (solid amber) is always the higher-intent action in context (Đăng ký khảo sát miễn phí, Yêu cầu báo giá, Gửi đăng ký, Đặt mua ngay); secondary (teal outline) is always the phone/Zalo fallback (Gọi ngay · Zalo tư vấn). Never invert.

UX-DR13: Build `footer-strip` — thin solid-teal bar on every page with bold white brand name and a contact line (Zalo/phone/email).

UX-DR14: Hide an empty category (0 published products) from the category grid entirely rather than rendering a bare "no products" tile/page. `[ASSUMPTION: extrapolated from the explicit CMS/IA constraint that categories must be addable dynamically — not directly discussed for Site A; consistent with Site B's confirmed equivalent rule.]`

UX-DR15: Implement form submit success/failure states for both the survey form and the general contact form — success shows an inline confirmation message replacing the form in place (no redirect, no separate modal); failure retains entered field values and offers phone/Zalo as an immediate fallback. `[ASSUMPTION: no confirmation/error mock was rendered for Site A — extrapolated from Site B's confirmed equivalent pattern.]`

UX-DR16: Meet the accessibility floor — WCAG 2.1 AA target; individually labeled `survey-form-modal` fields with a visible (not color-only) required marker plus `aria-required` or equivalent; the `warning-banner` perceivable via icon + headline + text, not color alone; accessible labels on every `contact-chip`/CTA describing its destination; fully keyboard-operable `nav-dropdown` (open on focus/Enter, arrow-key/Tab navigation, close on Escape); descriptive alt text on all product photography; focus order follows visual reading order (nav → hero → content → footer on the homepage; nav → breadcrumb → gallery/info → specs → service-area/process-strip → footer on a PDP).

UX-DR17: Implement the mobile nav-collapse pattern per `stitch-prompts.md` direction — hamburger menu sliding from the side, "Sản phẩm" expanding as an inline accordion of all 11 categories; multi-column grids stack to a single column below tablet width. `[ASSUMPTION: not yet visually confirmed against rendered Stitch output — flagged as an open item in EXPERIENCE.md.]`

**Site B (trongdoitam.net) — "Mộc Trầm" design system**

UX-DR18: Implement the Site B design token set from DESIGN.md — a single humanist sans typeface throughout (Be Vietnam Pro or an equivalent with comparable Vietnamese OpenType support), the warm-wood color palette (primary `#8B5A2B`, secondary `#A97142`, error `#A63B2E`, plus surface/surface-sunken/border families), the 4px-based spacing scale (mobile `gutter` 16px / `gutter-desktop` 24px), and the radius scale (`sm` 6px – `lg` 14px).

UX-DR19: Build `nav` — fixed 56px-height bar, flat link list plus a Trống submenu (5 subpages); mobile collapses to a hamburger opening a full-height, single-level sheet; the hotline number (in `phone-number` typography) shows on tablet/desktop only; no account/login icon on any breakpoint or page.

UX-DR20: Build `sticky-contact-bar` — 3 segments (Gọi ngay / Chat Zalo / Bản đồ), fixed to the viewport bottom on every organic page at every breakpoint, never auto-hiding on scroll; the center Zalo segment is visually distinct (primary fill). Absent only on the `landing-page-cta` template.

UX-DR21: Build `category-tile` (CMS-driven, flexible-count homepage category block, never hardcoded to 3) and `product-card` (used on the homepage grid, Trống hub + 5 subpages, Thùng rượu page, Bồn tắm page) with a pressed/active state (2px inset primary ring + 0.98 scale-down) firing on touch-down.

UX-DR22: Build `product-detail-page` — per-model/variant page (drum loại, wine-barrel variant, bathtub line) with its own photos/specs/URL/SEO; photo gallery degrades gracefully to a single photo with no empty chrome; spec rows reuse `drum-config-reference`'s row treatment filtered to one model; price row shows a real price only where genuine, otherwise muted "Liên hệ báo giá" (drum, bathtub, and organic wine-barrel-variant pages all stay contact-for-quote — only `landing-page-cta` shows real pricing); a small sku-code shown near the title; ends in an embedded `quote-request-form`.

UX-DR23: Build `trust-block` — a short craftsman testimonial (attributed to Phạm Trí Trong), embeddable on Trống subcategory pages and any `product-detail-page`, not only the dedicated story page.

UX-DR24: Build `craftsman-story-block` (FR-10) — photo carousel/grid required at launch; the video slot renders only if a video asset exists, with no empty player or "video coming soon" placeholder when absent; captions/subtitles are a condition of shipping video at all, not a follow-up.

UX-DR25: Build `drum-config-reference` (FR-11, should-have) — a read-only reference table (size × loại 1/2/3 × bánh xe × sơn × vẽ mặt trống) with a disclaimer line ("Bảng tham khảo, không tính giá tự động"), always ending in the same quote-request/Zalo/phone CTA. If not shipped by launch, the Trống subpage simply omits the component and routes straight to contact channels — not a broken link.

UX-DR26: Build `landing-page-cta` (FR-12/13) — a distinct page shell with no nav and no 3-segment sticky bar, one full-width CTA per variant card ("Đặt mua ngay"); each variant (gỗ sồi, ngựa kéo, 1 ngựa, 2 ngựa) shows its own real price plus a variant-label chip; routes to an inline `quote-request-form` or Zalo/phone, never a cart/payment step.

UX-DR27: Build `quote-request-form` (FR-3) — embeddable on any product/category page and inline on the landing page; minimum fields name/phone/product-or-category-of-interest/message; inline `error`-colored validation text under the specific invalid field on blur and again on submit (never a top-of-form-only summary); successful submit shows an inline confirmation message in place of the form (no redirect, no modal); failure retains entered values and offers phone/Zalo as an immediate fallback.

UX-DR28: Build `article-detail-page` for Blog posts — title, optional byline/date metadata, running body copy with in-article subheads where needed, ending in a related-posts module (reusing the `product-card` grid convention) or a plain "← Quay lại Blog" back-link when no related posts exist — the page must never dead-end.

UX-DR29: Implement empty-state handling — an empty category/product-card grid (0 items) is hidden from its parent surface entirely, never rendered empty; an empty Blog listing shows a "Bài viết đang được cập nhật" message instead of a bare empty list, and the Blog nav link is never hidden just because it's currently empty.

UX-DR30: Implement the bồn tắm gỗ placeholder-safe template — must not silently publish with a broken-image icon or literal placeholder text; either a clearly labeled "coming soon" content state, or the page stays unpublished until real CMS content lands.

UX-DR31: Meet the accessibility floor — WCAG 2.1 AA target; accessible labels on every `sticky-contact-bar` segment and card CTA describing their destination; descriptive alt text on all product/category photography; captions on video if it ships; individually labeled `quote-request-form` fields with errors announced in adjacent text, never color alone; keyboard-reachable `sticky-contact-bar` without being trapped ahead of primary content; tap targets sized for an older, less web-savvy audience.

UX-DR32: Enforce the interaction ban list — no cart/checkout flows, no account/login UI on any page/breakpoint, no auto-advancing/auto-rotating hero carousels, no infinite scroll on category grids (use pagination or "load more" instead), no modal-on-modal stacking, no vanity social-proof counters (like/view counts) on blog posts.

**Cross-site**

UX-DR33: Keep Site A's and Site B's visual and voice systems fully distinct despite the shared CMS platform — no shared component library, palette, or typeface between "Xanh Lục Bảo Rạng Rỡ" (Site A: teal/amber, Mulish, professional-institutional register) and "Mộc Trầm" (Site B: warm wood, Be Vietnam Pro, calm heritage-craft register) — a visitor should never mistake one site for the other.

### FR Coverage Map

| FR | Epic |
|---|---|
| FR-1 | Epic 1 |
| FR-2 | Epic 1 |
| FR-3 | Epic 1 |
| FR-4 | Epic 1 |
| FR-5 | Epic 8 |
| FR-6 | Epic 6 |
| FR-7 | Epic 7 |
| FR-8 | Epic 6 |
| FR-9 | Epic 2 |
| FR-10 | Epic 2 |
| FR-11 | Epic 4 |
| FR-12 | Epic 3 |
| FR-13 | Epic 3 |
| FR-14 | Epic 5 |

NFRs and UX Design Requirements are not one-to-one with a single epic — they are threaded into the acceptance criteria of whichever epic's stories build the relevant surface (e.g. WCAG AA and empty-state handling ride along with each component's story; NFR-1's Core Web Vitals baseline capture and NFR-6's backup cadence are formalized as their own stories in Epic 8).

## Epic List

### Epic 1: Nền tảng dùng chung — Multi-site, SEO, Liên hệ & Thu thập Lead
Editor chỉnh SEO (title/meta/slug) trên từng trang mà không cần developer; khách truy cập ở cả 2 site luôn thấy điện thoại/Zalo/Maps đúng của từng site và gửi được form liên hệ chung; mọi lead được lưu vào một bảng dùng chung và sales xem được trong Manager extension; mỗi site báo cáo Analytics/Search Console riêng.
**FRs covered:** FR-1, FR-2, FR-3, FR-4

### Epic 2: Site B — Danh mục sản phẩm & Câu chuyện nghệ nhân
Khách xem trống (hub + 5 phân trang), thùng rượu gỗ, và bồn tắm gỗ theo từng trang SEO riêng, xem trang chi tiết từng model/biến thể, đọc trang câu chuyện nghệ nhân Phạm Trí Trong (liên kết từ cả 3 dòng sản phẩm), và gửi yêu cầu báo giá qua form nhúng.
**FRs covered:** FR-9, FR-10

### Epic 3: Site B — Landing page trả phí Thùng rượu gỗ (deadline Tết 2027)
Khách từ quảng cáo trả phí vào thẳng landing page riêng (không nav/sticky-bar), thấy giá thật cho từng biến thể (gỗ sồi, ngựa kéo, 1 ngựa, 2 ngựa), và bấm "Đặt mua ngay" để gửi yêu cầu qua form/Zalo/điện thoại.
**FRs covered:** FR-12, FR-13

### Epic 4: Site B — Bảng tham khảo cấu hình trống (should-have)
Khách xem bảng tham khảo (kích thước × loại 1/2/3 × bánh xe × sơn × vẽ mặt trống) trên các trang phân loại trống trước khi gửi yêu cầu — không tính giá tự động, luôn kết thúc bằng CTA liên hệ.
**FRs covered:** FR-11

### Epic 5: Site B — Blog/Nội dung
Khách đọc bài viết về nghề thủ công, cách bảo quản đồ gỗ, và hướng dẫn quà Tết, độc lập với danh mục sản phẩm.
**FRs covered:** FR-14

### Epic 6: Site A — Danh mục sản phẩm & Luồng sản phẩm (cần thi công / ship thẳng)
Khách duyệt 11 danh mục thiết bị trường học, vào trang chi tiết sản phẩm ở một trong hai biến thể (cần khảo sát-thi công hoặc ship thẳng), và với sản phẩm cần thi công thì mở form "Đăng ký khảo sát miễn phí" ngay trên trang, gửi thành công kể cả khi ở ngoài khu vực phục vụ (chỉ cảnh báo, không chặn).
**FRs covered:** FR-6, FR-8

### Epic 7: Site A — Blog/Tin tức
Khách đọc bài viết tư vấn dài ("nên chọn loại nào," "cách chọn...") phục vụ SEO dài hạn, tách khỏi danh mục sản phẩm.
**FRs covered:** FR-7

### Epic 8: Sẵn sàng ra mắt — Migration, Redirect & Kết nối liên site
Toàn bộ URL cũ (same-domain trên Site A và cross-domain sang trongdoitam.net) chuyển hướng 301/302 đúng qua Piranha Alias; nội dung từ site cũ được migrate/viết lại chuẩn SEO; ít nhất một liên kết biên tập cross-site giữa Site A và Site B tồn tại (không phải link-block tự động); baseline Core Web Vitals của site cũ được chốt trước khi launch; backup DB hàng ngày được bật.
**FRs covered:** FR-5

## Epic 1: Nền tảng dùng chung — Multi-site, SEO, Liên hệ & Thu thập Lead

Editor chỉnh SEO trên từng trang mà không cần developer; khách truy cập ở cả 2 site luôn thấy đúng kênh liên hệ của site đó và gửi được form liên hệ chung; mọi lead được lưu vào một bảng dùng chung và sales xem được trong Manager extension; mỗi site báo cáo Analytics/Search Console riêng. **FRs covered:** FR-1, FR-2, FR-3, FR-4.

### Story 1.1: Khởi tạo Piranha multi-site scaffold cho Site A & Site B

As a site operator,
I want the Piranha CMS project scaffolded from the official `piranha.mvc` template with two `Site` records (tbtruonghoc, trongdoitam.net) inside one instance and one shared MariaDB database,
So that both site builds share one platform from day one instead of diverging into separate deployments.

**Acceptance Criteria:**

**Given** a fresh `piranha.mvc` scaffold (Piranha 12.0.0, .NET 8, Piranha.Data.EF.MySql 12.0.0)
**When** the application starts against a MariaDB 10.11 database
**Then** two `Site` records exist — one for `tbtruonghoc` (marked `IsDefault`), one for `trongdoitam.net`
**And** a request to each site's hostname resolves to its own `Site` record, not a shared/ambiguous default.

**Given** the local development environment
**When** a developer runs `docker compose up`
**Then** the `mariadb` service image is pinned to `10.11` (never `:latest`), matching production exactly
**And** the `piranha-app` container connects successfully to the pinned `mariadb` service with a volume-mounted media folder.

**Given** the project has been scaffolded once
**When** any future story or feature is implemented
**Then** no second Piranha instance or duplicate `Site`/database setup is created "for Site B" — both sites continue to share the one instance created in this story.

### Story 1.2: Trường SEO trên từng trang (FR-1)

As a content editor,
I want to set the title tag, meta description, and URL slug on every Page and Post, on either site, from the Piranha Manager,
So that I can control each page's SEO without asking a developer.

**Acceptance Criteria:**

**Given** a Page or Post being edited in Piranha Manager, on either Site A or Site B
**When** the editor fills in Title, Meta Description, and Slug fields and publishes
**Then** the published page renders that exact title in `<title>`, that exact text in `<meta name="description">`, and resolves at the edited slug's URL.

**Given** a published page with an existing slug
**When** the editor changes the slug post-publish
**Then** the page is reachable at the new slug
**And** existing internal links referencing the page (via Piranha's internal page-reference mechanism, not a hardcoded URL string) continue to resolve correctly.

**Given** any newly created Page or Post type used by a later epic (category page, product page, blog post, landing page)
**When** it is rendered
**Then** it inherits the same editable Title/Meta Description/Slug fields — this is a platform-level capability, not something re-built per page type.

### Story 1.3: Thông tin liên hệ theo từng site (FR-2)

As a visitor to either site,
I want to see click-to-call, a Zalo chat entry point, and Google Maps using the correct contact details for the site I'm on,
So that I can reach the right business line without hunting for contact info.

**Acceptance Criteria:**

**Given** a `SiteSettings` SiteType with phone/Zalo/Maps fields, one instance per Piranha `Site`
**When** an editor fills in Site A's `SiteSettings` with tbtruonghoc's phone/Zalo/Maps details, and Site B's with trongdoitam.net's own details
**Then** every published page template on Site A reads Site A's values, and every page on Site B reads Site B's — no contact detail is hardcoded in any view.

**Given** a published page on either site
**When** a visitor taps the phone contact element
**Then** it triggers a real `tel:` link (not a JavaScript-only handler with no fallback).

**Given** a published page on either site
**When** a visitor taps the Zalo contact element
**Then** it opens the Zalo deep link, with a working web fallback when the Zalo app isn't installed.

**Given** a published page on either site
**When** a visitor views the Maps element
**Then** it shows/links to that site's own business location, not the other site's.

### Story 1.4: Form liên hệ/báo giá chung & lưu trữ lead (FR-3)

As a visitor on any product or category page,
I want to submit a lightweight contact/quote-request form without leaving the page,
So that I can ask for pricing without making a phone call first.

**Acceptance Criteria:**

**Given** a shared `FormSubmission` table (`id, site_id, form_type, name, phone, product_of_interest, message, location_address` nullable, `is_outside_service_area` nullable, `created_at`)
**When** a visitor submits the general contact form (`form_type = 'general'`) on any product/category page, on either site
**Then** a new `FormSubmission` row is created with the correct `site_id` and every submitted field intact
**And** the submission is sent client-side (fetch/AJAX) — no full-page reload or redirect.

**Given** a successful submission
**When** the request completes
**Then** an inline confirmation message replaces the form in place, with no page reload and no separate modal.

**Given** a submission that fails (network/server error)
**When** the visitor sees the failure
**Then** their entered field values are retained (not cleared)
**And** an inline error message offers phone/Zalo as an immediate fallback channel.

**Given** any new `FormSubmission` row, of any `form_type`
**When** it is created
**Then** an email notification fires automatically to the configured recipient.

**Given** the form's fields
**When** it renders
**Then** each field is individually labeled (not placeholder-text-only) and, on a validation failure, the error is announced in text adjacent to the invalid field — never conveyed by color alone.

> **Implementation note (2026-09-23, correct-course):** the "email notification" AC above is realized by Story 1.7, not this story. Story 1.4's shipped scope is the form + `FormSubmission` storage + inline confirmation only — the email send was split out at spec time (token-budget gate) and tracked separately.

### Story 1.5: Quản lý lead trong Piranha Manager (AD-3)

As a sales/ops user,
I want to see every form submission — general or survey — listed with full detail inside Piranha Manager,
So that I can follow up on leads without checking email or relying on pen-and-paper tracking.

**Acceptance Criteria:**

**Given** one or more rows exist in `FormSubmission` across both sites
**When** the sales user opens the "Danh sách khách để lại thông tin" Manager extension module
**Then** all submissions are listed, showing at minimum site, form type, name, phone, and submission date, most recent first.

**Given** the submission list
**When** the user clicks a row
**Then** a detail view shows every field of that submission, including `location_address` and `is_outside_service_area` when present (survey submissions).

**Given** the submission list
**When** the user needs to find submissions from one site only
**Then** the list can be filtered/sorted by site.

### Story 1.6: Analytics & Search Console riêng theo từng site (FR-4)

As the business owner,
I want each site to report to its own Google Analytics property and be separately verified in Search Console,
So that I can measure each site's SEO performance independently.

**Acceptance Criteria:**

**Given** `SiteSettings` holds a GA4 measurement ID and a Search Console verification value per `Site`
**When** a page loads on Site A
**Then** the GA4 tag fires using Site A's own measurement ID, and Site B's pages never fire under Site A's ID (and vice versa).

**Given** either site
**When** its sitemap is requested
**Then** it returns its own `sitemap.xml`, scoped only to its own pages.

**Given** an editor needs to change a site's GA4 ID or Search Console verification value
**When** they update it in `SiteSettings`
**Then** the change takes effect without a code deploy.

### Story 1.7: Thông báo email khi có lead mới (FR-3, AD-3)

As a sales/ops user,
I want to receive an email the moment a new lead comes in,
So that I can follow up quickly without needing to check Piranha Manager proactively.

**Acceptance Criteria:**

**Given** a shared `IFormNotificationService` (single implementation, single outbound email/SMTP configuration — never per-form ad hoc SMTP/API calls)
**When** a new `FormSubmission` row is created, from either form_type ("general" or "survey"), on either site
**Then** an email notification is sent to the configured recipient(s) for that submission's site, containing the submission's key fields (site, form type, name, phone, product of interest/message, and location/service-area flag when present).

**Given** the email send fails (SMTP unavailable, misconfigured, or any transient error)
**When** the failure occurs
**Then** the `FormSubmission` row itself is unaffected (already committed) and the visitor's inline success confirmation is unaffected — the failure is logged only, never retried inline, never surfaced to the visitor (fail-open, per the 2026-09-22 decision recorded in deferred-work.md).

**Given** the notification recipient
**When** a site needs a different recipient than the other
**Then** the recipient address is configurable per site (via `SiteSettings` or equivalent), not hardcoded.

**Given** no SMTP credentials or config keys currently exist in the repo
**When** this story is implemented
**Then** SMTP/API credentials are read from environment/`.env`-sourced configuration, never hardcoded — consistent with the existing connection-string pattern.

### Story 1.8: Site switcher dạng tab trong Piranha Manager (AD-1)

As an editor managing pages across both Site A and Site B,
I want each Piranha Site to appear as its own tab in Manager's page list,
So that I always know how many sites exist and never have to scroll through one site's pages to reach the other's.

**Acceptance Criteria:**

**Given** the configured Piranha `Site` records (currently Site A and Site B)
**When** an editor opens Manager's page list
**Then** each Site appears as its own tab, labeled with the site's name, and the number of tabs always matches the number of configured Sites — no manual tab configuration required when a Site is added or removed.

**Given** an editor is viewing one Site's tab
**When** the page list renders
**Then** it shows only that Site's pages — pages belonging to other Sites never appear in the list, and no cross-site scrolling is required to reach them.

**Given** an editor switches tabs
**When** they select a different Site's tab
**Then** the page list updates to that Site's pages without a full page reload.

**Given** this replaces Piranha's stock site-switcher dropdown on the page-list screen
**When** implemented
**Then** the dropdown is replaced (not left alongside the tabs) so there is exactly one site-selection control, not two conflicting ones.

**Out of scope (explicit):** search/filter of pages within a single site's tab — deferred separately; this story only isolates each site's pages into its own tab.

> **Implementation note (2026-09-23, correct-course):** Piranha's stock page-list view is core Manager UI, not an explicit custom-extension point like Story 1.5's Leads module was — confirm during spec-writing whether this needs a Manager UI override rather than a clean add-on module.

### Story 1.9: [SECURITY] Sửa lỗ hổng bypass validation khi lưu SiteSettings qua Piranha Manager + dọn dữ liệu test rác

As a site admin,
I want the existing Zalo/Maps/GA4/Search-Console save-time safety checks (added in Story 1.3/1.6) to actually apply when I save SiteSettings through Piranha Manager's own UI,
So that an unsafe URL scheme or script value can never persist, not only when application code happens to save a strongly-typed object directly.

**Acceptance Criteria:**

**Given** the `App.Hooks.SiteContent.RegisterOnBeforeSave` hook registered in `Program.cs`
**When** SiteSettings is saved via Piranha Manager's built-in UI — which always constructs and saves a `DynamicSiteContent` (confirmed by decompiling `Piranha.Manager.Services.SiteService.SaveContent()`: it always calls `_api.Sites.SaveContentAsync(model.Id, dynamicSiteContent)` with a `DynamicSiteContent` instance, never the app's own `SiteSettings` POCO)
**Then** the hook still runs its Zalo/Maps/GA4/SearchConsoleVerification checks, reading field values from `DynamicSiteContent.Regions` (an `IDictionary<string, object>`) — not only from the current `model is not SiteSettings settings` path, which always short-circuits for real Manager saves.

**Given** a value that fails validation is submitted through Manager (e.g. `javascript:alert(1)` in Zalo URL, `data:text/html,<script>...` in Maps URL)
**When** save is attempted
**Then** it is rejected with the same error behavior as today's `SiteSettings`-typed path, and no partial/poisoned save occurs.

**Given** the existing `SiteSettingsTests` suite (11 tests, currently all passing but only exercising the strongly-typed direct-save path)
**When** this fix lands
**Then** equivalent test coverage is added that exercises the actual Manager `SaveContent`/`DynamicSiteContent` path directly, so a future refactor can't silently reintroduce this bypass.

**Given** the shared dev MariaDB currently holds ~105 GUID-suffixed test-debris `Page` rows (e.g. `contact-test-a-...`, `maps-test-...`) mixed into Manager's real page list, left behind because integration tests (`SiteSettingsTests`, `PerPageSeoFieldsTests`, etc.) create real pages via `CreatePublishedPageAsync` but only ever restore the `SiteSettings`/field values they mutated, never delete the pages they create
**When** this story is implemented
**Then** the existing test-debris rows are deleted from the shared dev database, and the test suite itself is fixed (each test deletes the page(s) it created in a `finally` block, or the suite points at an isolated/throwaway database instead of the shared dev one) so future `dotnet test` runs stop adding new debris.

> **Root-cause note (2026-09-23, correct-course):** Found live during this session — Phước reported still saving `javascript:...`/script values into Zalo/Maps URL via Manager despite Story 1.3/1.6's hook existing in code. "Stale process" was ruled out (restarted the app fresh from current source, still reproduced) and "poisoned legacy data" was ruled out separately (two invalid placeholder GA4 values — `"xxxxxxxxxx"`/`"yyyyyyyyyyy"` — found and cleared from the dev DB, unrelated to this bug). Decompiling `Piranha.Manager.dll` (via `ilspycmd`) confirmed the true cause: Manager always saves a `DynamicSiteContent`, and the hook's type-check (`model is not SiteSettings settings`) always returns early for that type, so validation never actually runs for real Manager-driven saves — only for the app's own direct-save code path, which is exactly what `SiteSettingsTests` exercises (hence all 11 tests pass while the live bug persists). **This is a live, currently-exploitable gap in already-`done` Story 1.3/1.6 work — recommend picking this up ahead of Stories 1.7/1.8 despite the story number.**

### Story 1.10: GA4 script an toàn — chặn theo môi trường & cookie-consent gate trước go-live

As the business owner,
I want the GA4 tracking script to never fire real events outside production, and to ask visitor consent before firing at all,
So that dev/staging testing never pollutes real analytics data, and the sites handle visitor tracking consent responsibly before launch.

**Acceptance Criteria:**

**Given** `_Analytics.cshtml` currently renders the gtag.js snippet identically regardless of `ASPNETCORE_ENVIRONMENT`
**When** the environment is anything other than Production (e.g. Development, Staging)
**Then** the GA4 script does not render/fire at all, even if a real GA4 Measurement ID has been entered in that environment's `SiteSettings` — env-gated in code, not left to an operational rule someone has to remember.

**Given** a visitor loads any page on either site in Production
**When** the page renders, before any analytics event fires
**Then** a cookie-consent gate is presented, and GA4's gtag.js only fires after the visitor consents — no pageview or event is sent pre-consent.

**Given** a visitor declines or has not yet responded to the consent gate
**When** they browse the site
**Then** no GA4 request is ever sent for that session until/unless they later consent.

> **Decision note (2026-09-23, correct-course):** Both items were open product decisions in `deferred-work.md` from Story 1.6's code review — Phước decided both: env-gate in code (not just a runbook note), and yes to a consent gate before go-live (not a hard legal requirement for these Vietnamese SMB sites, but chosen as the safer default).

### Story 1.11: Smoke-test script — MariaDB restart/recovery

As a developer maintaining this platform,
I want an automated (if non-xUnit) check that the app survives a MariaDB restart without creating duplicate Site rows,
So that the live-recovery behavior verified once by hand during Story 1.1 stays verified going forward, not just trusted from memory.

**Acceptance Criteria:**

**Given** the running app and its `mariadb` Docker Compose service
**When** the `mariadb` container is stopped and restarted (simulating a live outage/recovery)
**Then** a script (`.ps1`/`.sh`, run outside normal `dotnet test` cadence — not inside the shared-DB xUnit suite) confirms the app reconnects and continues serving without manual intervention.

**Given** the app reconnects after the database comes back
**When** the script inspects the `Piranha_Sites` table
**Then** it confirms exactly the expected `Site` rows exist — no duplicates were created during the outage/recovery cycle.

> **Context (2026-09-23, correct-course):** `DockerComposeConfigTests` already statically guards the compose YAML (image pin, `depends_on: service_healthy`, `restart: unless-stopped`, volumes) but never exercises the live retry/recovery path — that was manually verified once during Story 1.1's implementation but never automated, since tearing down the shared dev MariaDB mid-suite would break other integration tests sharing it in the same run. A separate script sidesteps that conflict.

### Story 1.12: Xác nhận & sửa hiệu ứng nháy màn hình rỗng ở Leads Manager

As a sales/ops user opening the Leads screen in Piranha Manager,
I want the list to never flash an incorrect "no leads yet" message before real data loads,
So that I don't briefly think there are no leads when there actually are.

**Acceptance Criteria:**

**Given** the Leads Manager screen (`Leads.cshtml`/`manager-leads.js`) on first load, before `/manager/api/lead/list` resolves
**When** a developer manually verifies (click-through in Manager, ideally throttled to a slow connection to make any flash visible)
**Then** it is confirmed whether the empty-state message ("Chưa có khách để lại thông tin nào.") ever renders before the fetch resolves.

**Given** the flash is confirmed real
**When** fixed
**Then** `Leads.cshtml`'s `v-if="items.length !== 0"` gets a proper loading-state check (e.g. don't show the empty-state branch until `loading` is false AND the first fetch has completed), so the empty state only ever renders when there are genuinely zero leads.

**Given** the flash is confirmed NOT to happen (e.g. Piranha's own `.app:not(.ready)` CSS convention already hides content until ready)
**When** this story is picked up
**Then** the story is closed with that finding recorded — no code change needed, just confirmation.

> **Context (2026-09-23, correct-course):** Found during Story 1.5's code review; couldn't be confirmed either way at the time since Piranha Manager's compiled CSS wasn't source-browsable in that session. Note: `ilspycmd` (a decompiler) is now known to be available in this dev environment (used to root-cause Story 1.9) — could also be used to inspect Piranha's compiled Manager CSS/JS for the `.app:not(.ready)` rule instead of relying solely on a manual click-through.

## Epic 2: Site B — Danh mục sản phẩm & Câu chuyện nghệ nhân

Khách xem trống (hub + 5 phân trang), thùng rượu gỗ, và bồn tắm gỗ theo từng trang SEO riêng, xem trang chi tiết từng model/biến thể, đọc trang câu chuyện nghệ nhân Phạm Trí Trong, và gửi yêu cầu báo giá qua form nhúng. **FRs covered:** FR-9, FR-10.

### Story 2.1: Khung trang & thanh liên hệ cố định Site B (Mộc Trầm)

As a visitor to trongdoitam.net,
I want the site to consistently present the warm-wood Mộc Trầm brand with a clear nav and an always-visible contact bar,
So that I recognize a trustworthy craft-heritage brand and can reach a human at any time.

**Acceptance Criteria:**

**Given** the Mộc Trầm design tokens (Be Vietnam Pro typeface, warm-wood color palette, 4px spacing scale, radius scale)
**When** any organic Site B page renders
**Then** it uses these tokens consistently — no other typeface or unrelated color appears.

**Given** the `nav` component
**When** rendered on desktop/tablet
**Then** it shows a fixed 56px bar, flat link list, a Trống submenu covering all 5 subpages, and the hotline number
**And** on mobile, the hotline is omitted from `nav` and the hamburger opens a full-height, single-level sheet
**And** no account/login icon appears on any breakpoint or page.

**Given** the `sticky-contact-bar`
**When** any organic page (not the landing-page-cta template) is viewed at any breakpoint
**Then** it stays fixed to the viewport bottom, never auto-hides on scroll, and shows 3 segments (Gọi ngay / Chat Zalo / Bản đồ) with the Zalo segment visually distinct (primary fill)
**And** it is reachable by keyboard/assistive tech without being trapped ahead of primary content.

**Given** the site as a whole
**When** any page or component is built
**Then** none of the following ever ship: cart/checkout flows, account/login UI on any breakpoint, auto-advancing/auto-rotating hero carousels, infinite scroll on category grids (use pagination/"load more" instead), modal-on-modal stacking, or vanity social-proof counters (like/view counts) on blog posts.

**Given** Site A's separate "Xanh Lục Bảo Rạng Rỡ" system exists
**When** Site B is styled
**Then** it never borrows Site A's palette, typeface, or component visual language, and vice versa — a visitor must never mistake one site for the other.

### Story 2.2: Trống — hub & 5 trang phân loại (FR-9)

As a visitor researching a ceremonial or festival drum,
I want to browse a Trống hub page linking to 5 dedicated subcategory pages (Trường học, Lân, Đội, Lễ hội, Chùa),
So that I can find the right drum type quickly, whether I arrive via search or via browsing.

**Acceptance Criteria:**

**Given** the Trống hub is a standalone Page and each of the 5 subcategories is an Archive page
**When** a visitor opens the hub
**Then** it links out to all 5 subcategory pages, each targeting its own keyword set (per PRD SM-1: trống lễ hội, trống chùa, trống trường học at minimum).

**Given** a Trống subcategory Archive page with published Product Posts (drum models)
**When** a visitor opens it
**Then** it renders a `product-card` grid (CMS-count-flexible, 2-up mobile widening on larger viewports) with a category eyebrow, title, one-line description, and a price row (real price or muted "Liên hệ báo giá").

**Given** a Trống subcategory with 0 published products
**When** a visitor would reach that category from the hub or homepage
**Then** its tile is hidden from the grid entirely, not rendered as an empty page.

**Given** a `product-card`
**When** a visitor taps anywhere on the card body, or taps its CTA button
**Then** both are equally valid, separate tap targets that navigate to the same product detail page.

### Story 2.3: Trang danh mục Thùng rượu gỗ & Bồn tắm gỗ (FR-9)

As a visitor,
I want dedicated category pages for thùng rượu gỗ trang trí and bồn tắm gỗ,
So that I can browse these two product lines the same way I browse drums.

**Acceptance Criteria:**

**Given** Thùng rượu gỗ is modeled as an Archive page with Product Post children
**When** a visitor opens it
**Then** it shows a `product-card` grid of its variants, all contact-for-quote (muted "Liên hệ báo giá") — this organic page never shows the real per-item pricing reserved for the paid landing page (Epic 3).

**Given** Bồn tắm gỗ has no real content, pricing, or photos yet
**When** the category page is published
**Then** it renders a placeholder-safe state — either a clearly labeled "coming soon" content block, or the page stays unpublished — and never shows a broken-image icon or literal placeholder/lorem-ipsum text.

**Given** either category page
**When** it has 0 published products
**Then** it follows the same empty-category hide rule as Trống (Story 2.2).

### Story 2.4: Trang chi tiết sản phẩm (product-detail-page) cho từng model (FR-9)

As a visitor,
I want to open one specific drum/wine-barrel/bathtub model's own detail page with its own photos, specs, SKU, and price-or-contact CTA,
So that I can evaluate one specific product instead of stopping at a generic category listing.

**Acceptance Criteria:**

**Given** a `product-detail-page` for a specific model
**When** it renders
**Then** its photo gallery leads the page and degrades to a single photo with no empty gallery-arrow chrome or placeholder tiles when only one image exists.

**Given** the same page
**When** the spec section renders
**Then** it reuses the reference-table row treatment filtered to just this one model (not a duplicated full table), and shows a small `sku-code` near the title (e.g. "Mã: TC-L2-160").

**Given** the price row
**When** the product has a genuine price set in the CMS
**Then** it shows the real price; otherwise it shows the muted "Liên hệ báo giá" text — drum, bathtub, and organic wine-barrel-variant pages all stay contact-for-quote (only the Epic 3 landing page shows real per-variant pricing).

**Given** the end of the page
**When** a visitor reaches it
**Then** an embedded `quote-request-form` (from Epic 1, Story 1.4) is present, with `product_of_interest` prefilled to this model's name.

**Given** all product photography on the page
**When** it renders
**Then** every image carries descriptive alt text — this matters more than usual here since the page is photo-led with comparatively little body text.

### Story 2.5: Trang câu chuyện nghệ nhân & trust-block (FR-10)

As a buyer who cannot visit the Đọi Tam workshop in person,
I want to read/see the craftsman's story (Phạm Trí Trong) with photos, and video if available,
So that I trust the workshop's quality without needing an in-person visit.

**Acceptance Criteria:**

**Given** the craftsman/heritage story page
**When** it is published
**Then** it includes a photo carousel/grid (required at launch) and is linked from all three Site B product-line pages (Trống hub, Thùng rượu gỗ, Bồn tắm gỗ) — not buried inside a generic About page.

**Given** a video asset does or does not exist for the story page
**When** the page renders
**Then** the video slot appears only if an asset exists (with captions/subtitles), and its absence leaves no empty player, broken embed, or "video coming soon" placeholder.

**Given** the `trust-block` short testimonial component (attributed to Phạm Trí Trong)
**When** it is embedded on a Trống subcategory page or any `product-detail-page`
**Then** it renders inline at the point a buyer is deciding, supplementing (never replacing) the dedicated story page.

## Epic 3: Site B — Landing page trả phí Thùng rượu gỗ (deadline Tết 2027)

Khách từ quảng cáo trả phí vào thẳng landing page riêng (không nav/sticky-bar), thấy giá thật cho từng biến thể, và bấm "Đặt mua ngay" để gửi yêu cầu. **FRs covered:** FR-12, FR-13.

### Story 3.1: Landing Page Type & template trả phí (FR-12)

As a content editor,
I want to create a landing page that is distinct from organic pages, excluded from primary navigation, and reusable for future paid campaigns,
So that paid ad traffic lands on a single-purpose conversion page instead of a general catalog page.

**Acceptance Criteria:**

**Given** a Landing Page Type (standalone Page, per AD-2/AD-5) with its own repeatable variant+price region
**When** an editor creates a new landing page instance for the thùng rượu gỗ line
**Then** it is excluded from Site B's primary navigation while still resolving as a fully functional standalone URL for direct/ad traffic.

**Given** the landing-page-cta template
**When** it renders
**Then** it drops the standard `nav` and 3-segment `sticky-contact-bar`, replacing them with the single full-width CTA pattern — no other page type is modeled as a Landing Page merely to display a price.

**Given** the landing page template exists
**When** a future campaign (beyond the wine-barrel launch) needs one
**Then** a new instance can be created without a code change.

### Story 3.2: Giá theo biến thể & CTA "Đặt mua ngay" (FR-13)

As a visitor who clicked a paid ad for the wine-barrel line,
I want to see each variant's real price and tap "Đặt mua ngay" to act immediately,
So that I can convert without hitting a phone-only wall.

**Acceptance Criteria:**

**Given** the wine-barrel landing page's variant+price region
**When** it renders
**Then** each of the 4 confirmed variants (gỗ sồi, ngựa kéo, 1 ngựa, 2 ngựa) shows its own real, visible price next to a variant-label chip.

**Given** a visitor taps "Đặt mua ngay" on a variant
**When** the CTA fires
**Then** it opens the inline `quote-request-form` on the same page (or offers Zalo/phone) — never a cart or an online payment step.

**Given** a successful submission from the landing page
**When** it completes
**Then** the same inline-success pattern as Epic 1's general form applies — no redirect away from the page the visitor trusted enough to act on.

**Given** the landing page has dropped `nav`/`sticky-contact-bar`
**When** it still renders
**Then** it inherits `gutter`, the card-grid conventions, and the full Mộc Trầm color/type system, so a visitor recognizes the same brand despite the different page shell.

## Epic 4: Site B — Bảng tham khảo cấu hình trống (should-have)

Khách xem bảng tham khảo kích thước/loại/bánh xe/sơn/vẽ mặt trống trước khi gửi yêu cầu — không tính giá tự động. **FRs covered:** FR-11.

### Story 4.1: Bảng tham khảo cấu hình trống (FR-11)

As a visitor researching a ceremonial drum,
I want to see a reference table of size, loại 1/2/3, bánh xe, sơn, and vẽ mặt trống options on the relevant Trống subpages,
So that I can narrow down what I want before contacting sales, without expecting an automatic price.

**Acceptance Criteria:**

**Given** the `drum-config-reference` component
**When** it is added to a Trống subpage
**Then** it renders as a read-only table (size × loại × bánh xe × sơn × vẽ mặt trống) with a visible disclaimer line ("Bảng tham khảo, không tính giá tự động")
**And** it ends in the same `quote-request-form`/Zalo/phone CTA pattern used elsewhere — it narrows the conversation, it never computes or displays a price itself.

**Given** this story has not yet been completed or is deprioritized against the Tết 2027 landing-page deadline (Epic 3)
**When** a Trống subpage built in Epic 2 renders without this component
**Then** the subpage still functions completely — it simply omits the reference table and routes straight to the existing contact channels, exactly as it does today by phone. This confirms Epic 2 does not depend on this epic to be complete.

## Epic 5: Site B — Blog/Nội dung

Khách đọc bài viết về nghề thủ công, cách bảo quản đồ gỗ, và hướng dẫn quà Tết, độc lập với danh mục sản phẩm. **FRs covered:** FR-14.

### Story 5.1: Trang danh sách Blog/Nội dung (FR-14)

As a visitor,
I want to browse a list of craft/heritage articles, wood-care advice, and Tết gifting guides,
So that I can learn more about the brand and products beyond the catalog.

**Acceptance Criteria:**

**Given** the Blog is modeled as an Archive page with BlogPost children (same Archive+Post mechanism as the product catalog, per AD-2)
**When** a visitor opens the Blog listing from Site B navigation
**Then** published posts are listed with native pagination, each with its own per-page SEO fields (Story 1.2), indexable independent of product pages.

**Given** no posts are published yet
**When** a visitor opens the Blog listing
**Then** it shows a plain "Bài viết đang được cập nhật" message rather than a bare empty list
**And** the Blog nav link remains visible regardless.

### Story 5.2: Trang chi tiết bài viết (article-detail-page) (FR-14)

As a visitor,
I want to open one article at its own URL with the full text and related posts,
So that I can read the whole piece and discover more content afterward.

**Acceptance Criteria:**

**Given** an `article-detail-page` for a published BlogPost
**When** it renders
**Then** it shows the title, optional byline/date metadata, and full body copy with in-article subheads where the article is long enough to need them.

**Given** related posts exist for that article
**When** the page renders its end
**Then** a related-posts module (reusing the `product-card` grid convention at smaller scale) appears.

**Given** no related posts exist
**When** the page renders its end
**Then** the related-posts module is omitted entirely and a plain "← Quay lại Blog" back-link appears instead — the page never dead-ends.

## Epic 6: Site A — Danh mục sản phẩm & Luồng sản phẩm (cần thi công / ship thẳng)

Khách duyệt 11 danh mục, vào trang chi tiết sản phẩm ở một trong hai biến thể, và với sản phẩm cần thi công thì gửi được form "Đăng ký khảo sát miễn phí" ngay trên trang. **FRs covered:** FR-6, FR-8.

### Story 6.1: Khung trang Site A (nav, hero, footer) — Xanh Lục Bảo Rạng Rỡ

As a visitor to tbtruonghoc.com,
I want the site to consistently present a confident, professional teal/amber institutional brand with clear navigation,
So that I trust this supplier with my school's procurement budget.

**Acceptance Criteria:**

**Given** the Site A design tokens (Mulish typeface, teal+amber palette, spacing/radius scales)
**When** any Site A page renders
**Then** it uses these tokens consistently, visually distinct from Site B's Mộc Trầm system — no shared palette or typeface between the two sites.

**Given** the `nav` component
**When** rendered on any page
**Then** it shows a solid banner bar, wordmark, a flat link list, and a "Sản phẩm" dropdown listing all 11 categories (never a mega-menu), ending in "Xem tất cả 11 nhóm sản phẩm →"
**And** a floating `contact-chip` (hotline + Zalo) stays visible at the nav's right edge on every page, on every scroll position
**And** the dropdown is fully keyboard-operable (open on focus/Enter, arrow-key/Tab navigation, close on Escape).

**Given** the homepage `hero-carousel`/`hero-fallback`
**When** product/installation photography is available
**Then** the carousel renders with a gradient overlay; when it is not available, or the viewport is under 768px, the solid-teal `hero-fallback` renders instead with the same heading/CTA content — never a broken or empty hero.

**Given** any page
**When** it renders on mobile
**Then** the nav collapses to a hamburger opening a side-sliding menu, with "Sản phẩm" expanding as an inline accordion of all 11 categories, and multi-column grids stack to one column.

**Given** every page
**When** it renders
**Then** a `footer-strip` (solid-teal bar, bold white brand name, Zalo/phone/email contact line) appears at the bottom.

**Given** any action element sitewide
**When** it is styled
**Then** primary (solid amber) is always the higher-intent action in context (Đăng ký khảo sát miễn phí, Yêu cầu báo giá, Gửi đăng ký) and secondary (teal outline) is always the phone/Zalo fallback (Gọi ngay · Zalo tư vấn) — this pairing is never inverted.

**Given** the site as a whole
**When** any page or component is built
**Then** no shopping cart or checkout/payment flow ships anywhere on Site A, matching the project-wide non-goal.

### Story 6.2: Trang chủ & trang tổng hợp danh mục (FR-6)

As a visitor,
I want a homepage showing featured categories and a full "Danh mục sản phẩm" page listing all 11 (and future) categories with search/filter,
So that I can browse the full catalog even as it grows past what a single glance can scan.

**Acceptance Criteria:**

**Given** the homepage
**When** it renders below the hero
**Then** it shows a curated 6-of-11 `category-tile` grid plus a "Xem tất cả" link, followed by the `trust-stat` band (500+ trường, 20 năm kinh nghiệm, etc.) placed below the category grid, not above the fold.

**Given** the aggregate "Danh mục sản phẩm" page
**When** it renders
**Then** it queries the site's Archive pages dynamically at render time to show the full, current category count plus a visibly distinct placeholder tile — never a hand-curated or hardcoded list, so a 12th category needs no code change.

**Given** the aggregate page's `category-search`
**When** a visitor types a search term or toggles a filter chip
**Then** the category grid filters live, client-side; a category may appear under more than one filter chip.

**Given** a category tile (homepage or aggregate page)
**When** it has a real, verifiable certification on file (e.g. ASTM, CARB P2)
**Then** it may show a certification badge — never a fabricated or placeholder certification.

### Story 6.3: 11 trang danh mục sản phẩm (FR-6)

As a school/preschool procurement contact,
I want each of the 11 school-equipment categories to have its own dedicated, SEO-targeted page,
So that I can find the specific type of equipment I need via search or browsing.

**Acceptance Criteria:**

**Given** the 11 confirmed Site A categories (dù che nắng sân trường, nội thất mầm non, quần áo nghi thức & cờ đội, thiết bị âm thanh–máy chiếu, bảng tương tác, màn hình LED hiển thị, thiết bị văn phòng, phòng thí nghiệm Lý–Hóa–Sinh, bàn thí nghiệm, thiết bị–đồ dùng dạy học, thiết bị mầm non ngoài trời)
**When** each is built as its own Archive page
**Then** each resolves to a unique URL with its own title/meta/content (Story 1.2), independently editable and independently rankable.

**Given** a category page
**When** it renders
**Then** a `breadcrumb` (Trang chủ / Sản phẩm / [danh mục]) appears directly below `nav`.

**Given** a category with 0 published products
**When** it would appear in the nav dropdown, homepage grid, or aggregate page
**Then** it is hidden from that grid entirely rather than shown as an empty page.

### Story 6.4: Trang chi tiết sản phẩm ship thẳng (FR-6)

As a procurement contact ordering a standalone shippable product (e.g. a dù tròn umbrella),
I want to see specs, a real price, and a clear shipping note,
So that I can decide and request a quote without needing a site survey.

**Acceptance Criteria:**

**Given** a shippable product's detail page
**When** it renders
**Then** it shows no `service-area` or `process-strip` blocks, and instead shows a `shipping-note` chip ("Giao hàng toàn quốc — 3–7 ngày làm việc tùy khu vực").

**Given** the `price-block`
**When** the product has a real price set in the CMS
**Then** it shows the real price in `price` typography with a bulk-order note; when no price is set, it shows the "Liên hệ để nhận báo giá" fallback in the identical block position/styling.

**Given** the primary CTA
**When** a visitor taps "Yêu cầu báo giá"
**Then** it routes to the Epic 1 general contact form (Story 1.4) with `product_of_interest` prefilled — never promising an installation visit the shippable flow doesn't include.

**Given** a `trust-badge` row (e.g. "Bảo hành chính hãng," "Đội thi công riêng") on the page
**When** it renders
**Then** it is non-interactive/informational only.

**Given** a gallery image that is genuine project/installation photography
**When** it is marked as such
**Then** it may show the `photo-badge` ("Hình ảnh thi công thực tế") — never applied to stock, placeholder, or AI-generated imagery.

**Given** all product photography on the page
**When** it renders
**Then** every image carries descriptive alt text.

### Story 6.5: Trang chi tiết sản phẩm cần thi công & form Đăng ký khảo sát miễn phí (FR-6, FR-8)

As a preschool director (Cô Lan) whose sunshade canopy needs on-site installation,
I want to see the service area and install process on the product page, then request a free survey without leaving the page,
So that I know upfront whether the company serves my area and can request a technician visit either way.

**Acceptance Criteria:**

**Given** an installation-required product's detail page
**When** it renders
**Then** it shows the `service-area` block ("Khu vực phục vụ: Miền Bắc – Thanh Hóa") and the 3-step `process-strip` (Khảo sát → Hợp đồng → Thi công), and its `price-block` shows the "Liên hệ để nhận báo giá" fallback by default (an exact price remains possible if the CMS record has one).

**Given** the primary CTA "Đăng ký khảo sát miễn phí"
**When** a visitor taps it
**Then** the `survey-form-modal` opens as an in-page overlay (not a page navigation), with `sản phẩm quan tâm` prefilled/read-only from page context, and required fields địa điểm/địa chỉ, họ tên, số điện thoại.

**Given** the `FormSubmission` table from Epic 1
**When** the survey form is submitted
**Then** a row is created with `form_type = 'survey'`, including `location_address` and, when applicable, `is_outside_service_area = true` — every submission includes enough for a technician to schedule a survey from the fields alone.

**Given** a visitor enters a địa điểm outside Miền Bắc–Thanh Hóa
**When** they attempt to submit
**Then** a non-blocking amber `warning-banner` appears (icon, headline, body text — never color alone) and the field border tints amber, but the submit button stays enabled and the submission still succeeds — the form never hard-blocks based on location.

**Given** the `survey-form-modal`'s fields
**When** it renders
**Then** each field carries a visible label (not placeholder-only) plus a visible `error`-colored required-marker on required fields, with `aria-required` (or equivalent) for screen readers; and the same `trust-badge`/`photo-badge` rules from Story 6.4 apply to this PDP's gallery and mini-trust row.

## Epic 7: Site A — Blog/Tin tức

Khách đọc bài viết tư vấn dài phục vụ SEO dài hạn, tách khỏi danh mục sản phẩm. **FRs covered:** FR-7.

### Story 7.1: Trang danh sách Blog/Tin tức (FR-7)

As a visitor,
I want to browse a list of advice-style articles ("nên chọn loại nào," "cách chọn..."),
So that I can research before buying, independent of the product catalog.

**Acceptance Criteria:**

**Given** the Site A Blog is modeled as an Archive page with BlogPost children (same mechanism as Story 5.1, applied to Site A)
**When** a visitor opens it from Site A navigation
**Then** published posts are listed with native pagination and their own per-page SEO fields, discoverable independent of product pages.

**Given** no posts are published yet
**When** a visitor opens the listing
**Then** it degrades gracefully (a plain "đang được cập nhật" message) rather than showing a bare empty list.

### Story 7.2: Trang chi tiết bài viết Site A (FR-7)

As a visitor,
I want to open one blog article at its own URL with the full text,
So that I can read the complete advice piece a search result pointed me to.

**Acceptance Criteria:**

**Given** a published Site A blog post
**When** a visitor opens it
**Then** it renders at its own indexable URL with title, body copy, and in-article subheads where needed.

**Given** the end of the article
**When** the page renders
**Then** it offers a related-posts module when related posts exist, or a back-to-listing link when none do — the page never dead-ends, matching Site B's equivalent pattern (Story 5.2).

## Epic 8: Sẵn sàng ra mắt — Migration, Redirect & Kết nối liên site

Toàn bộ URL cũ được chuyển hướng đúng, nội dung site cũ được migrate, ít nhất một liên kết biên tập cross-site tồn tại, và các baseline vận hành trước launch (Core Web Vitals, backup) được thiết lập. **FRs covered:** FR-5.

### Story 8.1: Chuyển hướng URL cũ qua Piranha Alias (AD-4)

As an SEO stakeholder,
I want every legacy tbtruonghoc.com URL to redirect correctly — same-domain to its new Site A location, and legacy trống URLs cross-domain to trongdoitam.net,
So that existing search rankings and inbound links survive the rebuild instead of breaking into 404s.

**Acceptance Criteria:**

**Given** Piranha's native `IApi.Aliases`/`AliasRouter`
**When** a redirect is created for a legacy URL
**Then** it is scoped (`SiteId`) to Site A regardless of whether the destination is same-domain (Site A restructuring) or cross-domain (legacy trống URL → trongdoitam.net) — Site A is where the OLD URL lived in both cases.

**Given** the legacy nav/sub-category audit in `addendum.md` (e.g. `/bo-dong-phuc-nghi-thuc-doi/`, `/cac-loai-co/`, legacy trống sub-pages)
**When** each legacy URL is requested post-launch
**Then** it 301/302-redirects to its correct new destination, verified against the audit list.

**Given** cross-domain aliases targeting Site B
**When** they are created
**Then** this happens only after Site B's final URL/slug structure (Epic 2, Epic 3) is frozen — never before, to avoid a live redirect pointing at a URL that later changes.

### Story 8.2: Migrate nội dung site cũ sang danh mục mới

As a content editor,
I want the legacy site's product/category content rewritten to SEO standard and placed into the new Site A category pages,
So that Site A launches with real content instead of empty categories.

**Acceptance Criteria:**

**Given** the legacy content audit in `addendum.md`
**When** the 11 Site A categories (Story 6.3) are populated
**Then** every category that existed on the legacy site has its content migrated and rewritten to current SEO standard (own title/meta/slug per Story 1.2).

**Given** the two proven organic-search pages ("bo-dong-phuc-nghi-thuc-doi", "cac-loai-co")
**When** their content is migrated into the "Quần áo nghi thức & cờ đội" category
**Then** the original intent-matching copy that was ranking despite thin SEO is preserved in the rewritten version, not replaced wholesale.

### Story 8.3: Liên kết biên tập giữa Site A và Site B (FR-5)

As a visitor reading about one Ngọc Anh business line,
I want to discover the other business line through a natural, contextual link,
So that I can find related products without seeing a jarring sitewide ad block.

**Acceptance Criteria:**

**Given** a Site A page whose content genuinely relates to Site B (e.g. a ceremonial-uniform/cờ page mentioning the drums used alongside it)
**When** an editor adds a link to the relevant Site B page
**Then** it is a plain rich-text hyperlink placed manually inside the content — no dedicated link-block component is built for this.

**Given** the craftsman-story page on Site B (which naturally mentions the company's school-equipment business)
**When** an editor adds a contextual link to Site A
**Then** the same manual, contextual-link rule applies — at least one such link exists in each direction before launch.

**Given** the full published site
**When** any template is audited
**Then** no automated, site-wide, identical cross-site link block exists on every page of either site.

### Story 8.4: Baseline Core Web Vitals & backup tự động trước launch (NFR-1, NFR-6)

As the business owner,
I want a captured performance baseline from the legacy site and daily automated database backups running before either site launches,
So that I can prove SEO growth doesn't come at the cost of site speed, and a data-loss event can't destroy both sites' content at once.

**Acceptance Criteria:**

**Given** the legacy tbtruonghoc.com site, still live
**When** its Core Web Vitals (LCP, CLS, INP) are measured before Site A/Site B launch
**Then** the baseline values are recorded as the counter-metric reference point (SM-C2) — this measurement happens before launch, not after.

**Given** the shared MariaDB database (Epic 1, Story 1.1)
**When** the automated daily `mariadb-dump` backup job is configured
**Then** it runs daily, retains 14 days of backups on the CentOS host, and has been verified at least once to produce a restorable dump before either site launches.
