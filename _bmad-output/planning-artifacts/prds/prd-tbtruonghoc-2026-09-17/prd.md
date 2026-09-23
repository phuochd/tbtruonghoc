---
title: "PRD: Ngọc Anh Multi-Site Rebuild — tbtruonghoc (school equipment) + trongdoitam.net (heritage woodcraft)"
status: final
created: 2026-09-17
updated: 2026-09-17
---

# PRD: Ngọc Anh Multi-Site Rebuild — tbtruonghoc + trongdoitam.net
*Working title — confirm.*

## 0. Document Purpose

This PRD defines a rebuild that, after discovery, turned out to be **two products, not one**: a school-equipment catalog site (tbtruonghoc, rebuilt on the current domain) and a separate heritage-woodcraft brand site (trongdoitam.net), built together in the same phase because they share a platform, a design process, and an owner, but serve different audiences under different brand stories. A third site, **trongngocanh.com** (general Ngọc Anh corporate/brand site linking to both), is confirmed but explicitly deferred past phase 1 — named here only so it isn't lost.

This PRD reflects the same 3-site architecture as the Product Brief (`_bmad-output/planning-artifacts/briefs/brief-tbtruonghoc-2026-09-17/brief.md`), which was formally updated via `bmad-product-brief`'s Update path to match once the site split was confirmed — the two documents are in sync (Open Question 8, resolved). Both sites are covered in one PRD, grouped by site, because most cross-cutting concerns (Piranha CMS choice, SEO discipline, phone-first UX, the Google Stitch design workflow) apply identically to both — splitting into two documents would duplicate that shared material for little benefit. Vietnamese product/category names and target keywords are kept verbatim (not translated) because they are the literal search terms being optimized for.

## 1. Vision

Ngọc Anh's institutional-equipment business and its heritage-woodcraft ambitions have been tangled into one undifferentiated site that ranks for almost nothing. Splitting them lets each site tell its own, sharper story to its own audience:

**tbtruonghoc** stays the workhorse: schools, preschools, playgrounds, and public venues finding and buying institutional equipment (sunshades, furniture, AV/lab gear, ceremonial-uniform sets) — practical, price- and spec-driven, phone/Zalo-first, now with per-page SEO control it never had on the old .NET stack.

**trongdoitam.net** carries the story the equipment catalog couldn't: an authentic craft heritage (the owner's father-in-law, master craftsman and Đọi Tam guild chairman, personally quality-controls every piece) applied across everything that workshop makes — drums, decorative wine barrels, and wooden bathtubs. This is where trust-and-craftsmanship buyers (temples, communal houses, Tết gift buyers) land, and where the time-critical Tết 2027 wine-barrel campaign lives.

The two sites cross-link each other editorially (not as a site-wide link block — see FR-5 and the SEO caution in `addendum.md`) so a visitor discovering one business line can find the other, without diluting either site's focus. **trongngocanh.com** eventually ties both together under the parent brand; not now.

## 2. Site Architecture & Product Taxonomy

| Site | Domain | Brand focus | Audience |
|---|---|---|---|
| **Site A — tbtruonghoc** | current domain, rebuilt | Institutional/school equipment | Schools, preschools, playgrounds/parks |
| **Site B — trongdoitam.net** | new | Đọi Tam heritage woodcraft | Temples/communal houses/family halls, Tết gift buyers |
| **Site C — trongngocanh.com** | existing, not phase 1 | Parent/corporate brand | Deferred — links to A and B once built |

**Site A category list** (thiết bị trường học): dù che nắng (sân trường), nội thất mầm non, quần áo nghi thức + cờ (nghi thức đội — the proven organic-search niche, stays here as school-ceremony gear, not craft product), thiết bị âm thanh/máy chiếu, bảng tương tác, màn hình Led hiển thị, thiết bị văn phòng, phòng thí nghiệm Lý-Hóa-Sinh, bàn thí nghiệm, thiết bị-đồ dùng dạy học, thiết bị mầm non ngoài trời.

