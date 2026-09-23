---
name: Mộc Trầm (trongdoitam.net)
status: final
sources:
  - "{planning_artifacts}/briefs/brief-tbtruonghoc-2026-09-17/brief.md"
  - "{planning_artifacts}/briefs/brief-tbtruonghoc-2026-09-17/addendum.md"
  - "{planning_artifacts}/prds/prd-tbtruonghoc-2026-09-17/prd.md"
created: 2026-09-17
updated: 2026-09-18
---

# trongdoitam.net — Experience Spine

> Site B of the Ngọc Anh multi-site rebuild (Site A — tbtruonghoc — is a separate, out-of-scope bmad-ux run). Heritage-woodcraft catalog: trống (all types, one hub + 5 subpages), thùng rượu gỗ trang trí, bồn tắm gỗ, plus a distinct paid-ads landing page for the wine-barrel line. Paired with `DESIGN.md` (Mộc Trầm). The optional multi-lens Reviewer Gate (UX-specific validation: flow/token/component/state coverage) was explicitly skipped by the user for this run — this pair went through the structure/prose editorial polish pass only.

## Foundation

Mobile-first responsive web. No named UI system — nothing is inherited from an external component library; `DESIGN.md` and this spine together **are** the system for trongdoitam.net. Vietnamese-only, domestic market only (no i18n). No shopping cart or checkout anywhere on the site — every product and landing page resolves to a contact channel (phone, Zalo, or the quote-request form), never to a cart.

**Delivery-process fact:** visual production goes through Google Stitch, an external tool Claude cannot operate directly — Claude drafts Stitch prompts by hand for the client to run and iterate on manually (see brief `addendum.md`). No Stitch or Figma output exists yet. **`DESIGN.md` and this spine win over any future mock, wireframe, or Stitch import on conflict** — a Stitch iteration that drifts from a token or a behavioral rule here is the thing that's wrong, not this document.

## Information Architecture

| Surface | Reached from | Purpose |
|---|---|---|
| Homepage | Direct/search entry, nav logo | Dense catalog-forward entry point: flexible-count category block for all 3 product lines, trust strip, sticky contact bar |
| Trống — hub | Nav, homepage category-tile | Overview of all drum types under one workshop story; links out to the 5 subcategory pages |
| Trống — Trường học | Trống hub, nav submenu, search | Dedicated SEO surface for school-ceremony drums; own keyword target (SM-1) |
| Trống — Lân | Trống hub, nav submenu, search | Dedicated SEO surface for lion-dance drums |
| Trống — Đội | Trống hub, nav submenu, search | Dedicated SEO surface for ceremonial-troupe drums |
| Trống — Lễ hội | Trống hub, nav submenu, search | Dedicated SEO surface for festival drums; own keyword target (SM-1) |
| Trống — Chùa | Trống hub, nav submenu, search | Dedicated SEO surface for temple drums; own keyword target (SM-1); primary surface for UJ-3 |
| Thùng rượu gỗ (product/category page) | Nav, homepage category-tile | Organic (non-paid) presentation of the wine-barrel line; contact-for-quote, distinct from the paid landing page |
| Bồn tắm gỗ (product page) | Nav, homepage category-tile | New, from-scratch line; placeholder-safe template (see State Patterns) |
| Câu chuyện nghệ nhân (craftsman/heritage story page) | Linked from all 3 product-line pages, footer | Trust-building step realizing UJ-3; photo required, video should-have |
| Blog / nội dung (listing page) | Nav | Craft/heritage articles, wood care advice, Tết gifting guides (FR-14); separate from product catalog; listing only — see Blog post detail below for the article template |
| Blog post detail | Blog / nội dung (listing page) | `article-detail-page` template: one article's own URL/SEO — the layer the finalized IA was missing (client-flagged gap, see `.memlog.md`) |
| Trống — model/loại detail (e.g. "Trống chùa loại 2") | Trống — Trường học, Lân, Đội, Lễ hội, Chùa (product-card grid on each subcategory page) | `product-detail-page` template: one specific drum model's own photos/specs/URL/SEO. `[ASSUMPTION: exact model/loại count and naming per subcategory is not enumerated anywhere — PRD Open Question 1 — illustrative placeholder (e.g. loại 1/2/3 per subcategory) used to specify this pattern; see Open Items.]` |
| Thùng rượu gỗ — variant detail | Thùng rượu gỗ (product/category page) | `product-detail-page` template per confirmed variant (gỗ sồi: ngựa kéo, 1 ngựa, 2 ngựa) — own photos/specs/URL/SEO; organic/SEO counterpart to the separate `landing-page-cta` template (FR-12/13), not a duplicate of it — that template stays the single-page, all-variants-priced paid-ads destination |
| Bồn tắm gỗ — product detail | Bồn tắm gỗ (product page) | `product-detail-page` template, placeholder-safe (see State Patterns) — no real content yet, so kept minimal/generic rather than inventing variant names |
| Landing page — Thùng rượu (paid ads) | Paid-ad click only; excluded from nav | Single-page, conversion-focused destination realizing UJ-4; visible per-variant pricing (FR-13) |
| Liên hệ / Yêu cầu báo giá (general contact form) | Embedded on any product/category page, footer | FR-3 general lead capture; every product/category surface embeds this, it is not a separate destination-only page |

