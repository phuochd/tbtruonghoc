# Epic 1 Context: Nền tảng dùng chung — Multi-site, SEO, Liên hệ & Thu thập Lead

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

This epic establishes the shared platform foundation that both Site A (tbtruonghoc.com, school equipment) and Site B (trongdoitam.net, heritage woodcraft) build on top of for the rest of the project. It scaffolds one Piranha CMS instance hosting both sites, gives content editors self-service control over per-page SEO metadata, ensures each site shows its own correct contact channels, and stands up a single shared lead-capture pipeline (form submission → storage → sales-facing admin view) plus independent analytics/Search Console reporting per site. Every later epic's category, product, blog, and landing pages depend on the SEO-field capability, the contact-form/lead-storage mechanism, and the per-site settings model this epic delivers — it is the epic every other epic is built on.

## Stories

- Story 1.1: Khởi tạo Piranha multi-site scaffold cho Site A & Site B
- Story 1.2: Trường SEO trên từng trang (FR-1)
- Story 1.3: Thông tin liên hệ theo từng site (FR-2)
- Story 1.4: Form liên hệ/báo giá chung & lưu trữ lead (FR-3)
- Story 1.5: Quản lý lead trong Piranha Manager (AD-3)
- Story 1.6: Analytics & Search Console riêng theo từng site (FR-4)

## Requirements & Constraints

- Editors must be able to set title tag, meta description, and URL slug on every Page/Post, on either site, from Piranha Manager, with no developer involvement. Slugs must remain editable post-publish without breaking internal links (use Piranha's internal page-reference mechanism, never hardcoded URL strings).
- Every published page template on both sites must show click-to-call, a Zalo entry point, and Google Maps using the correct business line's details for that site — never hardcoded per view, never showing the other site's data.
- A lightweight contact/quote-request form must be embeddable on any product/category page on either site; every submission must be retrievable afterward with all fields intact. Submission must be client-side (fetch/AJAX) — never a full-page-reload form post.
- Every new form submission (any form_type) must trigger an automatic email notification.
- Sales/ops must be able to view, filter (by site), and see full detail of every submission from within Piranha Manager — no reliance on email or manual tracking.
- Each site must report to its own GA4 property and be independently verified in Search Console, with its own scoped `sitemap.xml`; changing a site's GA4 ID or verification value must take effect without a code deploy.
- No CRM/webhook/lead-routing integration in v1 — the Manager extension is the entire lead-management system for this phase.
- Local dev must run via Docker Compose with the MariaDB image pinned to exactly `10.11` (never `:latest`), matching production, so migration/SQL-dialect gaps can't hide locally.
- Form fields must be individually labeled (not placeholder-only); validation errors must be announced in text adjacent to the invalid field, never conveyed by color alone (accessibility floor: WCAG 2.1 AA).

## Technical Decisions

- **Single Piranha instance, native multi-site (AD-1):** Site A and Site B are two `Site` records inside one Piranha installation, one database, one deployment, one media library, one Manager login. Never split into separate deployments; never stand up a second Piranha instance "for Site B." `Site` records (hostnames, `IsDefault`) are created once, during this epic's scaffold story, and never re-derived later.
- **Stack:** .NET 8, Piranha CMS 12.2.0, Piranha.Templates `piranha.mvc` scaffold 12.0.0, Piranha.Data.EF.MySql 12.0.0 (Pomelo, `MariaDbServerVersion`), MariaDB 10.11 LTS, CentOS Stream 9 self-hosted (prod). `.NET 8` reaches End of Support 2026-11-10 — tracked as a near-term operational risk, not addressed in this epic.
- **Per-page SEO (FR-1):** handled by Piranha's core Page/Post meta fields — no custom architecture needed. This is a platform-level capability: any Page/Post type built in later epics (category, product, blog, landing page) automatically inherits editable Title/Meta Description/Slug rather than having it rebuilt per type.
- **Per-site settings singleton:** one `SiteSettings` SiteType, one instance per Piranha `Site`, holds both GA4 measurement ID/Search Console verification (FR-4) and phone/Zalo/Maps contact details (FR-2). Every view reads from it; neither is ever hardcoded per-view.
- **Lead capture — custom storage, not Piranha Comments (AD-3):** FR-3 (and later FR-8) submissions write to one shared custom EF Core `FormSubmission` table — never Piranha's built-in Comment/PostComment model. Fixed schema: `id, site_id, form_type, name, phone, product_of_interest, message, location_address (nullable), is_outside_service_area (bool, nullable), created_at`. A custom Piranha Manager extension module ("Danh sách khách để lại thông tin") lists/details every submission.
- **Media/dev parity:** local dev uses Docker Compose (`piranha-app` + `mariadb` services) with a volume-mounted media folder; the `mariadb` service image tag must stay pinned to `10.11`.

## UX & Interaction Patterns

- Contact form success: an inline confirmation message replaces the form in place — no redirect, no separate modal.
- Contact form failure: entered field values are retained (never cleared); an inline error message offers phone/Zalo as an immediate fallback channel.
- Click-to-call must be a real `tel:` link (not a JS-only handler with no fallback); the Zalo entry point must open a deep link with a working web fallback when the Zalo app isn't installed.

## Cross-Story Dependencies

- Story 1.1 (scaffold, Site records, shared database) is the prerequisite for every other story in this epic and every later epic — no later story creates its own Site/database setup.
- Story 1.2's SEO-field capability is consumed by every Page/Post type built in Epics 2–7 (category pages, product pages, blog posts, landing pages) — it is built once here, not per page type.
- Story 1.3 and Story 1.6 both extend the same `SiteSettings` SiteType (contact fields vs. GA4/Search Console fields) — treat it as one shared model, not two.
- Story 1.4's `FormSubmission` table and client-side submission pattern is reused directly by Epic 6's survey form (Story 6.5, `form_type = 'survey'`) and embedded on product pages built in Epic 2 (Story 2.4) and the Epic 3 landing page (Story 3.2).
- Story 1.5's Manager extension depends on Story 1.4's `FormSubmission` table existing first.
- Epic 8's daily database backup (NFR-6) depends on the shared database created in Story 1.1.
