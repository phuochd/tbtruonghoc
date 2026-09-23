---
name: Mộc Trầm (trongdoitam.net)
description: Heritage-woodcraft brand system for trongdoitam.net — Đọi Tam drums, decorative wine barrels, and wooden bathtubs. Warm-wood palette, dense catalog-forward layout, one humanist sans typeface, sized for an audience that skews older and less web-savvy. Pre-Stitch spec — see Brand & Style for the delivery-process note.
status: final
sources:
  - "{planning_artifacts}/briefs/brief-tbtruonghoc-2026-09-17/brief.md"
  - "{planning_artifacts}/briefs/brief-tbtruonghoc-2026-09-17/addendum.md"
  - "{planning_artifacts}/prds/prd-tbtruonghoc-2026-09-17/prd.md"
created: 2026-09-17
updated: 2026-09-18
colors:
  background: '#F5EFE6'
  surface: '#FFFFFF'
  surface-sunken: '#EFE7D8'
  on-surface: '#3A2E22'
  on-surface-muted: '#7A6A56'
  primary: '#8B5A2B'
  on-primary: '#FFFFFF'
  secondary: '#A97142'
  on-secondary: '#FFFFFF'
  border: '#DCCFB8'
  border-subtle: '#EAE1CD'
  error: '#A63B2E'
  on-error: '#FFFFFF'
typography:
  display:
    fontFamily: 'Be Vietnam Pro'
    fontSize: 28px
    fontWeight: '700'
    lineHeight: '1.25'
  heading-lg:
    fontFamily: 'Be Vietnam Pro'
    fontSize: 24px
    fontWeight: '700'
    lineHeight: '1.3'
  heading-md:
    fontFamily: 'Be Vietnam Pro'
    fontSize: 20px
    fontWeight: '700'
    lineHeight: '1.35'
  heading-sm:
    fontFamily: 'Be Vietnam Pro'
    fontSize: 17px
    fontWeight: '600'
    lineHeight: '1.4'
  body:
    fontFamily: 'Be Vietnam Pro'
    fontSize: 16px
    fontWeight: '400'
    lineHeight: '1.6'
  body-sm:
    fontFamily: 'Be Vietnam Pro'
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.5'
  caption:
    fontFamily: 'Be Vietnam Pro'
    fontSize: 13px
    fontWeight: '400'
    lineHeight: '1.4'
  label:
    fontFamily: 'Be Vietnam Pro'
    fontSize: 12px
    fontWeight: '600'
    lineHeight: '1.4'
    letterSpacing: 0.04em
  price:
    fontFamily: 'Be Vietnam Pro'
    fontSize: 18px
    fontWeight: '700'
    lineHeight: '1.3'
  phone-number:
    fontFamily: 'Be Vietnam Pro'
    fontSize: 20px
    fontWeight: '700'
    lineHeight: '1.3'
  cta:
    fontFamily: 'Be Vietnam Pro'
    fontSize: 16px
    fontWeight: '600'
    lineHeight: '1.2'
rounded:
  sm: 6px
  DEFAULT: 10px
  md: 10px
  lg: 14px
  full: 9999px
spacing:
  '1': 4px
  '2': 8px
  '3': 12px
  '4': 16px
  '5': 24px
  '6': 32px
  '8': 48px
  gutter: 16px
  gutter-desktop: 24px
  card-gap: 12px
  section-gap: 32px
