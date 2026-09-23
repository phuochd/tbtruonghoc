---
name: Xanh Lục Bảo Rạng Rỡ — Banner Đậm (tbtruonghoc.com)
description: Institutional school-equipment brand system for tbtruonghoc.com (Site A) — a brightened teal-and-amber palette with a solid-teal banner treatment for nav/hero, a spacious confident-brand desktop-first layout, and Mulish as the brand typeface, built for B2B/institutional procurement buyers (schools, preschools, playgrounds/parks). Pre-Stitch spec — see Brand & Style for the delivery-process note.
status: final
sources:
  - "{planning_artifacts}/briefs/brief-tbtruonghoc-2026-09-17/brief.md"
  - "{planning_artifacts}/briefs/brief-tbtruonghoc-2026-09-17/addendum.md"
  - "{planning_artifacts}/prds/prd-tbtruonghoc-2026-09-17/prd.md"
created: 2026-09-18
updated: 2026-09-18
colors:
  bg: '#FCFEFD'
  surface: '#FFFFFF'
  primary: '#0E7A6C'
  text: '#123832'
  text-muted: '#4F6E68'
  border: '#D3EEE7'
  border-neutral: '#E3ECE9'
  banner-bg: '#0E7A6C'
  banner-text: '#FFFFFF'
  banner-link: '#CDEDE6'
  banner-subtext: '#D8F3EC'
  banner-outline: '#6FBFAE'
  cta: '#E3A63B'
  cta-text: '#2B1B02'
  accent-soft: '#FCEDD1'
  accent-ink: '#8A5F14'
  trust-bg: '#DFF6EE'
  trust-border: '#B7E7D9'
  trust-ink: '#0E7A6C'
  on-primary: '#E7FBF5'
  surface-tint: '#EFF8F6'
  border-tint: '#BFE3DA'
  warning-bg: '#FFF7E8'
  warning-border: '#F0D7A0'
  warning-ink: '#6B4E10'
  error: '#C0511F'
typography:
  # Mulish (Google Fonts, weights 400/600/700/800, Vietnamese subset) — picked
  # by Phước over Inter, Plus Jakarta Sans, and IBM Plex Sans; see Typography
  # section below and mockups/typography-1.html for the comparison and rationale.
  display:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 34px
    fontWeight: '800'
    lineHeight: '1.22'
  heading-lg:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 25px
    fontWeight: '800'
    lineHeight: '1.3'
  heading-md:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 19px
    fontWeight: '700'
    lineHeight: '1.3'
  heading-sm:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 17px
    fontWeight: '800'
    lineHeight: '1.3'
  heading-xs:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 15px
    fontWeight: '800'
    lineHeight: '1.3'
  body:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 13.5px
    fontWeight: '400'
    lineHeight: '1.6'
  body-sm:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 12.5px
    fontWeight: '400'
    lineHeight: '1.55'
  caption:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 11px
    fontWeight: '600'
    lineHeight: '1.4'
  label:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 11px
    fontWeight: '700'
    lineHeight: '1.3'
    letterSpacing: 0.05em
  price:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 24px
    fontWeight: '800'
    lineHeight: '1.2'
  price-fallback:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 20px
    fontWeight: '800'
    lineHeight: '1.2'
  cta:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 14px
    fontWeight: '700'
    lineHeight: '1.2'
  nav-brand:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 17px
    fontWeight: '800'
    lineHeight: '1.2'
    letterSpacing: 0.02em
  nav-link:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 12.5px
    fontWeight: '600'
    lineHeight: '1.2'
  stat-number:
    fontFamily: 'Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: 18px
    fontWeight: '800'
    lineHeight: '1.2'
rounded:
  sm: 6px
  md: 8px
  lg: 10px
  xl: 12px
  full: 9999px
spacing:
  '1': 4px
  '2': 8px
  '3': 12px
  '4': 16px
  '5': 20px
  '6': 24px
  '8': 32px
  gutter: 40px
  content-max-width: 1200px
  section-gap: 48px
  card-gap: 20px
  field-gap: 14px
