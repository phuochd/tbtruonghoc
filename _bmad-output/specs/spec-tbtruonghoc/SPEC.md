---
id: SPEC-tbtruonghoc
companions:
  - ../../planning-artifacts/architecture/architecture-tbtruonghoc-2026-09-18/ARCHITECTURE-SPINE.md
  - ../../planning-artifacts/ux-designs/ux-tbtruonghoc-2026-09-18/DESIGN.md
  - ../../planning-artifacts/ux-designs/ux-tbtruonghoc-2026-09-18/EXPERIENCE.md
  - ../../planning-artifacts/ux-designs/ux-trongdoitam-2026-09-17/DESIGN.md
  - ../../planning-artifacts/ux-designs/ux-trongdoitam-2026-09-17/EXPERIENCE.md
  - ../../planning-artifacts/prds/prd-tbtruonghoc-2026-09-17/addendum.md
sources:
  - ../../planning-artifacts/briefs/brief-tbtruonghoc-2026-09-17/brief.md
  - ../../planning-artifacts/briefs/brief-tbtruonghoc-2026-09-17/addendum.md
  - ../../planning-artifacts/prds/prd-tbtruonghoc-2026-09-17/prd.md
---

> **Canonical contract.** This SPEC and the files in `companions:` are the complete, preservation-validated contract for what to build, test, and validate. Source documents listed in frontmatter are for traceability — consult them only if you need narrative rationale or prose color this contract intentionally omits.

# Ngọc Anh Multi-Site Rebuild — tbtruonghoc + trongdoitam.net

## Why

Thiết Bị Trường Học Ngọc Anh's institutional-equipment business is nearly invisible on Google — almost every sale today comes from cold calling or word-of-mouth, and the current .NET site can't even set a per-page title/meta/slug to start fixing that (a mandate the old platform structurally blocks). Splitting the business into two focused sites on Piranha CMS is both a pain fix (per-page SEO control, finally) and an opportunity to capture: **trongdoitam.net** lets the owner's father-in-law's real Đọi Tam craftsman heritage — currently buried inside a generic equipment catalog — carry its own trust story across drums plus two new product lines (thùng rượu gỗ, bồn tắm gỗ), while **tbtruonghoc** keeps serving schools and institutions as a practical, spec-driven catalog. A hard external mandate sits inside this vision: the thùng rượu gỗ line must be ready for paid-ads traffic before Tết Âm Lịch 2027 (~mid-February 2027).

## Capabilities

- **CAP-1**
  - **intent:** Content editors can set title tag, meta description, and URL slug on every page/post on both Piranha sites, without developer involvement.
  - **success:** Every published page on both sites has a distinct, editable title tag and meta description; slugs are human-readable and editable post-publish without breaking internal links.

- **CAP-2**
  - **intent:** Every page on both sites displays click-to-call, a Zalo chat entry point, and Google Maps, using that site's own contact details.
  - **success:** Click-to-call and Zalo entry points are present and functional on 100% of published page templates on both sites.

- **CAP-3**
  - **intent:** A lightweight contact/quote-request form can be embedded on any product or category page on either site.
  - **success:** Every submission is retrievable afterward with all submitted fields intact and is captured and routed per the FormSubmission mechanism (architecture-spine AD-3).

- **CAP-4**
  - **intent:** Each site reports to its own Google Analytics property and is separately verified/submitted in Search Console.
  - **success:** Analytics tracking fires on all page types on both sites, independently attributable per site; each site has its own submitted sitemap and visible indexing status from launch.

- **CAP-5**
  - **intent:** Site A and Site B link to each other only through contextual, editorial links embedded in relevant content — never a site-wide link block.
  - **success:** Cross-site links appear only where the connection is genuine (e.g., a ceremonial-uniform page mentioning drums used alongside it); no automated, site-wide, identical link block exists on either site.

- **CAP-6**
  - **intent:** Each of Site A's 11 confirmed categories (danh mục thiết bị trường học) gets its own SEO-optimized page with its own target keyword set.
  - **success:** Each category resolves to a unique URL with category-specific title/meta/content and ranks for at least one assigned target keyword within the SM-1 measurement window.

- **CAP-7**
  - **intent:** Site A has a blog/news section for longer-form, advice-style content ("nên chọn loại nào," "cách chọn..."), separate from the product catalog.
  - **success:** Blog posts are discoverable from Site A navigation and indexable independent of product pages.

- **CAP-8**
  - **intent:** A dedicated "Đăng ký khảo sát miễn phí" (free survey request) form exists on Site A's installation-required category/product pages, distinct from the general contact form.
  - **success:** Reachable from every installation-required category page via a CTA distinct from the generic contact CTA; every submission includes at minimum installation location/address, product of interest, and a callback contact (name + phone); a submission from outside Miền Bắc–Thanh Hóa shows a warning but is never rejected.

- **CAP-9**
  - **intent:** Each of Site B's 3 product lines — trống (all types unified under one brand story), thùng rượu gỗ trang trí, and bồn tắm gỗ — gets its own SEO-optimized page(s).
  - **success:** Each product line resolves to distinct URLs with line-specific title/meta/content; priority drum keywords (trống lễ hội, trống chùa, trống trường học) are tracked per SM-1.

- **CAP-10**
  - **intent:** A craftsman/heritage story page tells the Đọi Tam workshop and craftsman (Phạm Trí Trong) story, linked from all three Site B product-line pages — not buried in a generic "About" page.
  - **success:** Linked from all three Site B product-line pages; ships with both photo and video content — video is confirmed (not a should-have contingency).