**Site B category list** (đồ gỗ thủ công Đọi Tam): trống — **all types together** (trường học, lân, đội, lễ hội, chùa; deliberately not split across sites, to keep the workshop/craftsman story coherent under one brand rather than fragmenting it), thùng rượu gỗ trang trí (new, Tết 2027 deadline), bồn tắm gỗ (new).

**Site C** is out of phase-1 scope entirely (§5, §6).

## 3. Target User

### 3.1 Jobs To Be Done

*Site A*
- As a school/preschool procurement lead, I need to find and price equipment (sunshades, furniture, ceremonial uniforms, AV/lab gear) without relying on word-of-mouth, so I can compare options before calling.
- As an institution needing on-site installation (sunshade canopies), I need confidence the seller can survey, quote, and install — not just sell and disappear.

*Site B*
- As a temple/communal-house/family-hall caretaker buying a ceremonial drum, I need to trust the craftsmanship and specify exact configuration before ordering.
- As a Tết gift buyer, I need to quickly see what a decorative wine barrel looks like and costs, from a paid ad, without being routed into a phone-only sales process that kills ad conversion.
- As a buyer interested in a wooden bathtub, I need the same craftsmanship story and product clarity as the drum/wine-barrel lines — this is a new line with no existing content or proof point.

*Both sites*
- As the business owner, I need every page on both sites to keep working as a phone/Zalo lead generator even as organic search becomes a second channel.

### 3.2 Non-Users (v1)

- Individual consumers browsing casually with no institutional or gifting purchase intent.
- International or non-Vietnamese-speaking buyers — both sites are Vietnamese-only, domestic market only.

### 3.3 Key User Journeys

- **UJ-1 (Site A). Cô Lan sources a sunshade canopy that needs installation.**
  - **Persona + context:** Cô Lan, hiệu trưởng of a preschool, needs a dù mái installed over the schoolyard.
  - **Entry state:** Unauthenticated, arrives via Google search or word of mouth.
  - **Path:** Browses product images and specs on the sunshade category page → calls the hotline → sales rep consults by phone → requests an on-site survey via the **"Đăng ký khảo sát miễn phí"** form (FR-8).
  - **Climax:** Receives a formal quote matched to actual site conditions after the survey.
  - **Resolution:** Contract signed, installed by the company's own crew.
  - **Edge case:** Outside the installation service area (Miền Bắc through Thanh Hóa) — this should be clear before she invests time in a survey request.

- **UJ-2 (Site A). Thầy Nam orders a standalone dù tròn (shippable, no installation).**
  - **Persona + context:** Thầy Nam, a procurement contact at a khu vui chơi/công viên with a large open area, needs umbrellas that ship rather than requiring a survey/contract cycle — same buyer type as UJ-1 (installation-required vs. shippable is a product-line split, not a persona split), simpler purchase.
  - **Path:** Browses size/material options → contacts for price → agrees → ships to location. Realizes FR-6.
  - **Resolution:** Delivered; installation offered only as an optional paid add-on.

- **UJ-3 (Site B). Bác Thành commissions a ceremonial drum from the Đọi Tam workshop.**
  - **Persona + context:** Bác Thành, thủ từ of a village đình, cares about authenticity and craftsmanship more than price alone.
  - **Path:** Browses drum designs → contacts by phone/Zalo → (commonly) visits the workshop, meets the craftsman → specifies configuration (size, loại 1/2/3, bánh xe, sơn, vẽ mặt trống).
  - **Climax:** Quote matched to his exact configuration.
  - **Resolution:** Delivery date agreed and drum delivered.
  - **Edge case:** Buyers who cannot visit in person still need the trust signal a visit provides — realized via the heritage/craftsman story page (FR-10).

- **UJ-4 (Site B). Chị Hương buys a decorative wine barrel as a Tết gift via paid ads.**
  - **Persona + context:** Ad-driven intent, low patience for a phone-only funnel.
  - **Path:** Sees a paid ad → lands on a dedicated campaign landing page → sees clear per-item pricing (unlike drums/bathtubs, which stay contact-for-quote) → clicks "Đặt mua ngay."
  - **Climax:** CTA routes to a contact form/Zalo/phone to finalize — pricing transparency removes the main ad-funnel friction point.
  - **Resolution:** Order and payment finalized off-site, in time for Tết delivery.
  - **Edge case:** No on-page cart recovery mechanism exists in v1 (see §6 Non-Goals).

