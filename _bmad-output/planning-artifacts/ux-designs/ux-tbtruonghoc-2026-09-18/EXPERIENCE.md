---
name: Xanh Lục Bảo Rạng Rỡ — Banner Đậm (tbtruonghoc.com)
description: Institutional school-equipment site for tbtruonghoc.com (Site A) — an 11-category catalog split across installation-required and shippable product flows, contact-for-quote only (no cart), phone/Zalo-first B2B procurement journeys. Paired with DESIGN.md.
status: final
sources:
  - "{planning_artifacts}/briefs/brief-tbtruonghoc-2026-09-17/brief.md"
  - "{planning_artifacts}/briefs/brief-tbtruonghoc-2026-09-17/addendum.md"
  - "{planning_artifacts}/prds/prd-tbtruonghoc-2026-09-17/prd.md"
created: 2026-09-18
updated: 2026-09-18
---

# tbtruonghoc.com — Experience Spine

> Site A of the Ngọc Anh multi-site rebuild (Site B — trongdoitam.net — is a separate, already-finalized bmad-ux run; read for house style/rigor only, not for content — Site A is a deliberately distinct sub-brand). This spine and its paired `DESIGN.md` win on conflict with any `.working/` mock, wireframe, or import — those are exploratory rounds, several explicitly superseded by later rounds.

## Foundation

Web, desktop-first with responsive mobile as a secondary target — confirmed by Phước, in explicit contrast to Site B's mobile-first, older-audience design. No named UI system — nothing is inherited from an external component library; bespoke Piranha CMS templates are the implementation target, and `DESIGN.md` and this spine **are** the system for tbtruonghoc.com. `DESIGN.md` is the visual identity reference (colors, typography, shapes, component visual specs); this document is the behavior.

Audience is B2B/institutional, not consumer: school principals, preschool directors, procurement staff, playground/park operators — people evaluating a supplier for an institution's budget, not browsing for themselves. There is **no shopping cart or checkout anywhere** on the site (a project-wide non-goal) — every path resolves to a phone call, a Zalo chat, or one of two forms (the general contact/quote form, FR-3; the installation survey-request form, FR-8). Vietnamese-only, domestic market only, no i18n.

**Delivery-process fact:** visual production goes through Google Stitch, an external tool Claude cannot operate directly — Claude drafts Stitch prompts by hand for Phước to run and iterate on manually (brief `addendum.md`). No Stitch or Figma output exists yet for Site A; every `.working/` file is an HTML exploration used to converge on decisions in this discovery session, not production Stitch output. **This spine and `DESIGN.md` win over any future Stitch iteration that drifts from a token or behavioral rule here** — the Stitch output is the thing to check, not this document.

Two shared project-wide constraints apply here exactly as they do on Site B: FR-1 (per-page SEO fields — title, meta description, slug — editable without developer involvement) and FR-2/FR-3/FR-4 (persistent phone/Zalo/Maps contact on every page, a general contact/quote form, and separate Analytics/Search Console per site). FR-5's editorial-only cross-site backlinking (never a sitewide link block, to avoid a PBN/link-scheme signal between commonly-owned domains) applies to any Site A ↔ Site B mention. No dark mode requirement, no regulated-industry compliance burden beyond ordinary institutional procurement trust.

## Information Architecture