components:
  nav:
    background: '{colors.banner-bg}'
    text: '{colors.banner-text}'
    link-text: '{colors.banner-link}'
    logo-typography: '{typography.nav-brand}'
    link-typography: '{typography.nav-link}'
    padding: '16px {spacing.6}'
  contact-chip:
    background: 'rgba(255,255,255,0.16)'
    border: 'rgba(255,255,255,0.3)'
    text: '{colors.banner-text}'
    typography: '{typography.caption}'
    radius: '{rounded.full}'
  hero-carousel:
    overlay: 'linear-gradient(180deg, rgba(14,122,108,.05) 0%, rgba(14,122,108,.10) 45%, rgba(14,122,108,.72) 100%)'
    heading: '{typography.display}'
    heading-color: '{colors.banner-text}'
    subtext-color: '{colors.banner-subtext}'
    eyebrow-border: '{colors.banner-outline}'
  hero-fallback:
    background: '{colors.banner-bg}'
    heading: '{typography.display}'
    heading-color: '{colors.banner-text}'
    subtext-color: '{colors.banner-subtext}'
  category-tile:
    background: '{colors.bg}'
    border: '{colors.border-neutral}'
    radius: '{rounded.xl}'
    icon-background: '{colors.primary}'
    icon-radius: '{rounded.md}'
    label: '{typography.body}'
  nav-dropdown:
    background: '{colors.surface}'
    border: '{colors.border-neutral}'
    radius: '{rounded.lg}'
    shadow: '0 14px 30px rgba(0,0,0,.18)'
    item-radius: '{rounded.sm}'
    item-hover-background: '{colors.surface-tint}'
    viewall-color: '{colors.primary}'
  trust-badge:
    background: '{colors.trust-bg}'
    border: '{colors.trust-border}'
    text: '{colors.trust-ink}'
    radius: '{rounded.full}'
    typography: '{typography.caption}'
  trust-stat:
    number-typography: '{typography.stat-number}'
    number-color: '{colors.primary}'
    label-typography: '{typography.caption}'
    label-color: '{colors.text-muted}'
  price-block:
    background: '{colors.surface-tint}'
    border: '{colors.border-tint}'
    radius: '{rounded.lg}'
    label-typography: '{typography.label}'
    price-present-typography: '{typography.price}'
    price-absent-typography: '{typography.price-fallback}'
    price-color: '{colors.primary}'
    note-typography: '{typography.body-sm}'
  service-area:
    background: '{colors.bg}'
    border: '{colors.border-neutral}'
    radius: '{rounded.md}'
    heading-typography: '{typography.heading-xs}'
    heading-color: '{colors.primary}'
    warning-note-background: '{colors.warning-bg}'
    warning-note-border: '{colors.warning-border}'
    warning-note-text: '{colors.warning-ink}'
  process-strip:
    background: '{colors.surface-tint}'
    border: '{colors.border-tint}'
    radius: '{rounded.xl}'
    step-number-background: '{colors.primary}'
    step-number-text: '{colors.banner-text}'
    arrow-color: '{colors.banner-outline}'
  survey-form-modal:
    background: '{colors.bg}'
    radius: '{rounded.xl}'
    shadow: '0 20px 50px rgba(0,0,0,.35)'
    backdrop: 'rgba(18,56,50,.62)'
    field-border: '{colors.border-tint}'
    linked-field-background: '{colors.surface-tint}'
    linked-field-text: '{colors.primary}'
    required-marker: '{colors.error}'
    submit-background: '{colors.cta}'
    submit-text: '{colors.cta-text}'
  warning-banner:
    background: '{colors.warning-bg}'
    border: '{colors.warning-border}'
    text: '{colors.warning-ink}'
    icon-background: '{colors.cta}'
    icon-text: '{colors.cta-text}'
  cta-buttons:
    primary-background: '{colors.cta}'
    primary-text: '{colors.cta-text}'
    primary-radius: '{rounded.sm}'
    primary-typography: '{typography.cta}'
    secondary-border: '{colors.primary}'
    secondary-text: '{colors.primary}'
    secondary-radius: '{rounded.sm}'
    secondary-typography: '{typography.cta}'
  footer-strip:
    background: '{colors.primary}'
    text: '{colors.on-primary}'
    brand-text: '{colors.banner-text}'