## 4. Glossary

- **Site A (tbtruonghoc)** — the rebuilt school/institutional-equipment site, current domain.
- **Site B (trongdoitam.net)** — the new heritage-woodcraft brand site: trống, thùng rượu gỗ, bồn tắm gỗ.
- **Site C (trongngocanh.com)** — the existing parent/corporate domain, deferred past phase 1.
- **Danh mục (product category)** — a top-level product grouping with its own SEO-optimized page and target keyword set; see §2 for the confirmed per-site lists.
- **Piranha CMS** — the platform both sites are built on, chosen for native per-page SEO fields (title, meta description, slug).
- **Google Stitch** — external AI UI-design tool; Claude drafts text prompts for Phước to run manually (process detail in `addendum.md`), for both sites' interfaces.
- **Sản phẩm cần thi công (installation-required product)** — Site A products (e.g., dù mái) whose flow includes an on-site survey, contract, and company-crew install, bounded to Miền Bắc through Thanh Hóa.
- **Sản phẩm ship được (shippable product)** — products sold/delivered without a mandatory survey step, nationwide.
- **Landing page (paid-ads landing page)** — a Site B page built as a paid-campaign destination, distinct from organic category/product pages, supporting visible pricing (FR-12, FR-13).
- **Nghệ nhân (master craftsman)** — the Đọi Tam workshop craftsman (owner's father-in-law, guild chairman), whose story anchors Site B (FR-10).
- **Editorial cross-linking** — manual, contextual links between Site A and Site B (FR-5), not a site-wide automated link block.

## 5. Features

### 5.1 Shared/Cross-Site Foundations

**Description:** Concerns that apply identically to both sites — the reason this stays one PRD rather than two.

**Functional Requirements:**

#### FR-1: Per-page SEO fields (both sites)
Content editors can set title tag, meta description, and URL slug on every page, on both Piranha instances, without developer involvement.

**Consequences (testable):**
- Every published page on both sites has a distinct, editable title tag and meta description.
- URL slugs are human-readable and editable post-publish without breaking internal links.

#### FR-2: Persistent contact channels (both sites)
Every page on both sites displays click-to-call, a Zalo chat entry point, and Google Maps, using the correct contact details/location for that site's business line.

**Consequences (testable):**
- Click-to-call and Zalo entry points are present and functional on 100% of published page templates on both sites.

#### FR-3: General contact/quote-request form (both sites)
A lightweight contact form on any product/category page on either site.

**Consequences (testable):**
- Every submission is retrievable after the fact with all submitted fields intact (capture itself is verifiable independent of where it routes).
- Form submissions are captured and routed (destination mechanism is an architecture-level decision).

#### FR-4: Google Analytics + Search Console integration (both sites)
Each site reports to its own Analytics property and is separately verified/submitted in Search Console.

**Consequences (testable):**
- Analytics tracking fires on all page types on both sites, independently attributable per site.
- Each site has its own submitted sitemap and visible indexing status from launch.

#### FR-5: Editorial cross-site backlinking
Site A and Site B (and Site C, later) link to each other through contextual, editorial links embedded in relevant content — not a site-wide footer/sidebar link block.

**Consequences (testable):**
- Cross-site links appear only in content contexts where the connection is genuine (e.g., a Site A ceremonial-uniform page mentioning the drums used alongside it, linking to Site B; a Site B craftsman-story page mentioning the company's school-equipment business, linking to Site A).
- No automated, site-wide, identical link block exists on every page of either site.

**Notes:** `[NOTE FOR PM: common ownership of both domains is discoverable via WHOIS/hosting; heavy reciprocal linking between commonly-owned sites is a known Google link-scheme/PBN risk pattern. Editorial, contextual linking (as specified) is the safer approach — see addendum.md for detail. Do not scale this up into a sitewide link exchange without revisiting this risk.]`

### 5.2 Site A: tbtruonghoc (thiết bị trường học)

**Description:** The institutional-equipment catalog — 11 confirmed categories (§2), each individually SEO-optimized, replicating the one proven organic win (quần áo nghi thức + cờ) across the rest of the catalog.

**Functional Requirements:**

#### FR-6: Site A dedicated category pages
Each of the 11 Site A categories gets its own page with its own target keyword set. `[ASSUMPTION: the exact SKU/spec list within the newly-scoped-in categories (Phòng thí nghiệm, Bảng tương tác, Màn hình Led, Thiết bị văn phòng, Bàn thí nghiệm, Thiết bị-đồ dùng dạy học, Thiết bị mầm non ngoài trời) is not yet enumerated — Open Question 1.]`

**Consequences (testable):**
- Each category resolves to a unique URL with category-specific title/meta/content.
- Each category page ranks for at least one assigned target keyword within the SM-1 measurement window.

#### FR-7: Site A blog/news section
Longer-form, advice-style content ("nên chọn loại nào," "cách chọn...") for durable long-tail SEO, separate from the product catalog.

**Consequences (testable):**
- Blog posts are discoverable from Site A navigation, indexable independent of product pages.

#### FR-8: "Đăng ký khảo sát miễn phí" (free survey request) form
Realizes UJ-1. A dedicated form for installation-required categories (dù che nắng), distinct from the general contact form (FR-3).

**Consequences (testable):**
- Reachable from every installation-required category page via a CTA distinct from the generic contact CTA.
- Every submission includes, at minimum: installation location/address, product of interest, and a callback contact (name + phone) — a technician can schedule a survey from these fields alone, with no follow-up call needed just to gather basics.
- A visitor entering a location outside Miền Bắc–Thanh Hóa sees a warning before submitting but can still submit; the form does not reject the submission.

**Notes:** Geographic service bound (Miền Bắc–Thanh Hóa) is visible near this CTA. **Resolved (Open Question 2):** the form warns visitors outside the service area but does not block submission — sales reviews and decides case-by-case rather than losing a potential lead outright.

### 5.3 Site B: trongdoitam.net (đồ gỗ thủ công Đọi Tam)

**Description:** The heritage-craft brand — drums (all types, unified), the new wine-barrel line (Tết 2027 deadline), and the new wooden-bathtub line, all sharing one craftsman/workshop story.

**Functional Requirements:**

#### FR-9: Site B dedicated category pages
Trống (all types combined under one coherent brand story), thùng rượu gỗ trang trí, and bồn tắm gỗ each get their own SEO-optimized page(s). `[ASSUMPTION: bồn tắm gỗ has no existing content, pricing, or product photos — this is a from-scratch line, unlike thùng rượu which at least has named variants. Flagged in Open Questions.]`

**Consequences (testable):**
- Each of the three product lines resolves to distinct URLs with line-specific title/meta/content.
- Priority drum keywords (dù che nắng is Site A; trống lễ hội, trống chùa, trống trường học apply here) are tracked per SM-1.

#### FR-10: Craftsman/heritage story page
Realizes UJ-3's trust-building step. Tells the Đọi Tam workshop/craftsman story (photos, video, workshop address), linked from drum, wine-barrel, and bathtub pages — not buried in a generic "About" page.

**Consequences (testable):**
- Linked from all three Site B product-line pages.
- Includes photo content at minimum; video is a should-have. `[ASSUMPTION: video asset availability/timeline unconfirmed — Open Question 3.]`

#### FR-11: Drum configuration reference (should-have)
Realizes UJ-3's configuration step (size, loại 1/2/3, bánh xe, sơn, vẽ mặt trống) as a reference table/lightweight selector, still ending in a quote request — not a checkout flow. Confirmed as valuable but not a hard requirement; fallback is configuring by phone/Zalo, as today.

**Consequences (testable):**
- If built, configuration options shown match what sales actually offers.

**Notes:** Priority relative to the Tết 2027 wine-barrel deadline is an open sequencing question — Open Question 4.

#### FR-12: Dedicated paid-ads landing page type
Realizes UJ-4. Editors can create landing pages distinct from organic Site B category/product pages, intended as paid-campaign destinations — built generally, reusable for future campaigns beyond the wine-barrel launch.

**Consequences (testable):**
- Landing pages are excluded from primary Site B navigation but fully functional as standalone URLs for ad traffic.
- At least one landing page exists for the thùng rượu gỗ line before Tết Âm Lịch 2027 (~mid-February 2027).

#### FR-13: Visible per-item pricing on landing pages
Landing pages display specific prices per item/variant, unlike Site B's organic product pages (drums, bathtubs remain contact-for-quote).

**Consequences (testable):**
- Each wine-barrel variant (gỗ sồi, ngựa kéo, 1 ngựa, 2 ngựa) shows a specific price on its landing page.
- A clear "Đặt mua ngay" CTA routes to a contact form/Zalo/phone — no cart or online payment attached (§6 Non-Goals).

#### FR-14: Site B blog/content section
Confirmed. Covers craft/heritage topics, care/maintenance advice for wood products, and gifting guides for Tết — same pattern as Site A's blog (FR-7).

**Consequences (testable):**
- Follows the same pattern as FR-7: own per-page SEO fields, indexable, discoverable from Site B navigation, separate from the product catalog.

## 6. Non-Goals (Explicit)

- No shopping cart or online checkout/payment anywhere in v1, on either site — including the wine-barrel line, despite its visible pricing (FR-13). Evaluated and explicitly declined in favor of the phone/Zalo-finalized flow (SM-C1).
- **trongngocanh.com (Site C) is not built in phase 1** — confirmed deferred. This PRD does not define its scope beyond naming its eventual role (§2).
- No automated/site-wide cross-domain link exchange — only editorial, contextual cross-linking (FR-5); see the link-scheme risk note in `addendum.md`.
- No paid-ad campaign management/operation — this PRD ensures Site B's landing page and measurement infrastructure exist; running/budgeting campaigns is out of scope.
- No multi-language/internationalization on either site.
- No CRM or lead-routing system design — form capture (FR-3, FR-8) is in scope; downstream routing is architecture-level.
- No content-editing training program for Phước, despite Piranha making self-editing possible.

## 7. MVP Scope

### 7.1 In Scope

- **Site A**: full rebuild on Piranha with per-page SEO (FR-1) across 11 categories (FR-6), blog (FR-7), phone/Zalo/Maps + contact form (FR-2, FR-3), survey-request form (FR-8), Analytics/Search Console (FR-4).
- **Site B**: new build on Piranha with per-page SEO (FR-1) across 3 product lines (FR-9), craftsman story page (FR-10), blog/content section (FR-14), phone/Zalo/Maps + contact form (FR-2, FR-3), paid-ads landing page capability with visible pricing (FR-12, FR-13) delivered ahead of the Tết 2027 deadline, Analytics/Search Console (FR-4). **Site B is prioritized ahead of Site A** given its fixed external deadline (see the delivery-sequencing note directly below).
- Editorial cross-linking between Site A and Site B (FR-5).
- Content for all categories/lines on both sites migrated and rewritten to SEO standard from the legacy tbtruonghoc.com content as the starting dataset (delivery task, not an end-user-facing FR).

**Delivery risk, sequencing resolved:** this scope is **two full site builds in the same phase**, one of them (Site B) carrying a hard external deadline (Tết Âm Lịch 2027, ~mid-February 2027) and launching from a near-zero content/photo baseline for two of its three product lines (thùng rượu, bồn tắm) — materially more work than the brief's original single-site plan. **Resolved:** Site B is prioritized first because its deadline is externally fixed and Site A's is not; Site A work proceeds in parallel where capacity allows but yields to Site B when the two compete for the same build time. `[NOTE FOR PM: this is a priority ordering, not a schedule — Architecture/Sprint Planning still owns translating it into an actual timeline.]`

### 7.2 Out of Scope for MVP

- Site C (trongngocanh.com) — deferred, no target date set.
- Drum configuration reference (FR-11) — should-have, may slip if it competes with Site B's Tết-deadline work.
- Online cart/checkout on either site — deferred indefinitely, no v2 date scheduled.

## 8. Success Metrics

**Primary**
- **SM-1**: Priority keywords reach page 2 or better (ideally page 1) on Google within 3 months of each site's respective launch — Site A: category terms like dù che nắng, dù che nắng sân trường; Site B: trống lễ hội, trống chùa, trống trường học. Validates FR-1, FR-6, FR-9. `[ASSUMPTION: the brief's original 3-month clock was anchored to "the trongdoitam.net switch"; with no single-site migration event anymore, this is re-anchored to each site's own launch date — flagged for explicit confirmation, Open Question 7.]`

**Secondary**
- **SM-2**: Site A categories other than "quần áo nghi thức" begin generating inbound contact/orders attributed to Google search, vs. today's near-100% phone/referral origin. Validates FR-6, FR-4.
- **SM-3**: At least one Site B wine-barrel landing page is live and receiving paid-ad traffic before Tết Âm Lịch 2027. Validates FR-12, FR-13.

**Counter-metrics (do not optimize)**
- **SM-C1**: Phone/Zalo contact volume/rate must not decrease on either site as organic SEO investment increases. Counterbalances SM-1, SM-2.
- **SM-C2**: Page load speed/Core Web Vitals (LCP, CLS, INP) must not regress on either site, measured against a baseline captured from the legacy tbtruonghoc.com site before Site A/Site B launch, as SEO content volume grows. Counterbalances SM-1. `[NOTE FOR PM: capturing the legacy-site CWV baseline is an acceptance step that must happen before launch, or this counter-metric has nothing to regress against.]`

## 9. Open Questions

1. Exact SKU/spec list for Site A's newly-scoped-in categories (Phòng thí nghiệm Lý-Hóa-Sinh, Bảng tương tác, Màn hình Led, Thiết bị văn phòng, Bàn thí nghiệm, Thiết bị-đồ dùng dạy học, Thiết bị mầm non ngoài trời) — not yet enumerated.
2. ~~Should the survey-request form (FR-8) hard-block out-of-area submissions or just warn?~~ **Resolved:** warn but still accept (see FR-8).
3. Is video content for the Site B craftsman/heritage story page (FR-10) actually available/plannable, or does it launch photo-only?
4. Should FR-11 (drum configuration reference) be sequenced before or after the Tết 2027 wine-barrel work, given both compete for Site B build time?
5. ~~Does Site B need its own blog/content section?~~ **Resolved:** yes, confirmed as FR-14.
6. ~~Resourcing/sequencing across two simultaneous site builds~~ **Resolved:** Site B prioritized first (fixed external deadline); see §7.1. Architecture/Sprint Planning still owns translating this into an actual schedule.
7. The brief's original "3 months from the trongdoitam.net switch" success-metric anchor no longer applies cleanly now that there's no single migration event — confirm whether SM-1's re-anchoring to each site's own launch date (as drafted) is correct.
8. ~~The Product Brief was stale on its Solution/Scope/Vision sections (single-site-then-rebrand model) — should it be formally updated before this PRD is finalized?~~ **Resolved:** updated via `bmad-product-brief`'s Update path; brief and PRD are now in sync.
9. What content/pricing/photos exist today (if any) for bồn tắm gỗ, given it's a from-scratch product line with no legacy-site precedent, unlike thùng rượu (which at least has named variants from the brief)?

## 10. Assumptions Index

- From §5.3 FR-9 — bồn tắm gỗ has no existing content, pricing, or product photos; from-scratch line. See Open Question 9.
- From §5.3 FR-10 — video asset availability/timeline for the craftsman story page is unconfirmed. See Open Question 3.
- From §5.2 FR-6 — SKU/spec list for newly-scoped-in Site A categories not yet enumerated. See Open Question 1.
- From §8 SM-1 — success-metric measurement window re-anchored from "domain switch" to "each site's own launch," since the single-migration event no longer exists. See Open Question 7.