| Surface | Reached from | Purpose |
|---|---|---|
| Homepage | Direct/search entry, nav logo | Confident-brand entry point: `{hero-carousel}`/`{hero-fallback}`, 6-of-11 featured categories + "xem tất cả," trust-stat band, footer strip |
| Danh mục sản phẩm (aggregate category page) | Nav "Sản phẩm" dropdown "Xem tất cả," homepage "xem tất cả" link | Full listing of all 11 categories in one place — the browse path, complementing the dropdown's quick-pick path |
| Category page (× 11) | Nav dropdown, aggregate page, homepage featured tile | Dù che nắng sân trường, Nội thất mầm non, Quần áo nghi thức & cờ đội, Thiết bị âm thanh – máy chiếu, Bảng tương tác, Màn hình LED hiển thị, Thiết bị văn phòng, Phòng thí nghiệm Lý–Hóa–Sinh, Bàn thí nghiệm, Thiết bị–đồ dùng dạy học, Thiết bị mầm non ngoài trời (PRD §2/FR-6) |
| Product detail — installation-required | Category page (e.g. Dù che nắng) | FR-8 survey-driven PDP — see Component Patterns |
| Product detail — shippable | Category page (e.g. Nội thất mầm non) | Simpler, no-survey PDP — see Component Patterns |
| Khảo sát request overlay | Installation-required PDP CTA | FR-8 survey-request modal, in-page overlay, not a separate page navigation |
| Blog / Tin tức | Nav | FR-7 long-form SEO content ("nên chọn loại nào," "cách chọn...") — separate from the product catalog |
| Giới thiệu (About) | Nav | Company background/credibility page |
| Liên hệ / general contact form | Nav, embedded on any product/category page | FR-3 general lead capture; not a form reserved to a single destination page |

**Nav pattern for 11 categories:** the "Sản phẩm" nav item opens a `{components.nav-dropdown}` — a simple single-column list of all 11 category names (not mega-menu — see DESIGN.md Layout & Spacing) — ending in a "Xem tất cả 11 nhóm sản phẩm →" link to the aggregate **Danh mục sản phẩm** page. The dropdown is the quick path for a visitor who already knows what they want; the aggregate page is the browse path for a visitor who doesn't. Both must exist and stay in sync with the same category list.

**Structural CMS/IA constraint:** the category list must support adding new categories dynamically in the future without a code change — flagged explicitly by Phước as structural, not just a content-authoring convenience. This affects the nav dropdown, the aggregate page's grid, and any category-count-dependent copy ("Xem tất cả 11 nhóm sản phẩm" must not be a hardcoded string once a 12th category exists). The aggregate page renders `{components.category-tile}` at full, CMS-driven count (not the homepage's curated 6), plus a visibly distinct placeholder tile demonstrating that the grid isn't hardcoded — approved without changes. → [mockups/key-danh-muc-tong-hop.html](mockups/key-danh-muc-tong-hop.html)

Homepage structure top-to-bottom: `{components.nav}` → `{components.hero-carousel}`/`{components.hero-fallback}` → 6 featured `{components.category-tile}` cards + "xem tất cả" link → `{components.trust-stat}` band → `{components.footer-strip}`. Category → product hierarchy is two levels deep everywhere (category page → product detail); no sub-category nesting was identified in the PRD's 11-category list. Modal/overlay stacking never exceeds one level (the survey-form overlay is the only modal identified; it does not open further modals on top of itself).

**Breadcrumb, every deep page:** `{components.breadcrumb}` appears directly under `nav` on every category page, PDP, and the aggregate page — Trang chủ / Sản phẩm / [danh mục] / [sản phẩm], each step but the last a link. Absent on the homepage (nothing to trail back from). Surfaced during Stitch-output review as a wayfinding gap; adopted because it reinforces the institutional-trust register cheaply.

**Search + filter on the aggregate page:** `{components.category-search}` sits between the aggregate page's header and its category grid — a text search plus a row of toggleable filter chips grouping the 11 (and future) categories loosely by use-case. Becomes more valuable as the category count grows past what a single glance can scan; adopted for that reason.

## Voice and Tone