---

> This DESIGN.md and its paired EXPERIENCE.md win on conflict with any `.working/` mock, wireframe, or import referenced below — those files are exploratory and superseded rounds where they disagree with what's written here.

## Brand & Style

tbtruonghoc.com sells to people who are spending someone else's budget — school principals, preschool directors, procurement staff — on equipment their institution will live with for years. Phước's brand direction for Site A, stated plainly in discovery, was **"chuyên nghiệp, đáng tin cậy"** (professional, trustworthy). Unlike Site B (trongdoitam.net), there was no pre-existing craft story or named artisan to lean on here — the trust has to come from the site itself reading like it belongs to a capable, established institutional supplier, not from provenance.

That instruction ran head-first into two anti-patterns the discovery process explicitly ruled out. First, the **legacy tbtruonghoc.com site** itself: white background, generic sans-serif hierarchy, and red "Giảm giá %" discount badges — a promo-driven retail-marketplace register that undercuts institutional trust rather than building it (see `imports/legacy-site-tbtruonghoc-com.md`). Second, **Site B's warm-wood/heritage register** (Mộc Trầm: aged jackfruit-wood browns, a single humanist sans, catalog-dense layout) — a deliberate sibling-brand distinction, not a shared system. Site A and Site B share an owner and a CMS, not a palette or a voice; a visitor should never mistake one site for the other.

Five color directions were explored before landing on a **teal + amber** register (`.working/color-themes-1.html`), picked over warmer/browner and colder/grayer alternatives specifically because teal reads clinical-but-approachable in an institutional-blue family without being cold, and amber gives it one confident, warm action color rather than a second cool tone competing for attention. Phước's own feedback pushed it further: the initial "Xanh Lục Bảo Định Chế" (institutional teal) needed to be **brighter and fresher** ("tươi sáng") to suit a school-age-adjacent audience rather than reading corporate-somber — this produced the brightened "Xanh Lục Bảo Rạng Rỡ" pass (`.working/color-themes-2.html`), then converged with a solid-teal "banner" treatment for the nav/hero band (`mockups/color-themes-3.html`) — the final, locked palette this document specifies.

The resulting posture is **Confident Institutional Brand**: a large, photo-led hero that leads with the company's scale and competence before it leads with a product list (the picked homepage register, "Thương Hiệu Rộng Rãi" — spacious, low-density, brand-forward), a flat and simple nav that never resorts to a mega-menu even across 11 product categories, and a persistent, unhidden path to a phone call or Zalo chat on every page. This is a desktop-first, B2B procurement surface — confident and roomy where Site B is dense and catalog-forward, and where the legacy site was promo-cluttered.

**Delivery-process note:** produced pre-Stitch — see EXPERIENCE.md Foundation for the full delivery-process fact.

## Colors

The palette is **Xanh Lục Bảo Rạng Rỡ — Banner Đậm** ("Radiant Emerald — Bold Banner"), locked in `mockups/color-themes-3.html` after three rounds of refinement.

→ Visual reference: [mockups/color-themes-3.html](mockups/color-themes-3.html) — the converged, locked palette, with the full round-1 → round-2 → round-3 lineage documented in its header comment.