components:
  nav:
    background: '{colors.surface}'
    border-bottom: '{colors.border}'
    brand-text: '{colors.on-surface}'
    brand-accent: '{colors.secondary}'
    height: 56px
    hotline-typography: '{typography.phone-number}'
    hotline-color: '{colors.primary}'
  trust-block:
    background: '{colors.surface-sunken}'
    border: '{colors.border}'
    radius: '{rounded.md}'
    quote-typography: '{typography.body}'
    attribution-typography: '{typography.caption}'
    attribution-color: '{colors.on-surface-muted}'
  sticky-contact-bar:
    background: '{colors.surface}'
    border-top: '{colors.border}'
    shadow: '0 -4px 14px rgba(58,46,34,0.10)'
    segment-text: '{typography.body-sm}'
    zalo-background: '{colors.primary}'
    zalo-text: '{colors.on-primary}'
    icon-size: 20px
  category-tile:
    background: '{colors.surface}'
    border: '{colors.border}'
    radius: '{rounded.md}'
    label: '{typography.label}'
    label-color: '{colors.secondary}'
    title: '{typography.heading-sm}'
    layout: 'flexible-count grid, CMS-driven — never hardcoded to 3'
  product-card:
    background: '{colors.surface}'
    border: '{colors.border}'
    radius: '{rounded.md}'
    category-label: '{typography.label}'
    category-label-color: '{colors.secondary}'
    title: '{typography.heading-sm}'
    description: '{typography.body-sm}'
    description-color: '{colors.on-surface-muted}'
    price: '{typography.price}'
    price-contact-color: '{colors.on-surface-muted}'
    cta-background: '{colors.primary}'
    cta-text: '{colors.on-primary}'
    cta-typography: '{typography.cta}'
    cta-radius: '{rounded.sm}'
    active-ring: '{colors.primary}'
    active-scale: '0.98'
  landing-page-cta:
    background: '{colors.primary}'
    text: '{colors.on-primary}'
    typography: '{typography.cta}'
    radius: '{rounded.sm}'
    price-typography: '{typography.price}'
    variant-label: '{typography.label}'
  quote-request-form:
    background: '{colors.surface}'
    border: '{colors.border}'
    radius: '{rounded.md}'
    label: '{typography.body-sm}'
    input-border: '{colors.border}'
    input-border-focus: '{colors.primary}'
    submit-background: '{colors.primary}'
    submit-text: '{colors.on-primary}'
    error-text: '{colors.error}'
  craftsman-story-block:
    background: '{colors.surface}'
    radius: '{rounded.lg}'
    heading: '{typography.heading-lg}'
    caption: '{typography.caption}'
    caption-color: '{colors.on-surface-muted}'
  drum-config-reference:
    background: '{colors.surface-sunken}'
    border: '{colors.border}'
    radius: '{rounded.md}'
    row-typography: '{typography.body-sm}'
    cta-background: '{colors.primary}'
    cta-text: '{colors.on-primary}'
  product-detail-page:
    background: '{colors.background}'
    gallery-background: '{colors.surface}'
    gallery-border: '{colors.border}'
    gallery-radius: '{rounded.md}'
    title: '{typography.heading-lg}'
    sku-code-typography: '{typography.caption}'
    sku-code-color: '{colors.on-surface-muted}'
    category-label: '{typography.label}'
    category-label-color: '{colors.secondary}'
    spec-background: '{colors.surface-sunken}'
    spec-border: '{colors.border}'
    spec-row-typography: '{typography.body-sm}'
    price: '{typography.price}'
    price-contact-color: '{colors.on-surface-muted}'
    cta-background: '{colors.primary}'
    cta-text: '{colors.on-primary}'
    cta-radius: '{rounded.sm}'
  article-detail-page:
    background: '{colors.background}'
    title: '{typography.heading-lg}'
    meta-typography: '{typography.caption}'
    meta-color: '{colors.on-surface-muted}'
    body-typography: '{typography.body}'
    subhead-typography: '{typography.heading-sm}'
    related-label: '{typography.label}'
    related-label-color: '{colors.secondary}'
    back-link-color: '{colors.primary}'
---

## Brand & Style

trongdoitam.net sells trust before it sells anything else. The workshop's real edge isn't price — it's that the buyer can meet the craftsman, or at least see and believe in him: Phạm Trí Trong, guild chairman of làng nghề Đọi Tam, personally quality-controls every piece. The visual system exists to carry that weight without shouting it. "A little classical, because this is a heritage craft village — but the colors stay simple, not flashy" was the brand instruction, and it remained the throughline across every subsequent decision: the picked palette (Mộc Trầm, "warm and solid like jackfruit wood gone glossy with age") over four louder alternatives, and the picked homepage register (Danh Mục Nhanh — dense, catalog-forward) over a more spacious storytelling-first alternative, because for this business the product and its price have to be visible fast, not discovered after a scroll.

The resulting posture is **Warm Workshop Catalog**: dense enough that a visitor sees product, category, and price within one screen (the register the user explicitly chose), but warmed and slowed down at the type level so that an older, less web-savvy audience — thủ từ đình chùa, temple and communal-house caretakers, Tết gift buyers — isn't punished for the density. Read Colors and Typography together: the palette supplies the "cổ điển nhưng đơn giản" heritage feel, the type scale supplies the readability correction the user asked for on top of it. Where the two pull in different directions, readability wins — the user was explicit that the audience's comfort outranks matching the original mockup's tight type scale exactly.