Microcopy only — brand posture and aesthetic register live in `DESIGN.md` Brand & Style. Register: **professional, institutional Vietnamese B2B** ("chuyên nghiệp, đáng tin cậy" — Phước's explicit direction), speaking to a procurement decision-maker, not a consumer. `[ASSUMPTION: word-level register below is inferred from the session's decisions (the brand-direction phrase, the audience description, the explicit rejection of the legacy site's promo-badge tone) — not a verbatim client style guide, the same caveat Site B's EXPERIENCE.md carries for its own Voice and Tone. Actual page/blog/product prose is written later, directly in Piranha CMS, by the client — this spine defines the register content should land in, not the content itself.]`

| Do | Don't |
|---|---|
| "Đội thi công riêng, khảo sát và lắp đặt trọn gói." — specific, capability-led | "Giảm giá sốc! Mua ngay!" — the legacy site's discount-badge, urgency-driven register |
| "Liên hệ để nhận báo giá." / "Đăng ký khảo sát miễn phí." — plain, offers a clear next step | Vague marketing superlatives ("chất lượng hàng đầu," "uy tín số 1") without a specific claim underneath |
| Specific, verifiable detail: service-area bound stated plainly ("Miền Bắc – Thanh Hóa"), process steps named (Khảo sát → Hợp đồng → Thi công) | Burying the service-area limitation or the survey process behind vague reassurance |
| Calm, procedural language matched to a procurement audience evaluating a supplier | Consumer-retail hype language, exclamation-heavy copy, countdown/urgency framing |
| "Giao hàng toàn quốc" only on genuinely shippable products | Implying nationwide shipping or no-installation-needed on a product that requires a site survey |

Site A's professional-institutional register is deliberately distinct from Site B's warm/craft-heritage voice (see Site B EXPERIENCE.md Voice and Tone for contrast) — the two sites should never sound interchangeable, matching `DESIGN.md`'s instruction that they must never look interchangeable either.

## Component Patterns

Behavioral. Visual specs live in `DESIGN.md` Components — every component below has a matching visual-spec entry there under the same name.

| Component | Use | Behavioral rules |
|---|---|---|
| `nav` | Every page | Flat link list + "Sản phẩm" dropdown trigger. Floating `contact-chip` always visible at the nav's right edge — never scrolled away, never hidden behind a menu. [mockups/direction-thuong-hieu-rong-rai.html](mockups/direction-thuong-hieu-rong-rai.html) |
| `nav-dropdown` | "Sản phẩm" nav item | Opens on hover/click, lists all 11 categories as a simple single-column list (not mega-menu), ends in a "Xem tất cả" link to the aggregate page. Click/tap anywhere on a row navigates to that category. [mockups/key-chi-tiet-can-thi-cong.html](mockups/key-chi-tiet-can-thi-cong.html) (shown open) |
| `hero-carousel` / `hero-fallback` | Homepage | Renders as a photo carousel when product/installation photography is available for the rotation; falls back to a solid-teal banner (no photo) when it is not. Both states must be designed and must render correctly — a homepage must never assume photos exist. Carousel auto-behavior (interval, pause-on-hover) is not specified in this session — architecture/implementation default. [mockups/direction-thuong-hieu-rong-rai.html](mockups/direction-thuong-hieu-rong-rai.html) (carousel state) |
| `category-tile` | Homepage (6 of 11), Danh mục sản phẩm (all 11 + placeholder) | Tap/click anywhere on the tile navigates to that category page. Homepage shows a curated 6, not a full CMS-driven 11 — the aggregate page is where the full, CMS-flexible list renders, including a visibly distinct placeholder tile for future categories. [mockups/key-danh-muc-tong-hop.html](mockups/key-danh-muc-tong-hop.html) |
| Product detail — installation-required | Category pages whose products require on-site work (e.g. Dù che nắng sân trường) | Adds `service-area` statement and `process-strip` blocks not present on shippable PDPs; primary CTA is "Đăng ký khảo sát miễn phí," opening the `survey-form-modal`. [mockups/key-chi-tiet-can-thi-cong.html](mockups/key-chi-tiet-can-thi-cong.html) |
| Product detail — shippable | Category pages whose products ship without installation (e.g. Nội thất mầm non) | Simpler layout: no service-area/process-strip blocks; adds a shipping-note chip instead; primary CTA is "Yêu cầu báo giá," routing to the general contact form (FR-3), not the survey overlay. [mockups/key-chi-tiet-ship-thang.html](mockups/key-chi-tiet-ship-thang.html) |
| `price-block` | Both PDP variants | Renders a real price (`{typography.price}`) when the CMS product record has a price field filled; renders "Liên hệ để nhận báo giá" (`{typography.price-fallback}`) in the identical block position and styling when it does not. This is a CMS-conditional state per product, not a fixed per-category rule — a shippable product can still be price-absent, and the reverse — an installation-required product with a set price — is architecturally possible, even though installation-required products are price-absent in every rendered example. |
| `survey-form-modal` | Installation-required PDP CTA | Opens as an in-page overlay, not a page navigation — dimmed/blurred backdrop over the PDP behind it. Minimum fields: sản phẩm quan tâm (prefilled/linked from the product page context, read-only styled), địa điểm/địa chỉ*, họ tên*, số điện thoại* (FR-8's minimum field set: location, product of interest, name + phone). Submits without leaving the page. [mockups/key-form-khao-sat.html](mockups/key-form-khao-sat.html) |
| `warning-banner` | Inside `survey-form-modal`, on địa điểm field | Fires when the entered location falls outside Miền Bắc – Thanh Hóa. Non-blocking: the warning appears, the affected field's border tints amber, but the submit button stays enabled and the form still sends (FR-8, PRD Open Question 2, resolved: warn, don't block). [mockups/key-form-khao-sat.html](mockups/key-form-khao-sat.html) (state b) |
| `cta-buttons` | Every page with an action | Primary (amber fill) is always the more specific, higher-intent action for that context (Đăng ký khảo sát miễn phí, Yêu cầu báo giá, Gửi đăng ký, Đặt mua ngay); secondary (teal outline) is always the phone/Zalo fallback (Gọi ngay · Zalo tư vấn). Never invert which action gets the primary treatment. |
| `contact-chip` | Every page, nav | Persistent, functional `tel:` and Zalo deep-link — never a JavaScript-only handler that fails silently without the Zalo app present; both need a sensible web fallback. |
| `trust-badge` / `trust-stat` | PDP (`trust-badge`, e.g. Bảo hành chính hãng), Homepage (`trust-stat`, e.g. 500+ trường) | Both are non-interactive, informational-only — no click behavior. `trust-stat` deliberately sits below the category grid, not above the fold, so it doesn't compete with the hero/category-grid brand moment. |
| `breadcrumb` | Every category page, PDP, aggregate page (not homepage) | Each step but the current page is a click-through link back up the hierarchy. Text-only, no icons. |
| `category-search` | Aggregate page | Text search filters the category grid live (client-side, since the full list is small); filter chips are multi-select toggles that narrow the grid by loose use-case grouping, not a strict taxonomy — a category may appear under more than one chip. |
| `photo-badge` | PDP gallery, on real installation/project photos only | Non-interactive label, "Hình ảnh thi công thực tế." **Never applied to stock, placeholder, or AI-generated imagery** — the badge is itself a trust claim about photo authenticity, so applying it to non-genuine photography would be a false claim. |
| `category-tile` certification badges | Aggregate page tiles (optional) | Shown only when a real, verifiable certification exists for that category in the CMS (e.g. ASTM, CARB P2, TT 38/39) — an optional per-category field, never a default/placeholder value. Absent by default until content ops confirms and enters a real certification. |

## State Patterns

| State | Surface | Treatment |
|---|---|---|
| Hero: photos available, desktop/tablet (≥768px) | Homepage | `{components.hero-carousel}` — full-bleed photo background, gradient overlay, carousel dot/arrow controls. Tablet (~768–1023px) uses the same carousel, resized. |
| Hero: no photos available, desktop/tablet (≥768px) | Homepage | `{components.hero-fallback}` — solid `{colors.banner-bg}`, same heading/CTA content, no carousel controls. Must render correctly, not as a broken/empty state — this is the documented default when a rotation has no photo assets, not an error condition. |
| Hero: phone width (<768px) | Homepage | `{components.hero-fallback}` ALWAYS, regardless of photo availability — a screen-width override, not a photo-availability rule. Photo carousels are dropped entirely below tablet width: shorter load time, and no swipe-vs-page-scroll gesture conflict on a phone. Directional, from `stitch-prompts.md`; not yet visually confirmed by a rendered mock. |
| Price present | `price-block`, both PDP variants | CMS product record has a price field filled → show `{typography.price}` value in `{colors.primary}`, plus a short note (e.g. bulk-order contact hint). [mockups/key-chi-tiet-ship-thang.html](mockups/key-chi-tiet-ship-thang.html) |
| Price absent | `price-block`, both PDP variants | CMS product record's price field is empty → show "Liên hệ để nhận báo giá" in `{typography.price-fallback}`, same block position/styling, plus a note on why (e.g. price depends on site conditions). [mockups/key-chi-tiet-can-thi-cong.html](mockups/key-chi-tiet-can-thi-cong.html) |
| Survey location within service area | `survey-form-modal` | No warning shown; form behaves as a normal lead-capture submission. [mockups/key-form-khao-sat.html](mockups/key-form-khao-sat.html) (state a) |
| Survey location outside service area | `survey-form-modal` | `warning-banner` appears between the địa điểm field and họ tên field; địa điểm field border tints amber; submit stays enabled and functional — **non-blocking by explicit PRD resolution** (FR-8/Open Question 2). Sales reviews and decides case-by-case rather than losing the lead outright. [mockups/key-form-khao-sat.html](mockups/key-form-khao-sat.html) (state b) |
| Empty/new category (0 published products) | Category page, `category-tile`/aggregate grid | `[ASSUMPTION: not directly discussed for Site A in this session. Extrapolated from the explicit CMS/IA constraint that new categories must be addable dynamically — a newly-added category will necessarily start empty. Reasonable default, consistent with Site B's equivalent rule: hide an empty category tile from the grid entirely rather than render a bare "no products" page, until it has at least one published product.]` |
| Form submit success (both forms) | `survey-form-modal`, general contact form | `[ASSUMPTION: not rendered or discussed in this session — no confirmation-state mock exists for either form. Reasonable default, consistent with Site B's equivalent pattern: an inline confirmation message replaces the form in place, no page redirect, no separate modal.]` |
| Form submit failure (network/server) | `survey-form-modal`, general contact form | `[ASSUMPTION: not rendered or discussed. Reasonable default: entered values are retained, not cleared, and an inline error offers phone/Zalo as an immediate fallback — this audience already defaults to calling.]` |

## Interaction Primitives

- Tap/click anywhere on a `category-tile` navigates to that category; the dropdown's row behaves the same way.
- The "Sản phẩm" nav dropdown opens on hover (desktop) or click/tap (touch), closes on click-away or selecting an item. `[ASSUMPTION: exact open/close trigger (hover vs. click) was not tested for desktop-first Site A the way Site B specified touch-only interaction — reasonable default given desktop-first: hover-to-open with a click fallback, since this is standard desktop nav convention and no rule against it was stated.]`
- CTA hierarchy is fixed sitewide: primary = amber fill = the highest-intent action available in that context; secondary = teal outline = the phone/Zalo fallback. This pairing appears identically in every rendered mock (homepage hero, both PDP variants, the survey modal) — treat it as load-bearing, not a per-page choice.
- The survey-form modal's out-of-area warning is evaluated live against the entered địa điểm text (or a structured location field, at Architecture's discretion) and never blocks submission — this is the one explicitly resolved (PRD Open Question 2) non-blocking-validation rule in the whole system; do not generalize it into "no form ever blocks" without a similar explicit resolution.
- Click-to-call (`tel:`) and the Zalo deep link are real, functional links from the nav's `contact-chip` on every page — never JavaScript-only handlers with no web fallback.
- **Banned:** cart/checkout flows anywhere (project-wide non-goal), a mega-menu for the 11-category nav (explicitly rendered and not picked), showing a broken/empty hero when no photos exist, hard-blocking the survey form on an out-of-area location.

## Accessibility Floor

Behavioral; visual contrast values live in `DESIGN.md`.

- `[ASSUMPTION: exact contrast ratio target was not discussed for Site A in this session. WCAG 2.1 AA is assumed as a reasonable site-wide floor, consistent with Site B's equivalent assumption — not directly elicited or confirmed with the client, flagged for triage.]`
- Focus order on every page follows visual reading order: nav → hero → content → footer on the homepage; nav → breadcrumb → gallery/info → specs → (service-area → process-strip, installation-required only) → footer on a PDP.
- `survey-form-modal` fields are individually labeled (not placeholder-only labels that disappear on focus); required fields carry a visible `{colors.error}`-colored asterisk marker, not color alone — the label text itself should also convey "required" for screen-reader users (e.g. via an `aria-required` attribute or equivalent, an implementation-level detail this spine flags but does not specify).
- The out-of-service-area `warning-banner` must be perceivable without relying on color alone: it carries an icon ("!" mark), a bold headline, and full explanatory text — not just an amber field-border tint. Any implementation must keep all three, not reduce the signal to color alone.
- Every `contact-chip` and CTA button has an accessible label describing its destination (e.g. "Gọi ngay tới Ngọc Anh," not just "Gọi ngay" with no context for a screen-reader user landing mid-page).
- The `nav-dropdown` must be fully keyboard-operable (open on focus/Enter, navigate with arrow keys or Tab, close on Escape) since it is the primary path to 10 of the 11 categories from any page.
- All product photography carries descriptive alt text — this matters more than usual on PDPs, which are photo-led with comparatively little body text.
- Tap targets meet a minimum comfortable size for a desktop-first-but-responsive audience; the specific numeric target (e.g. 44×44px equivalent) is left to Architecture/implementation to formalize once mobile behavior (an open item — see Foundation) is designed.

## Key Flows

Two representative journeys — one installation-required, one shippable — show the behavioral rules above in context.

### Flow 1 — UJ-1: Cô Lan sources a sunshade canopy that needs installation

1. Cô Lan, hiệu trưởng of a preschool, arrives via Google search or word of mouth — lands on the **Dù che nắng sân trường** category page, then a specific product detail page (installation-required variant, e.g. "Dù che sân trường học"). [mockups/key-chi-tiet-can-thi-cong.html](mockups/key-chi-tiet-can-thi-cong.html)
2. She browses the gallery and specs table; the `price-block` shows "Liên hệ để nhận báo giá" (price-absent state) — expected for an installation-dependent product whose cost depends on site conditions.
3. She reads the `service-area` block ("Khu vực phục vụ: Miền Bắc – Thanh Hóa") — this addresses the PRD's named edge case directly: she needs service-area clarity *before* investing time in a survey request, and the page states it plainly rather than burying it.
4. She calls the hotline via the nav's `contact-chip` (her preferred first step) and speaks with a sales rep, who walks her toward requesting a formal on-site survey.
5. She (or the rep, on her behalf) opens the `survey-form-modal` via the "Đăng ký khảo sát miễn phí" CTA. The sản phẩm quan tâm field is already linked to "Dù che sân trường học" from page context; she fills địa điểm, họ tên, số điện thoại. Her school is within the service area, so no warning appears. [mockups/key-form-khao-sat.html](mockups/key-form-khao-sat.html) (state a)
6. She submits. A technician surveys the schoolyard in person, then sends a formal quote matched to actual site conditions (the `process-strip`'s step 1 → step 2).
7. **Climax:** She receives the site-condition-matched quote and signs — the page's job (state the service area clearly, make the survey request effortless, hand off to a human for the technical part) is done; the quote-and-contract conversation happens off-platform by design.
8. **Resolution:** Contract signed, installed by the company's own crew (`process-strip` step 3).

Edge case, addressed structurally: if Cô Lan's school had fallen outside Miền Bắc – Thanh Hóa, the `service-area` block's plain statement would have told her before she opened the survey form at all — and even if she'd proceeded anyway, the `warning-banner` state (non-blocking) would still have accepted her submission rather than turning her away outright. [mockups/key-form-khao-sat.html](mockups/key-form-khao-sat.html) (state b)

### Flow 2 — UJ-2: Thầy Nam orders a standalone dù tròn (shippable, no installation)

1. Thầy Nam, a procurement contact at a khu vui chơi/công viên with a large open area, needs umbrellas that ship rather than requiring a survey/contract cycle — same buyer type as Cô Lan (installation-required vs. shippable is a product-line split, not a persona split), but a simpler purchase.
2. He arrives on a shippable-product category page (e.g. within Nội thất mầm non, or a comparable shippable dù listing) and opens a product detail page in the shippable variant. [mockups/key-chi-tiet-ship-thang.html](mockups/key-chi-tiet-ship-thang.html)
3. He reviews size/material options in the specs table. Unlike Cô Lan's page, there is no service-area block and no process-strip — the page is visibly simpler because there's no survey/install step to explain.
4. The `price-block` shows a real price (price-present state) with a per-unit note about bulk-order pricing for larger quantities — pricing transparency this audience can act on without a phone call first.
5. He notes the shipping-note chip ("Giao hàng toàn quốc — 3–7 ngày làm việc tùy khu vực"), confirming nationwide delivery applies.
6. He taps "Yêu cầu báo giá," routing to the general contact form (FR-3) rather than the survey overlay — this CTA never promises an installation visit it can't deliver, matching Voice and Tone's rule against overpromising.
7. **Climax:** He agrees a price and quantity by phone/Zalo following up on the form submission — the page's job (clear specs, real price, honest shipping expectation) is done without ever needing a site visit.
8. **Resolution:** Delivered; installation offered only as an optional paid add-on, not a required step — the shippable flow stays genuinely simpler end to end, not just visually simpler.

## Open Items for UX

Not elicited from the client in this session; full reasoning stays at each citation, this is a scan index for triage — mirrors the equivalent table in Site B's EXPERIENCE.md.

| Open item | Sections | Question |
|---|---|---|
| Mobile visual confirmation | Foundation, Layout & Spacing (`DESIGN.md`) | Nav-collapse pattern is now decided (hamburger menu, sliding from the side, "Sản phẩm" expands as an inline accordion of all 11 categories — see `stitch-prompts.md`) and multi-column grids are specified to stack to 1 column on mobile. But no mobile mock has been rendered and reviewed yet — every `.working/`/`mockups/` file is desktop-only; the mobile direction exists only as Stitch-prompt instructions. Confirm against actual Stitch output before implementation. |
| Contrast ratio target | Accessibility Floor | WCAG 2.1 AA assumed as the site-wide floor — confirm with client. |
| Form submit success/failure states (both forms) | State Patterns | No confirmation or error-state mock exists for either the survey form or the general contact form — behavior specified here is extrapolated from Site B's equivalent pattern, not independently confirmed for Site A. |
| Empty/new category rendering | State Patterns | Not directly discussed; extrapolated from the explicit dynamic-category IA constraint. |
| Error/validation-error color | `DESIGN.md` Colors | Only a required-field asterisk color was ever rendered; no full validation-error message state was designed. Extended from that one confirmed value — confirm before implementation. |
| Voice and Tone word-level register | Voice and Tone | Inferred from session decisions, not a client-confirmed style guide. Actual content authoring happens later in Piranha CMS by the client, not as part of this UX deliverable. |