- **CAP-11** *(should-have)*
  - **intent:** A drum configuration reference (size, loại 1/2/3, bánh xe, sơn, vẽ mặt trống) is available as a read-only reference table/lightweight selector, still ending in a quote request — not a checkout flow.
  - **success:** If built, the configuration options shown match what sales actually offers. Sequenced before the Tết 2027 wine-barrel landing page work (CAP-12/CAP-13) — a confirmed priority call.

- **CAP-12**
  - **intent:** Editors can create paid-ads landing pages on Site B, distinct from organic category/product pages and reusable for future campaigns beyond the wine-barrel launch.
  - **success:** Landing pages are excluded from primary Site B navigation but fully functional as standalone URLs for ad traffic; at least one landing page for the thùng rượu gỗ line exists and is live before Tết Âm Lịch 2027 (~mid-February 2027).

- **CAP-13**
  - **intent:** Landing pages display specific prices per item/variant, unlike Site B's organic product pages (drums, bathtubs remain contact-for-quote).
  - **success:** Each wine-barrel variant (gỗ sồi, ngựa kéo, 1 ngựa, 2 ngựa) shows a specific price on its landing page, paired with a clear "Đặt mua ngay" CTA that routes to a contact form/Zalo/phone — never to a cart or online payment.

- **CAP-14**
  - **intent:** Site B has a blog/content section covering craft/heritage topics, wood-care advice, and Tết gifting guides, following the same pattern as CAP-7.
  - **success:** Own per-page SEO fields, indexable, discoverable from Site B navigation, separate from the product catalog.

## Constraints

- One shared Piranha CMS instance and database serves both sites (Site C joins later as a third `Site` record) — never per-site deployments (architecture-spine AD-1).
- Product taxonomy is modeled as Archive+Post (danh mục/blog); standalone content (landing pages, craftsman story page, homepage, category hub) is a standalone Page — no ad-hoc content modeling (AD-2).
- Lead-form submissions (CAP-3, CAP-8) write to one shared custom `FormSubmission` table plus a Piranha Manager extension — never Piranha's built-in Comment feature (AD-3).
- All redirects, same-domain and cross-domain, route through Piranha's native Alias/AliasRouter — no custom middleware (AD-4); cross-domain aliases into Site B are created only after Site B's URL structure is frozen.
- Stack is forced to .NET 8 + Piranha CMS 12.0.0 + MariaDB 10.11 LTS, self-hosted on CentOS Stream 9 — Piranha does not yet cross-compile for .NET 10 (architecture-spine has the full stack table and the .NET 8 EOL 2026-11-10 risk).
- Site B is prioritized ahead of Site A; Site A proceeds in parallel where capacity allows but yields to Site B whenever the two compete for build time, because only Site B carries a fixed external deadline.
- Cross-site linking (CAP-5) must stay editorial/contextual only, never a sitewide or reciprocal link block — common ownership of both domains is WHOIS/hosting-discoverable, and heavy reciprocal linking between commonly-owned domains is a Google link-scheme/PBN risk pattern.
- Site A's installation-required products (e.g. dù che nắng) are serviceable only Miền Bắc through Thanh Hóa; shippable products on both sites serve nationwide.
- Both sites are Vietnamese-only, domestic-market only — no i18n/multi-language on either site.
- CAP-11 (drum configuration reference) is built before the Tết 2027 wine-barrel landing page work (CAP-12/CAP-13) begins — a confirmed sequencing decision that accepts the wine-barrel work may be pushed if the two compete for the same build time.
- Both sites must meet a WCAG 2.1 AA contrast/accessibility floor — a confirmed target, not merely assumed.

## Non-goals

- No shopping cart or online checkout/payment anywhere in v1, on either site — including the wine-barrel landing page, despite its visible pricing (CAP-13).
- Site C (trongngocanh.com) is not built in phase 1 — this spec only names its eventual role as the future parent-brand site.
- No automated/sitewide cross-domain link exchange — only editorial, contextual cross-linking (CAP-5).
- No paid-ad campaign management/operation — only the landing page capability and measurement infrastructure are in scope.
- No multi-language/internationalization on either site.
- No CRM or lead-routing system design beyond form capture (CAP-3/CAP-8) — downstream routing is an architecture-level decision, not specified further here.
- No content-editing training program for the owner, despite Piranha making self-editing possible.

## Success signal

Within 3 months of each site's own launch, that site's priority keywords reach Google page 2 or better, ideally page 1 (Site A: dù che nắng, dù che nắng sân trường; Site B: trống lễ hội, trống chùa, trống trường học — SM-1), and Site A's non-ceremonial categories begin generating Google-attributed inbound contact for the first time (SM-2). At least one Site B wine-barrel landing page is live and receiving paid-ad traffic before Tết Âm Lịch 2027 (SM-3). Two guardrails must hold throughout: phone/Zalo contact volume must not decrease as organic SEO investment grows (SM-C1), and Core Web Vitals must not regress versus a pre-launch legacy-site baseline (SM-C2, baseline capture is a pre-launch acceptance step).

## Assumptions

- Bồn tắm gỗ has no existing content, pricing, or product photos — a from-scratch product line. A competitor's page (trongdoitam.com.vn, a different Đọi Tam-village business — Doanh nghiệp Trống Thanh Hùng, not Phạm Trí Trong's workshop) confirms the category and naming pattern (gỗ sồi/gỗ đẹp/gỗ thông, contact-only pricing) but its content/photos are not usable as owned content (PRD Open Question 9, pending final confirmation).
- The exact SKU/spec list for Site A's newly-scoped-in categories is deferred to content-entry time — category-page structure (CAP-6) does not depend on the count or names of SKUs; content/sales supplies the list directly when CMS content is populated (PRD Open Question 1, resolved).