Nav is a flat single level (hub + submenu for Trống, flat links for the other two lines) — the catalog is shallow by design (3 product lines total), so no mega-menu or multi-level drawer is needed. Modal stacks never exceed one level (e.g. a lightbox photo viewer on the story page never opens another modal on top of itself).

→ Composition reference: no Stitch/Figma mockups exist for Site B yet — [mockups/color-themes.html](mockups/color-themes.html) and [mockups/homepage-direction.html](mockups/homepage-direction.html) are the exploratory HTML mocks used to lock the palette and homepage register (see DESIGN.md Colors/Components for the same links in visual-spec context; spine wins on conflict — see Foundation).

## Voice and Tone

Microcopy only — brand posture and editorial voice live in `DESIGN.md` Brand & Style. `[ASSUMPTION: word-level register below is inferred from the session's decisions (palette-choice reasoning, audience description, the craftsman-first differentiator) — not a verbatim client style guide. Reasonable default: warm, calm, unembellished, grounded in real craftsmanship — but not directly confirmed word-for-word with the client.]` Actual page/blog/product prose is written later, directly in Piranha CMS, by the client — this spine defines the register that content should land in, not the content itself.

Inferred register: warm, unembellished (không phô trương — no hard-sell superlatives), trầm tĩnh/điềm đạm (calm, unhurried — matching a craft-heritage brand talking to temple and communal-house buyers who value authenticity over sales pressure), grounded in real, specific craftsmanship detail rather than marketing hype, and respectful of institutional buyers (đình, chùa, nhà thờ dòng họ) who are evaluating trust and meaning, not just price.

| Do | Don't |
|---|---|
| "Nghệ nhân Phạm Trí Trong trực tiếp kiểm định từng sản phẩm." — specific, named, verifiable | "Chất lượng hàng đầu, uy tín số 1!" — generic superlative claims |
| "Liên hệ để được tư vấn cấu hình trống phù hợp." — calm, offers help | "Mua ngay kẻo lỡ! 🔥" — urgency/hype framing, exclamation-heavy |
| Plain, complete sentences; Vietnamese honorifics used naturally where a real conversation would use them (bác, chị, anh) | Flattening every audience to the same casual internet register |
| Specific craft/material detail (gỗ mít, da trâu, đánh bóng thủ công) | Vague marketing adjectives standing in for detail ("cao cấp", "sang trọng" with nothing under it) |
| "Đặt mua ngay" only where a real price is shown (landing page) | "Đặt mua ngay" on contact-for-quote product pages — the CTA should never promise a transaction the page can't complete |

## Component Patterns

Behavioral. Visual specs live in `DESIGN.md.Components` — every component below has a matching visual-spec entry there under the same name.