- **Background (`#FCFEFD`)** is the page canvas — a near-white with a faint cool cast, not a clinical pure white. Used for the body of every page outside the nav/hero banner.
- **Surface (`#FFFFFF`)** is true white, reserved for cards and panels that need to read as placed on the background (product cards, the nav-dropdown panel, the survey-form modal).
- **Primary (`#0E7A6C`)** is the brand teal — the color of the nav bar, the hero banner, the footer strip, category icon dots, and every "this is Ngọc Anh" moment. It is also `banner-bg`: nav and hero deliberately share the exact same hex so that, in the no-photo hero state, nav and hero read as one continuous teal band rather than two stacked elements.
- **Text (`#123832`)** is the primary body-text color — a deep teal-black, never pure `#000`, keeping every page in the same color family even at maximum contrast.
- **Text-muted (`#4F6E68`)** is for secondary copy: descriptions, captions, helper notes. As on Site B, it must never be the only carrier of essential information (price, CTA label, phone number always use `text` or a stronger role).
- **Border (`#D3EEE7`) / Border-neutral (`#E3ECE9`)** are two closely related hairlines — reach for `border-neutral` by default on new card/image components, since it's what the final homepage and all three key screens actually use. `border` is the cooler, slightly more saturated line used within the locked-palette reference itself (card/nav-hairline contexts); `border-neutral` is the desaturated neutral hairline the homepage and PDP rounds settled into for card and image edges. Both are legitimate, sourced values.
- **Banner-text (`#FFFFFF`) / Banner-link (`#CDEDE6`) / Banner-subtext (`#D8F3EC`) / Banner-outline (`#6FBFAE`)** are the on-teal type/outline family used exclusively inside the nav and hero banner: full white for the wordmark and heading, a softened mint for secondary nav links and hero body copy, and a mid-teal outline for the hero's secondary (outline) button border and the process-strip's arrow glyphs.
- **CTA (`#E3A63B`) / CTA-text (`#2B1B02`)** is the single amber action color — every primary button on the site (Nhận báo giá, Đăng ký khảo sát miễn phí, Yêu cầu báo giá, Gửi đăng ký, Đặt mua ngay) uses this fill. It appears nowhere decoratively.
- **Accent-soft (`#FCEDD1`) / Accent-ink (`#8A5F14`)** is an amber-tinted chip pair, documented in the locked palette for future amber-family badge/tag use (e.g. a "Mới" or promotional flag on a category), not yet applied in any rendered mock.
- **Trust-bg (`#DFF6EE`) / Trust-border (`#B7E7D9`) / Trust-ink (`#0E7A6C`)** is the small pill-chip treatment for short trust claims ("Bảo hành chính hãng"), as locked in `mockups/color-themes-3.html` and reused for the PDP's `mini-trust` chips.
- **Surface-tint (`#EFF8F6`) / Border-tint (`#BFE3DA`)** is a second, very slightly cooler teal-tint that the palette grew into once applied to real page layouts: it's what the final picked homepage's trust-band, and all three key screens' price-block, service-area, process-strip, and shipping-note surfaces, actually render. Both tint pairs are real and current — `trust-bg`/`trust-border` for small pill badges, `surface-tint`/`border-tint` for larger info-block surfaces.
- **On-primary (`#E7FBF5`)** is the muted mint used for secondary text sitting directly on `primary` (the footer strip's contact line, next to the bold-white brand name).
- **Warning-bg (`#FFF7E8`) / Warning-border (`#F0D7A0`) / Warning-ink (`#6B4E10`)** is a warm amber-family warning treatment — deliberately **not** a red/error color — used for the survey form's out-of-service-area banner. The file header in `mockups/key-form-khao-sat.html` is explicit that this must stay in the amber family, not read as a blocking error, because the warning is non-blocking by design (FR-8, PRD Open Question 2, resolved: warn but still accept).
- **Error (`#C0511F`)** `[ASSUMPTION: the only warm/red-adjacent hue that appears anywhere in the approved artifacts is the required-field asterisk in mockups/key-form-khao-sat.html. No distinct field-validation-failure state (e.g. "trường này là bắt buộc" after a failed submit) was ever rendered or discussed in this session. This token is extracted from that one confirmed use (the required-field marker) and extended, as a reasonable default, to cover general form validation-error text — flagged for explicit confirmation before implementation, since it was never itself tested as an error color.]`

## Typography

**Mulish** (Google Fonts, weights 400/600/700/800, Vietnamese subset), with a system-font fallback stack for load resilience: `Mulish, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif`.

→ Visual reference: [mockups/typography-1.html](mockups/typography-1.html) — Mulish picked head-to-head against three other candidates (Inter, Plus Jakarta Sans, IBM Plex Sans), all rendered in the locked palette with real Vietnamese diacritic-bearing copy.

Phước picked Mulish explicitly for two reasons: its rounder terminals read as the brightest/freshest of the four candidates — the clearest typographic expression of the "tươi sáng" (bright/fresh, school-age-appropriate) note that also shaped the color-theme refinement rounds — and it is the most visually distinct from Site B's Be Vietnam Pro (warm/humanist vs. Mulish's geometric-rounded character), keeping the two sub-brands as visually distinct at the type level as they already are at the color level. Inter was rejected as too close in spirit to a system font (under-commits on brand distinction); IBM Plex Sans was rejected as too technical/cold for the "đáng tin cậy" (trustworthy, approachable) half of the brand direction; Plus Jakarta Sans was a close second.