**Delivery-process note:** produced before any Google Stitch pass; this pair is the source-of-truth spec Stitch output must be checked against — full delivery-process explanation in EXPERIENCE.md Foundation.

## Colors

The palette is **Mộc Trầm** — locked from a field of five candidates, chosen for reading as "gỗ mít lên nước": aged, warm, honest wood, not a decorated or lacquered surface.

→ Visual reference: [mockups/color-themes.html](mockups/color-themes.html) — all five candidate palettes side by side (Mộc Trầm is variation #1); shows what was rejected and why the others read louder/colder than this one.

- **Background (`#F5EFE6`)** is the page canvas everywhere — a warm off-white with a visible tan cast, never a clinical white. It is what makes the site feel like a wood-shop floor rather than a generic e-commerce template.
- **Surface (`#FFFFFF`)** is reserved for content that needs to read as distinct and "placed on" the background: cards, the nav bar, forms, the sticky contact bar. The contrast between `background` and `surface` is intentionally subtle — this is tonal layering, not a hard card-on-page effect.
- **Surface-sunken (`#EFE7D8`)** *(invented — not one of the five picked-palette tokens; extrapolated one step darker than `background` for the rare case a component needs to read as recessed rather than raised, e.g. the drum-config-reference table body)*. Use sparingly; most of the site should read as background-and-surface only.
- **On-surface (`#3A2E22`)** is the primary text color — a near-black warmed by brown, never a true `#000`. Used on both `background` and `surface`.
- **On-surface-muted (`#7A6A56`)** is for secondary text: card descriptions, captions, helper copy. It must never be the only color carrying essential information (price, phone number, CTA label always use `on-surface` or `on-primary`, never muted).
- **Primary (`#8B5A2B`)** is the workshop's own wood-brown — the color of doing something (buttons, active states, the Zalo segment of the sticky bar, links). It appears nowhere decoratively; if it's on screen, it's inviting a tap.
- **Secondary (`#A97142`)** is a lighter amber used for category eyebrows, the brand-name accent in the nav ("Trống **Đọi Tam**"), and card-thumbnail gradients. It signals "this is a label/category," never "this is an action" — that distinction (primary = act, secondary = label) is load-bearing and should not be blurred.
- **Border (`#DCCFB8`)** is the structural hairline: card outlines, nav underline, sticky-bar dividers.
- **Border-subtle (`#EAE1CD`)** *(invented — a lighter hairline for internal dividers inside a component, e.g. between a form's fields, where the structural `border` would read too heavy)*.
- **Error (`#A63B2E`)** *(invented — no error color existed in the picked palette; the five candidate palettes never specified one. This is a warm brick-red kept in the same family as Mộc Trầm rather than importing a cold Material-style red, so a form validation message doesn't visually clash with the rest of the page)*. Used only for form validation text and required-field indicators (`quote-request-form`) — never for decoration, never for "out of stock," since the catalog has no stock concept.

Avoid: saturated reds/oranges as accents (that register belongs to the rejected "Đất Nung Cổ" alternative, which was deliberately not picked), gradients used decoratively (the one gradient in the system — `secondary`→`primary` on card thumbnails — is a deliberate wood-grain echo, not a general pattern to extend), pure black or pure white text.

## Typography

**One humanist sans typeface, throughout, at every role** — this was an explicit, deliberate call against a split serif-heading/sans-body treatment that would have read more "boutique editorial" and less "workshop you can trust and call." Recommended family: **Be Vietnam Pro** — a humanist sans purpose-built for Vietnamese diacritics (correct stroke weight and spacing under sắc/huyền/hỏi/ngã/nặng and the full Vietnamese Latin-Extended set, which many Western-market humanist sans faces render poorly), warmer in stroke than system Arial/Helvetica, and open enough at small sizes to survive the dense catalog layout without collapsing into mush. If Stitch iteration surfaces a stronger option, any humanist sans with equivalent Vietnamese OpenType support and a comparable x-height may substitute — the single-family-throughout rule is the fixed constraint, the specific face is a recommendation.

Two semantic roles exist specifically because of the readability correction the user asked for on top of the Danh Mục Nhanh mockup's original scale: **`price`** and **`phone-number`**. In the working mockup these were incidental (11–12.5px, folded into generic card text); here they are named, first-class type roles set deliberately large (`price` 18px/700, `phone-number` 20px/700) because they are the two pieces of information this audience — older, less web-savvy, often deciding by phone — must never have to squint at. `cta` (16px/600, up from the mockup's 10px button label) gets the same treatment: a tap target's label is not a place to save space.

The ramp: `display` (28px) for homepage/story-page hero headlines only; `heading-lg` (24px) for page H1s (category hub, product line, story page); `heading-md` (20px) for section headers within a page; `heading-sm` (17px) for card and subcategory titles; `body` (16px) for running prose; `body-sm` (14px) for card descriptions and form labels; `caption` (13px) for photo credits and fine print; `label` (12px, uppercase, tracked) for category eyebrows only, in `secondary`.

Line-height is generous throughout (1.25–1.6) rather than the tighter leading a denser catalog layout might default to — this is the same readability correction applied consistently rather than only to the two flagged roles.

## Layout & Spacing

Mobile-first, single-column. The scale is 4px-based (`spacing.1`–`spacing.8`); `gutter` (16px) is the mobile side margin used site-wide, matching the density the picked Danh Mục Nhanh direction called for — this is not an airy editorial margin, it is a working catalog margin. `gutter-desktop` (24px) widens it only on larger viewports; the layout never goes full-bleed edge-to-edge.

The homepage and category pages use a 2-column card grid on mobile (`card-gap` 12px between cards) that can widen to 3–4 columns on tablet/desktop without changing card anatomy — this is what makes the `category-tile` and `product-card` components CMS-count-flexible rather than hardcoded: the grid reflows to however many items the CMS returns, it does not assume 3.

`section-gap` (32px) separates major page sections (hero → category grid → trust strip → quote/testimonial → footer/sticky-bar zone). This is deliberately tighter than an editorial site's section rhythm would be — Danh Mục Nhanh is a dense register, and generous 80px+ editorial gaps would fight that.

The **sticky-contact-bar** is fixed to the viewport bottom on every page, at every breakpoint, mobile and desktop alike — it is the one layout element that never scrolls away, because click-to-call/Zalo/Maps (FR-2) must be reachable from anywhere on the site without hunting for it.

The **landing-page-cta** template (FR-12/13, wine-barrel paid-ads page) breaks from the standard page shell: no nav, no sticky-contact-bar with three segments — see Components for its distinct single-CTA treatment. It still inherits `gutter`, the card grid conventions, and the full color/type system, so a visitor who clicks through from an ad still recognizes the same brand.

## Elevation & Depth

Depth is minimal and functional, not decorative — consistent with "cổ điển nhưng đơn giản" (classical but simple). Two shadow treatments exist, both derived from the picked mockup rather than invented:

- **Card rest state:** no shadow. Cards are separated from `background` by the `surface`/`background` tonal shift and a 1px `border` — not elevation.
- **Sticky-contact-bar:** `0 -4px 14px rgba(58,46,34,0.10)` — a soft upward shadow (tinted with `on-surface`, not black) that lifts the bar off the page content scrolling beneath it. This is the *only* place a shadow does real structural work, and it should stay that way; adding shadows elsewhere would undercut the flat, honest-wood-surface read.

No modal/dialog elevation system is specified — the site has no modal flows (forms are inline or full-page; see EXPERIENCE.md State Patterns).

## Shapes

Corners are softly rounded, never sharp and never pill-shaped for structural elements — sharp reads too industrial for a craft brand, full pill shapes read too "tech startup" for a heritage workshop. `rounded.sm` (6px) is used for buttons and small interactive chips; `rounded.md`/`DEFAULT` (10px) is the workhorse radius for cards, tiles, and form containers, taken directly from the picked mockup's card radius; `rounded.lg` (14px) is reserved for larger content blocks — the craftsman-story-block's photo/video container chief among them, where a slightly softer corner suits a larger, more contemplative piece of content. `rounded.full` exists only for the category-eyebrow chip and the Tết-2027/"Mới" product badges.

## Components

→ Visual reference: [mockups/homepage-direction.html](mockups/homepage-direction.html) — the picked "Danh Mục Nhanh" homepage register (locked Mộc Trầm palette applied), anchor for `category-tile`/`product-card` density and the pressed/active card state. Predates the readability correction (type scale here is smaller than the frontmatter tokens specify) — spine tokens win on that point.

- **`nav`** — Fixed-height top bar (56px), `surface` background, `border` bottom hairline. Brand wordmark "Trống **Đọi Tam**" with the second word in `secondary` — the one place `secondary` sits directly in a wordmark rather than a label. No mega-menu; the catalog's flatness (trống hub + 5 subpages, thùng rượu, bồn tắm) means nav can stay a simple link list plus a mobile hamburger. On tablet/desktop only, the nav bar also displays the hotline number (`phone-number` typography, `primary` color) — adopted from the first Stitch export, which added it on its own initiative; it directly serves an audience that defaults to calling. Mobile omits it (no room at 56px height); the `sticky-contact-bar`'s "Gọi ngay" segment covers mobile instead. No account/login icon anywhere in `nav`, on any breakpoint — the site has no accounts (no cart/checkout is a stated non-goal); a Stitch draft added one on the Blog screens only and it must not ship, since it implies functionality that doesn't exist.
- **`trust-block`** — A short craftsman testimonial/quote (attributed to Phạm Trí Trong), embeddable on the Trống subcategory pages and any `product-detail-page`, not just the dedicated craftsman-story page. `surface-sunken` background so it reads as a distinct, considered insert rather than another card, `rounded.md`. Adopted from the first Stitch export: reinforces the "trust before transaction" brand thesis at the exact moment a buyer is deciding, rather than only on the story page.
- **`sticky-contact-bar`** — Three equal segments (Gọi ngay / Chat Zalo / Bản đồ), `surface` background, `border` top hairline, the lift-shadow described under Elevation. The center Zalo segment is visually distinct — `primary` fill, `on-primary` text — because it is the preferred channel for configuration-heavy conversations (drum specs); the other two are plain text-on-surface. Present on every page except the landing-page-cta template, which uses a single full-width CTA instead (see below).
- **`category-tile`** *(the flexible-category-block)* — The homepage's product-category grid. Deliberately **not** a hardcoded 3-tile layout: it is a CMS-driven collection that renders however many category entries editors define, in a responsive grid (2-up mobile, wider on desktop). This exists because the catalog is expected to outgrow its current 3 lines (trống, thùng rượu gỗ, bồn tắm gỗ) per the brief's Vision section — the component must not need a code change to display a 4th or 5th line. Each tile: `surface` card, `label` eyebrow in `secondary`, `heading-sm` title, `border` outline, `rounded.md` corners.
- **`product-card`** — The catalog workhorse, used on the homepage grid, the Trống hub and its 5 subpages, and the Thùng rượu/Bồn tắm pages. Thumbnail area uses the `secondary`→`primary` gradient as an image placeholder/frame treatment. Category eyebrow (`label`, `secondary`), title (`heading-sm`), one-line description (`body-sm`, `on-surface-muted`), then a price row: either a real `price` (large, bold, `on-surface`) or, for contact-for-quote lines (drums, bathtubs), a `body-sm`/`on-surface-muted` "Liên hệ báo giá" — visually quieter than a real price so a visitor doesn't mistake "ask for pricing" for "no price exists." CTA button: `primary` fill, `cta` typography, `rounded.sm`. **Pressed/active state** (from the picked mockup, carried forward as intentional, not incidental): a 2px inset `primary` ring plus a 0.98 scale-down and a slightly darker/inset CTA — gives tactile confirmation on tap for an audience that may not trust that a tap "took."
- **`landing-page-cta`** — The wine-barrel paid-ads template's primary action. Full-width, `primary` background, `cta` typography at a larger touch target than the standard product-card CTA (this page has one job: convert ad traffic). Each variant (gỗ sồi, ngựa kéo, 1 ngựa, 2 ngựa) shows its own real `price` in `price` typography next to a `variant-label` chip — this is the only place on Site B where every line item is priced, per FR-13.
- **`quote-request-form`** — The general contact/quote form (FR-3), embeddable on any product/category page. `surface` card, `border` outline, `rounded.md`. Labels in `body-sm`, focus state swaps `border` for `primary` on the active field, submit button is `primary`/`on-primary`, validation errors in `error` text directly beneath the offending field (never a top-of-form-only error summary — this audience should not have to hunt for which field is wrong).
- **`craftsman-story-block`** — The heritage/story page's core content unit: a photo (required) or photo+video (should-have) block, `rounded.lg`, with a `caption` credit line beneath. Must render correctly with photo only — see EXPERIENCE.md State Patterns for the graceful-degradation rule this token pair supports.
- **`drum-config-reference`** — The should-have configuration table (FR-11: size, loại 1/2/3, bánh xe, sơn, vẽ mặt trống). Sits on `surface-sunken` to read as a distinct reference tool rather than another product card, `body-sm` rows, ending in the same `primary`/`on-primary` quote-request CTA pattern as everything else — this is a reference aid, not a configurator that computes a price; it always ends in a human quote request. Carries a small `caption`-typography disclaimer line beneath the table ("Bảng tham khảo, không tính giá tự động") — adopted from the first Stitch export, which added this on its own initiative; it makes the reference-not-calculator rule legible to the end user, not just internal spec.
- **`product-detail-page`** — *(added 2026-09-18, client-flagged gap — see EXPERIENCE.md Information Architecture)* The individual model/variant page a `product-card` links to: one specific trống loại, one thùng rượu gỗ variant, or the bồn tắm gỗ line's generic product page — each with its own photos, specs, URL, and SEO rather than everything flattened onto one category page. Photo gallery leads the page: `surface` frame, `rounded.md`, `border` outline; degrades to a single photo exactly like `craftsman-story-block`'s photo-only rule — no empty gallery arrows or placeholder tiles when only one image exists. Below it, a `label` category eyebrow (`secondary`) and the model/variant name (`heading-lg`). Spec rows reuse `drum-config-reference`'s row treatment (`surface-sunken` background, `body-sm` rows) filtered to just this one model, rather than a whole new spec-table component — a subset of that table's rows, not a duplicate of it. Price row follows the exact `product-card` rule: a real `price` only where a genuine price exists; otherwise the quieter `body-sm`/`on-surface-muted` "Liên hệ báo giá." Drum and bathtub detail pages, and the *organic* wine-barrel variant detail pages, all stay contact-for-quote — only the separate `landing-page-cta` template shows real per-variant pricing (FR-13); this page and that template are distinct, not the same surface. Ends in an embedded `quote-request-form`, the same terminal CTA pattern as `drum-config-reference`.
- **`article-detail-page`** — *(added 2026-09-18, client-flagged gap)* The individual blog/content article template — distinct from the Blog listing page, which only lists entries. `heading-lg` title, optional `caption`/`on-surface-muted` byline-and-date metadata line, `body` running copy with `heading-md`/`heading-sm` in-article subheads where an article is long enough to need them. Ends in a small related-posts module (reusing the `product-card` grid convention at a smaller scale) or, when no related posts exist, a plain "← Quay lại Blog" back-to-listing link — the page never dead-ends.

## Do's and Don'ts

| Do | Don't |
|---|---|
| One humanist sans family (Be Vietnam Pro or equivalent) for every text role | Mix a serif display face into headings — that read was explicitly rejected |
| Oversized `price`, `phone-number`, and `cta` roles — bigger than the original mockup's scale | Copy the mockup's original 10–13px card/CTA sizes uncritically; the user asked for larger |
| CMS-driven, flexible-count grids for `category-tile` and `product-card` collections | Hardcode a 3-tile homepage category block — the catalog will outgrow it |
| `primary` for anything actionable; `secondary` for anything that's just a label | Use `primary` and `secondary` interchangeably — the act/label distinction is load-bearing |
| Keep the sticky-contact-bar present and fixed on every organic page | Hide phone/Zalo/Maps behind a menu or below the fold — this audience calls, doesn't browse deep |
| Let the landing-page-cta template drop nav and the 3-segment sticky bar for one strong CTA | Reuse the standard page shell for paid-ads landing pages — it dilutes the single conversion goal |
| Show real per-item pricing only on the wine-barrel landing page | Add visible pricing to drum or bathtub product pages — those stay contact-for-quote by design |
| Use the pressed/active card state (inset ring + scale) to confirm taps | Add hover-only affordances anywhere — this audience is phone/tablet-first, not mouse-first |