| Component | Use | Behavioral rules |
|---|---|---|
| `nav` | Every organic page (not the landing-page-cta template) | Flat link list + Trống submenu (5 subpages). Mobile: hamburger opens a full-height sheet, one level, no nested accordions beyond the single Trống group. Tablet/desktop only: displays the hotline number, adopted from the first Stitch export. No account/login icon or any account-implying UI on any breakpoint, on any page — the site has no accounts (no cart/checkout is a site-wide non-goal); a Stitch draft added one on the Blog screens only and it must not ship. |
| `trust-block` | Trống subcategory pages, any `product-detail-page` | Short craftsman testimonial attributed to Phạm Trí Trong, embedded inline (not a separate page) at the point a buyer is deciding. Adopted from the first Stitch export. Supplements, never replaces, the dedicated craftsman/heritage story page. |
| `sticky-contact-bar` | Every organic page, fixed to viewport bottom | Always visible, never auto-hides on scroll (this audience needs the channel reachable at all times, not just on demand). Zalo segment (`{colors.primary}` fill) opens Zalo deep link or web fallback; phone segment triggers `tel:`; Maps segment opens Google Maps in a new tab. Absent only on the landing-page-cta template, which uses `landing-page-cta` instead. |
| `category-tile` | Homepage flexible-category-block | Renders one tile per CMS-defined category — count is not fixed at 3. Tap/click anywhere on the tile navigates to that category's hub/product page. Empty category (0 published products) is hidden from the grid entirely, not shown as an empty tile. |
| `product-card` | Trống hub + 5 subpages, Thùng rượu page, Bồn tắm page, homepage grid | Tap anywhere on the card body navigates to product detail; the CTA button is a secondary, larger tap target for the same destination (both must work — don't require precision-tapping the small button on a dense grid). Pressed/active state (`{colors.primary}` inset ring + 0.98 scale, per `DESIGN.md`) fires on touch-down, holds through the tap, gives feedback before navigation completes. Price row shows a real `{typography.price}` value only for the wine-barrel line's landing-page context; everywhere else (drums, bathtubs) shows "Liên hệ báo giá" in muted text — see Voice and Tone for why the CTA label must match. |
| `landing-page-cta` | Wine-barrel paid-ads landing page only | One CTA per variant card, "Đặt mua ngay," routes to the `quote-request-form` (embedded inline on the same page, not a separate page) or to Zalo/phone — never to a cart or payment step (site-wide non-goal). No nav, no 3-segment sticky bar on this template — replaced by this single persistent CTA so the page has exactly one job. |
| `quote-request-form` | Embedded on every product/category page, and inline on the landing page | Minimum fields: name, phone, product/category of interest, message. Client-side validation shows inline `{colors.error}` text under the specific invalid field on blur, not only on submit. Successful submit shows an inline confirmation state in place of the form (not a redirect, not a modal) — see State Patterns. |
| `craftsman-story-block` | Craftsman/heritage story page | Photo carousel or grid required at launch. Video slot renders only if a video asset exists; its absence must not leave a visible gap, broken embed, or placeholder box — see State Patterns for the exact degrade rule. |
| `drum-config-reference` | Trống subpages (should-have, FR-11; may not ship at launch — see Open Items) | Read-only reference table (size × loại 1/2/3 × bánh xe × sơn × vẽ mặt trống); no price computation. Carries a short disclaimer line under the table ("Bảng tham khảo, không tính giá tự động") — adopted from the first Stitch export, makes the reference-not-calculator rule legible to the end user. Every row ends at the same `quote-request-form`/Zalo/phone CTA — it narrows the conversation, it doesn't replace it. |
| `product-detail-page` | Reached from each Trống subcategory page's `product-card` grid, the Thùng rượu gỗ category page's variant list, and the Bồn tắm gỗ category page | Photo gallery leads the page; degrades to a single photo with no empty chrome, same rule as `craftsman-story-block`. Spec rows reuse `drum-config-reference`'s row treatment filtered to this one model — not a duplicated full table. Price row follows the `product-card` rule exactly: real `{typography.price}` only where a genuine price exists, otherwise "Liên hệ báo giá" in muted text — drum, bathtub, and *organic* wine-barrel-variant detail pages all stay contact-for-quote; only the separate `landing-page-cta` template shows real per-variant pricing (FR-13), and the two must never be conflated. This rule is easy to get wrong under a dense, price-forward catalog register — the first Stitch export fabricated prices for the organic Thùng rượu gỗ and Bồn tắm gỗ homepage cards; it must hold everywhere except `landing-page-cta`. Page shows a small `sku-code` (e.g. "Mã: TC-L2-160") near the title and ends in an embedded `quote-request-form`, same terminal pattern as `drum-config-reference`. |
| `article-detail-page` | Reached from Blog / nội dung listing, individual post links | Article header (title, optional byline/date metadata) followed by `body` running copy. Ends in a related-posts module (reusing the `product-card` grid convention) when related posts exist, or a plain "← Quay lại Blog" back-to-listing link when none do — see State Patterns. Never a dead end. |

## State Patterns

| State | Surface | Treatment |
|---|---|---|
| Cold load | Any page | Standard page load; no client-side app-shell skeleton needed (server-rendered CMS pages) — treat slow network as a performance concern (SM-C2), not a state to design chrome for. |
| Empty category | `category-tile` grid, any `product-card` grid | Category/product-card grid with 0 items is hidden from its parent surface entirely rather than rendered empty — a visitor should never see a bare "no products" grid on a live catalog page. |
| Empty blog/content section | Blog / nội dung | If no posts are published yet, the section shows a plain "Bài viết đang được cập nhật" message rather than a bare empty list — same principle as the bồn tắm gỗ pre-content state, applied to a listing surface instead of a product page. Nav link to Blog is not hidden just because it's currently empty. |
| Field focus | `quote-request-form` | Focused field border swaps from `{colors.border}` to `{colors.primary}` (native cursor/keyboard, no custom focus chrome beyond the border color). Focus ring must remain visible for keyboard navigation, not only pointer/touch. |
| Form validation error | `quote-request-form` | Inline `{colors.error}` message directly under the invalid field, on blur and again on submit attempt. Never a single top-of-form error summary — the user should not have to hunt for which field failed. |
| Form submit success | `quote-request-form` | Inline confirmation message replaces the form in place ("Đã nhận yêu cầu, chúng tôi sẽ liên hệ sớm.") — no page redirect, no modal. |
| Form submit failure (network/server) | `quote-request-form` | Entered values are retained (never cleared on failure); inline error offers phone/Zalo as an immediate fallback channel, since this audience already defaults to calling. |
| Video asset missing | `craftsman-story-block` | `[ASSUMPTION: video availability for the craftsman-story page is unconfirmed (PRD Open Question 3) — reasonable default: the page must degrade gracefully photo-only.]` Photo-only degrade: video slot is omitted from layout entirely (no empty player, no "video coming soon" placeholder) when no asset exists. This must work at launch. |
| Bồn tắm gỗ page, pre-content | Bồn tắm gỗ product page | `[ASSUMPTION: no bồn tắm gỗ content/pricing/photos exist yet (PRD Open Question 9) — reasonable default: template must render correctly with placeholder-safe structure rather than assume real content by launch.]` Page must not silently publish with a raw broken-image icon or literal "Lorem ipsum" — either a clearly-labeled "coming soon" content state, or the page stays unpublished until CMS content lands; either is acceptable, but "renders broken" is not. |
| Drum-config-reference not yet built | Trống subpages | `[ASSUMPTION: FR-11 sequencing against the Tết 2027 deadline is unresolved (PRD Open Question 4) — reasonable default: specify the component's behavior but do not guarantee it ships at launch.]` Fallback state is not a broken link: if FR-11 has not shipped, the subpage simply omits the component and routes straight to `quote-request-form`/Zalo/phone, exactly as it does today by phone. |
| Out-of-nav landing page reached directly | Landing page (paid ads) | No nav to "escape" to — by design (FR-12: excluded from primary navigation). A visitor wanting the rest of the site must ask via the CTA's contact channel — there is no site navigation to fall back on. |
| Offline / no network | Any page | No offline-specific handling specified — this is a marketing/catalog site, not an app with local state to preserve. Standard browser offline behavior applies. |
| Product-detail page, single photo only | `product-detail-page` | Same graceful-degrade principle as `craftsman-story-block`: renders correctly with just the one photo — no empty gallery-arrow chrome, no grey placeholder tiles standing in for missing images. |
| Article detail, no related posts | `article-detail-page` | If no related posts are tagged/available, the related-posts module is omitted entirely rather than rendered empty, and the page falls back to the plain "← Quay lại Blog" back-to-listing link — same hide-rather-than-show-empty principle as the Empty category state above. |

## Interaction Primitives

- Tap/click anywhere on a `product-card` body opens the product; the CTA button is a larger, separate, equally valid tap target for the same action — never the only way in on a dense mobile grid.
- Pressed/active card feedback (inset ring + scale-down, `{rounded.md}` corners preserved) fires on touch-down for every tappable card — this audience needs visible confirmation that a tap registered, not just a navigation after the fact.
- Click-to-call (`tel:`) and Zalo deep-link are real, functional links on every page via `sticky-contact-bar` — never JavaScript-only handlers that fail silently without a phone/Zalo app present; both must have a sensible web fallback.
- No hover-only affordances anywhere — the audience is phone/tablet-first; anything revealed on hover on desktop must also be reachable by a direct tap on touch devices.
- No carousels used for primary navigation or as the main way to reach a product — the `craftsman-story-block` photo carousel is the one exception, and it is supplementary content, not a navigation device.
- Tap targets sized for an older audience: buttons and CTA rows should read comfortably at typical phone viewing distance — this is the same readability priority that drove the `price`/`phone-number`/`cta` type-role sizing in `DESIGN.md`.
- **Banned:** cart/checkout flows anywhere (site-wide non-goal), account/login UI of any kind (the site has no accounts — a Stitch draft added an account icon to the Blog nav on its own initiative; it must not ship, since it implies functionality that doesn't exist), auto-advancing/auto-rotating hero carousels, infinite scroll on category grids (use pagination or "load more" if a category ever grows large enough to need it), modal-on-modal stacking, vanity social-proof counters (like counts, view counts) on blog posts — reads as "internet content site," at odds with the calm/trust register (see Voice and Tone); a Stitch draft added one and it should not ship without an explicit client decision.

## Accessibility Floor

Behavioral; visual contrast values live in `DESIGN.md`.

- `[ASSUMPTION: exact contrast ratio target — WCAG 2.1 AA is assumed as the floor site-wide. This is a standard reasonable default given the explicit readability priority for an older audience, but it was not directly elicited or confirmed with the client in this session — flagged for triage.]`
- Every `sticky-contact-bar` segment and every `product-card`/`category-tile` CTA has an accessible label describing its destination (e.g. "Gọi ngay tới Đọi Tam," not just "Gọi ngay" with no context for a screen-reader user landing mid-page).
- All product/category photography carries descriptive alt text — this matters more than usual here because the craftsman-story and product pages are photo-led with comparatively little body text.
- If video ships on the craftsman-story page, it carries captions/subtitles at minimum (FR-10 lists video as should-have and photo as the guaranteed baseline; captions are a condition of shipping video at all, not a follow-up).
- `quote-request-form` fields are individually labeled (not placeholder-text-only labels that disappear on focus) and errors are announced in text adjacent to the field, not conveyed by color alone.
- Focus order on every page follows visual reading order; the `sticky-contact-bar` is reachable by keyboard/assistive tech without being trapped ahead of primary page content.
- Tap targets meet a minimum comfortable size given the stated older/less-web-savvy audience — treated as a design constraint here, with the numeric target (e.g. 44×44px equivalent) left to Architecture/implementation to formalize against the chosen platform's conventions.

## Key Flows

### Flow 1 — UJ-3: Bác Thành commissions a ceremonial drum from the Đọi Tam workshop

1. Bác Thành, thủ từ of a village đình, arrives via search or word-of-mouth referral — lands on the homepage or directly on **Trống — Chùa** (the subpage matching his search term).
2. He browses `product-card` entries on the Trống — Chùa page — designs, materials, "Liên hệ báo giá" pricing (drums stay contact-for-quote site-wide) — then taps into a specific model's **`product-detail-page`** (e.g. "Trống chùa loại 2") to see that model's own photos and spec rows (drawn from `drum-config-reference`) — a concrete, specific product page rather than stopping at a generic subcategory listing.
3. From the model's detail page, he taps into the **Câu chuyện nghệ nhân** page, linked from there — sees the craftsman-story-block: Phạm Trí Trong, the workshop, photos (and video, if available) — the trust signal that substitutes for an in-person workshop visit when he can't make one himself (PRD UJ-3 edge case).
4. Reassured, he taps the Zalo segment of the `sticky-contact-bar` (his preferred channel for a conversation this detailed) rather than filling out the form.
5. Off-platform: he specifies configuration by phone/Zalo — size, loại 1/2/3, bánh xe, sơn, vẽ mặt trống — either from memory of the page's `drum-config-reference` table (if shipped) or by simply describing what he needs, same as today.
6. **Climax:** he receives a quote matched to his exact configuration, negotiated by phone/Zalo — the page's job (build trust, surface the craftsman story, make contact effortless) is done; closing the sale happens off-platform by design.
7. **Resolution:** delivery date agreed; drum delivered.

Failure path: if he can't find the specific drum type he wants from the Trống hub, the hub's link to all 5 subpages (plus their individual SEO placement, so search may land him directly on the right one) is the recovery path — he is never required to filter a single generic page.

### Flow 2 — UJ-4: Chị Hương buys a decorative wine barrel as a Tết gift via paid ads

1. Chị Hương sees a paid ad for the wine-barrel line and clicks through — lands on the **Landing page — Thùng rượu** (paid-ads template), not the homepage and not the organic Thùng rượu gỗ product page. No nav, no distraction from the single conversion goal.
2. She sees each variant (gỗ sồi, ngựa kéo, 1 ngựa, 2 ngựa) as its own card with a real, visible `{typography.price}` — the pricing transparency that (per PRD FR-13/UJ-4) removes the main ad-funnel friction point that a phone-only funnel would create.
3. She taps "Đặt mua ngay" on the `landing-page-cta` for her chosen variant.
4. This opens the inline `quote-request-form` (or routes to Zalo/phone, her choice) on the same page — no redirect away from the page she trusted enough to act on.
5. **Climax:** her request is captured (inline success state, no page reload) — pricing transparency got her to act on an ad instead of bouncing off a phone-only wall.
6. **Resolution:** order and payment finalized off-site by phone/Zalo, in time for Tết delivery (no cart/online payment exists anywhere — explicit non-goal).

Failure path: if she abandons before submitting, there is no cart-recovery mechanism (explicit PRD non-goal, UJ-4 edge case) — the landing page's persistent, single CTA is the only re-engagement surface; no exit-intent popup or retargeting behavior is specified in this spine.

## Open Items for UX

Not elicited from the client in this session; full reasoning stays at each citation, this is a scan index for triage.

| Open item | Sections | Question |
|---|---|---|
| Contrast ratio target | Accessibility Floor | WCAG 2.1 AA assumed as the site-wide floor — confirm with client. |
| Bồn tắm gỗ pre-content rendering | State Patterns | No product content/pricing/photos exist yet (PRD Open Question 9) — confirm placeholder form (labeled placeholder vs. staying unpublished). |
| Craftsman-story video availability | State Patterns, Component Patterns | Unconfirmed (PRD Open Question 3) — page must degrade gracefully photo-only until resolved. |
| Drum-config-reference (FR-11) sequencing | State Patterns, Component Patterns | Unresolved against the Tết 2027 deadline (PRD Open Question 4) — behavior specified, launch inclusion not guaranteed. |
| Voice and Tone word-level register | Voice and Tone | Inferred from session decisions, not a client-confirmed style guide. Actual content authoring happens later in Piranha CMS by the client, not as part of this UX deliverable. |
| Trống model/loại count & naming per subcategory | Information Architecture, Component Patterns (`product-detail-page`) | Not enumerated anywhere (PRD Open Question 1) — an illustrative placeholder (e.g. loại 1/2/3 per subcategory) was used to specify the `product-detail-page` pattern; confirm the real SKU list per subcategory with the client before CMS content entry. |