The ramp is desktop-first and larger than Site B's mobile-first scale at the top end: `display` (34px/800) is reserved for the homepage hero headline only, sized for a large full-bleed banner rather than a phone screen — taken from the final picked homepage direction (`mockups/direction-thuong-hieu-rong-rai.html`), which rendered the hero type larger than the earlier `color-themes-3.html` exploration (26px) once applied to the actual spacious homepage layout; the later, more-finished render wins. `heading-lg` (25px/800) is the product-detail page's H1. `heading-md` (19px/700) is a homepage section header ("Nhóm sản phẩm nổi bật"). `heading-sm` (17px/800) covers the specs-table heading and the survey-form modal's title. `heading-xs` (15px/800) is the service-area block's small emphasized heading. `body` (13.5px/400) is running product description prose; `body-sm` (12.5px/400) is note/helper text (price notes, shipping notes, service-area copy). `caption` (11px/600) covers trust-chip text, the footer strip, and the floating contact chip. `label` (11px/700, tracked 0.05em) is for the uppercase price eyebrow and the hero's small eyebrow badge.

Two roles exist specifically because of the price state pattern (see EXPERIENCE.md State Patterns): **`price`** (24px/800) is the real numeric price, sized to be the clear visual anchor of the info block when a price exists; **`price-fallback`** (20px/800) is the "Liên hệ để nhận báo giá" text that renders in the same block position when no price is set — one size step down from a real number, because a call-to-action sentence reads differently than a number and doesn't need quite the same visual weight to still dominate the block. `cta` (14px/700) is every button label, sized deliberately larger than a typical secondary label because a tap target's text is not a place to economize.

## Layout & Spacing

Desktop-first, with responsive mobile as a secondary, unverified target — every rendered mock in this session (`.working/*.html`) was built and reviewed at desktop width only (the `browser-frame` demo wrapper in every file is fixed at `content-max-width` 1200px). `[ASSUMPTION: no mobile-specific spacing, stacking, or nav-collapse behavior was ever rendered or discussed for Site A — unlike Site B, which is mobile-first by explicit decision. This is the inverse gap: mobile layout rules for Site A are not yet designed and should not be assumed to simply mirror Site B's mobile patterns, since the two sites deliberately don't share a visual system. Flagged as a genuine open item, not filled in here.]`

`gutter` (40px) is the desktop side padding used consistently across the nav, hero, and PDP sections in every mock — a wide, confident margin appropriate to the "Thương Hiệu Rộng Rãi" (spacious brand) register, in deliberate contrast to Site B's tighter 16–24px catalog gutters. `section-gap` (48px) separates major homepage sections (hero → featured categories → trust band → footer). `card-gap` (20px) is the gap inside the featured-category grid. `field-gap` (14px) separates fields within the survey-form modal.

The homepage's featured-category grid runs 3 columns wide at desktop width, showing only 6 of the 11 categories plus a "Xem tất cả 11 nhóm sản phẩm →" link — a deliberate low-density choice (the picked "Thương Hiệu Rộng Rãi" direction over three higher-density alternatives that were also rendered and rejected: mega-menu, dense 11-up grid, and a spec-sheet table layout). The product-detail page uses a 2-column grid (gallery / info) with a 46px gap, wide enough to keep gallery and info from feeling cramped against each other at desktop width.

## Elevation & Depth

Elevation is used sparingly and only for genuinely floating/overlay elements — everything else is flat, separated by `border`/`border-neutral` hairlines and the `bg`/`surface`/`surface-tint` tonal steps, not by shadow. This mirrors Site B's flat-by-default discipline, arrived at independently by both sites' rendered mocks rather than shared as a rule.

Three real shadows appear across the approved artifacts, all reserved for content that visually floats above the page:
- **Floating contact chip** (nav): `0 3px 10px rgba(0,0,0,.2)` — lifts the persistent hotline/Zalo chip off the teal banner.
- **Nav "Sản phẩm" dropdown panel**: `0 14px 30px rgba(0,0,0,.18)` — a stronger lift appropriate to a panel that overlaps page content.
- **Survey-form modal**: `0 20px 50px rgba(0,0,0,.35)` — the strongest shadow on the site, paired with a dimmed/blurred backdrop (`rgba(18,56,50,.62)`), appropriate to a true overlay that blocks interaction with the page behind it.

Cards, price blocks, the service-area block, and the process-strip carry **no shadow** — they're separated from the page purely by the `border-neutral`/`border-tint` hairline and the `surface-tint`/`bg` tonal shift. Do not add card shadows; it would blur the line between "flat content block" and "floating overlay" that this system otherwise keeps clean.

## Shapes

Corners are moderately soft — never sharp (too industrial/cold for an institutional-trust brand) and never fully rounded on structural elements (too playful/consumer for a B2B procurement site). `rounded.sm` (6px) is the workhorse for buttons, form inputs, and dropdown-item rows. `rounded.md` (8px) covers small image/icon containers (category-icon squares, thumbnail images). `rounded.lg` (10px) is for card-level surfaces — the gallery's main product image, the price block, the nav dropdown panel. `rounded.xl` (12px) is reserved for the largest content blocks — featured-category cards, the process-strip, and the survey-form modal — where a slightly softer corner suits a bigger surface. `rounded.full` is used only for pill chips (trust badges, the floating contact chip, the hero eyebrow badge) and true circles (the process-strip's numbered step markers).

## Components

→ Visual references: [mockups/direction-thuong-hieu-rong-rai.html](mockups/direction-thuong-hieu-rong-rai.html) (homepage, hero, category tiles, trust band), [mockups/key-chi-tiet-can-thi-cong.html](mockups/key-chi-tiet-can-thi-cong.html) (installation-required PDP, service-area, process-strip, nav dropdown open), [mockups/key-chi-tiet-ship-thang.html](mockups/key-chi-tiet-ship-thang.html) (shippable PDP, price-present), [mockups/key-form-khao-sat.html](mockups/key-form-khao-sat.html) (survey form, both service-area states).

- **`nav`** — Solid `banner-bg` top bar on every page. Wordmark "NGỌC ANH" in `nav-brand`, flat link list ("Trang chủ · Giới thiệu · Sản phẩm · Tin tức · Liên hệ") in `nav-link` — deliberately **not** a mega-menu even across 11 categories (see Layout & Spacing above). "Sản phẩm" opens the `nav-dropdown` on hover/click. A `contact-chip` (hotline + Zalo) floats at the nav's right edge on every page — the one always-visible, never-scrolled-away contact surface site-wide.
- **`nav-dropdown`** — Simple single-column list of all 11 category names, `surface` background, `rounded.lg`, drop shadow. Ends in a divider then a bold `primary`-colored "Xem tất cả 11 nhóm sản phẩm →" link to the aggregate category page. Deliberately flat-list, not multi-column mega-menu — this is what makes 11 categories manageable in a simple nav.
- **`hero-carousel`** — Full-bleed photo background (real product/installation photography), photo kept clearly visible — the teal gradient overlay (`hero-carousel.overlay`) stays nearly transparent through the upper ~45% of the image and only darkens toward the bottom, just enough for the white heading/CTA zone to stay legible, never a wash across the whole photo. `display` heading, `banner-subtext` body copy, primary amber CTA + outline secondary CTA, carousel dot/arrow controls at the bottom edge. This is the default hero state when photos are available.
- **`hero-fallback`** — When no product/installation photos exist yet for a rotation, the hero collapses to a solid `banner-bg` (same teal as the nav, so nav+hero read as one continuous band), same heading/CTA content, no photo, no carousel controls. Both states must render correctly — this is a graceful-degradation requirement, not an edge case to skip.
- **`category-tile`** *(featured-category card)* — Homepage-only, shows a curated 6 of the 11 categories with a "Xem tất cả" link out to the full list (see EXPERIENCE.md Information Architecture for the aggregate page's full, CMS-driven grid). `bg` card, `border-neutral` outline, `rounded.xl`, a small `primary`-filled icon block (`rounded.md`), category label in `body` weight. On the aggregate page, a tile may additionally carry 1-2 small `trust-badge`-style certification pill chips (e.g. "Đạt chuẩn ASTM," "CARB P2") in a row below the label — optional, CMS-driven per category, and shown ONLY when a real certification is on file; never a placeholder or generic claim.
- **`breadcrumb`** — Sits directly below `nav`, above page content, on every category page, PDP, and the aggregate "Danh mục sản phẩm" page. NOT shown on the homepage. Plain text trail with `text-muted` "/" separators, `primary`-colored links for every step except the current page, which renders in `text` (not a link). `caption` typography.
- **`category-search`** — Aggregate page only, sits below the page header, above the category grid. A search input (`surface` bg, `border-neutral` outline, `rounded.sm`) paired with a row of toggleable filter chips (pill shape, `rounded.full`, same visual family as `trust-badge` but interactive — `surface`/`border-neutral` at rest, `trust-bg`/`trust-border`/`trust-ink` when active) grouping categories loosely (e.g. "Mầm non & Vận động," "Thiết bị công nghệ").
- **`photo-badge`** — Small pill label overlaid directly on a real (non-stock) installation/product photo, reading "Hình ảnh thi công thực tế." `rounded.full`, `bg`-tinted semi-opaque backing (so it stays legible over any photo) with `text` ink, `caption` typography. Used on PDP gallery images that are genuine project photography, never on placeholder or AI-generated imagery.
- **`trust-badge`** — Small pill chip for short trust claims, used as the PDP's `mini-trust` row (e.g. "Bảo hành chính hãng," "Đội thi công riêng," "Khảo sát tận nơi miễn phí"). `trust-bg`/`trust-border`/`trust-ink`, `rounded.full`.
- **`trust-stat`** — Homepage-only numeric stat block (500+ trường đã lắp đặt, 20 năm kinh nghiệm, 100% bảo hành, 24/7 hỗ trợ), stacked number-over-label, no chip/pill shape. Sits in the `surface-tint`/`border-tint` trust band below the category grid — placed deliberately low on the page (after categories, not before), so the confident-brand hero and category grid get first attention.
- **`price-block`** — The PDP price surface, `surface-tint` background, `border-tint` outline, `rounded.lg`. Renders one of two states: a real `price` value in `primary` color (shippable products with a set price), or the `price-fallback` text "Liên hệ để nhận báo giá" in the same position and block styling when no price is set (installation-required products, or any product without a CMS price value). See EXPERIENCE.md State Patterns for the behavioral rule.
- **`service-area`** — Installation-required-only block. Bordered `bg` panel with a `heading-xs` "Khu vực phục vụ: Miền Bắc – Thanh Hóa" heading in `primary`, plain descriptive text, and a nested `warning-bg` note explaining that out-of-area survey requests are still accepted. Absent entirely from shippable-product PDPs.
- **`process-strip`** — Installation-required-only, 3-step visual (Khảo sát → Hợp đồng → Thi công) on a `surface-tint` band, numbered circles in `primary`/white, `banner-outline`-colored arrows between steps. Absent entirely from shippable-product PDPs, replaced there by a `shipping-note` chip ("Giao hàng toàn quốc — 3–7 ngày làm việc tùy khu vực").
- **`cta-buttons`** — Every primary action site-wide (Nhận báo giá, Đăng ký khảo sát miễn phí, Yêu cầu báo giá, Gửi đăng ký, Đặt mua ngay) is solid `cta` amber, `cta-text` ink, `rounded.sm`. Every secondary action (Gọi ngay · Zalo tư vấn) is an outline button in `primary` teal, same radius. This primary/secondary pairing is the single most consistent pattern across every rendered mock — never invert it (amber for secondary, outline for primary).
- **`survey-form-modal`** — The FR-8 "Đăng ký khảo sát miễn phí" form, a centered modal over a dimmed/blurred backdrop of the product-detail page behind it. Fields: sản phẩm quan tâm (prefilled/linked from page context, styled distinctly in `surface-tint`/`primary` to signal "already filled for you, not editable text"), địa điểm/địa chỉ*, họ tên*, số điện thoại* — required fields marked with an `error`-colored asterisk. Full-width `cta` submit button. See State Patterns for the out-of-area warning variant.
- **`warning-banner`** — Non-blocking amber-family warning (never red/`error`), used inside the survey-form modal when the entered địa điểm falls outside Miền Bắc–Thanh Hóa. Icon circle in `cta` amber with a bold "!" mark, bold headline + explanatory body text in `warning-ink`. The affected field's border also switches to `warning-border`/`warning-bg`-tinted background, giving a second, non-color-only signal.
- **`footer-strip`** — Thin `primary`-background bar on every page, bold white brand name left, `on-primary`-colored Zalo/phone/email contact line right.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Keep the nav a flat link list + one simple-list "Sản phẩm" dropdown, even across 11 categories | Build a multi-column mega-menu (see Layout & Spacing) |
| Reserve `cta` amber for primary actions only; `primary` teal outline for secondary actions | Use amber decoratively, or invert the primary/secondary button pairing |
| Design both `hero-carousel` and `hero-fallback` states — a PDP/homepage must never assume photos exist | Ship a hero that breaks or shows a broken-image icon when no product photos are available yet |
| Show `price-fallback` ("Liên hệ để nhận báo giá") when no CMS price exists, in the same block styling as a real price | Leave the price field visibly blank, or hide the whole price block, when price is absent |
| Use `service-area` + `process-strip` only on installation-required products | Show the khảo sát/hợp đồng/thi công process strip on a shippable product that has no installation step |
| Warn (amber, non-blocking) on out-of-service-area survey submissions, but let them submit | Hard-block or reject a survey-request form submission based on location — FR-8 explicitly resolved this as warn-not-block |
| Keep the confident, spacious "Thương Hiệu Rộng Rãi" register — large hero, only 6 of 11 categories on the homepage, generous `gutter`/`section-gap` | Default to a dense, promo-driven layout crowding every category and a discount badge onto the homepage — that is the legacy tbtruonghoc.com's rejected register |
| Keep the teal + amber palette and Mulish type clean and institutional | Introduce warm browns, a craft/heritage serif, or any other element of Site B's Mộc Trầm register — the two sites are deliberately distinct sub-brands, never to be visually confused |
| Reserve gradients for the one hero photo-overlay use | Use gradients decoratively elsewhere on the site |
| Keep body text in `text` (`#123832`), never pure black or white | Set running text in pure `#000` or `#FFF` |
| Show a certification `category-tile` badge only when a real, verifiable certification is on file for that category | Fabricate or imply a certification the company doesn't actually hold — a false claim risk on an institutional-trust B2B site |
| Reserve `photo-badge` ("Hình ảnh thi công thực tế") for genuine project photography | Put it on stock, placeholder, or AI-generated imagery — that would make the badge itself a false claim |
