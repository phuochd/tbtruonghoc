---
title: 'Story 6.1: Site A (Xanh Lục Bảo Rạng Rỡ) page shell — nav, hero, footer'
type: 'feature'
created: '2026-09-30'
status: 'done'
baseline_commit: '85127847e60763ab654b28c7681eb3294a2dff8b'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-tbtruonghoc-2026-09-18/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Site A (tbtruonghoc.com) still renders through the Piranha sample `_Layout` (Lato/Raleway, Bootstrap navbar, unstyled `_ContactBlock`), so it has no institutional brand, no "Sản phẩm" dropdown, no always-visible hotline/Zalo and no homepage hero.

**Approach:** Give Site A its own layout (picked by `SiteLayout`, like Site B) carrying the Xanh Lục Bảo Rạng Rỡ tokens (Mulish, teal/amber), a sticky teal nav with a sitemap-driven flat link list, a keyboard-operable "Sản phẩm" dropdown fed by the existing `ProductCatalog` hide rule, a contact chip, a mobile hamburger sheet with an inline accordion, a teal footer strip, sitewide primary/secondary button styles, and a homepage page type whose hero renders as a photo carousel (≥768px, photos present) or the solid-teal fallback.

## Boundaries & Constraints

**Always:**
- Site A pages load only Mulish 400/600/700/800 (Google Fonts, `display=swap`) + the DESIGN.md tokens as CSS custom properties in a new plain-CSS `site-a.css`; never `style.min.css`, Lato/Raleway, Be Vietnam Pro or any `sb-`/Mộc Trầm class or value. Bootstrap CSS may stay (shared partials use utilities) with its font/color vars pointed at Site A tokens. `<html lang="vi">`, skip link first.
- Nav links = non-hidden top-level sitemap items; nothing hardcoded. A top-level item whose page type is `ProductHubPage` gets the dropdown: its tiles from `ProductCatalog.GetHubTilesAsync` (so empty/hidden/draft categories are hidden, same rule as Site B), then a divider and "Xem tất cả {N} nhóm sản phẩm →" linking to the hub, N = tile count. No tiles → plain link, no dropdown.
- Dropdown: opens on hover (≥1024px) and on click/Enter/Space/ArrowDown; arrows/Tab move through rows; Escape closes and returns focus to the trigger; click-away closes. Works without JS via `:hover`/`:focus-within`.
- Below 1024px: wordmark + hamburger; the sheet slides in from the right (teal, close ×), "Sản phẩm" expands as an inline accordion listing the same tiles + "Xem tất cả". The contact chip stays visible outside the sheet (icon-only).
- Nav is sticky at the top on every width, so the chip never scrolls away. Chip/hero/footer phone and Zalo come from `SiteSettings` via `ContactLinks` (`tel:` digits; Zalo `SafeUrl`, as-is, new tab). Accessible labels name the destination ("Gọi ngay tới Ngọc Anh", "Chat Zalo với Ngọc Anh"). Unset value → that link omitted; nothing set → no chip.
- Footer strip: bold white "NGỌC ANH" left; right: Zalo · phone · email, then address (linked to Maps when `MapsUrl` is safe) — keeps FR-2's per-site address/Maps on Site A. Add an optional `Email` region to `SiteSettings` (rendered as `mailto:` only when it is a valid address).
- Buttons: `.sa-btn--primary` (amber fill, cta-text) and `.sa-btn--secondary` (teal outline). The hero always pairs its primary CTA with secondary "Gọi ngay · Zalo tư vấn"; never inverted.
- Hero (homepage type only): photos present and ≥768px → carousel with the DESIGN.md gradient overlay and dot/arrow controls; no photos, or <768px → solid-teal fallback with the same eyebrow/heading/subtext/CTAs. Phones must not download hero photos (`<picture>` with a `min-width:768px` source). Photo alt = media AltText, else the heading. Never an empty/broken hero; blank heading falls back to the page title.
- `ViewData["HideSiteChrome"] = true` drops nav, chip and footer (tokens/analytics/consent stay). `_Analytics` and `_CookieConsent` stay included.
- **Decision (Q1):** the carousel is manual only — dot/arrow controls, no auto-advance, no timers.
- **Decision (Q2):** hero is authored on a new page type "Trang chủ (Site A)" (`SiteAHomePage`) with regions eyebrow, heading, subtext, primary CTA label (default "Nhận báo giá") + link (primary omitted when the link is blank/unsafe), and a photo list. Story 6.2 extends this same type.
- **Decision (Q3):** idempotently seed a published "Trang chủ" `SiteAHomePage` only when Site A has no pages at all; hero fields blank (heading falls back to the title). Never touches existing pages.

**Never:**
- Change Site B's output, `_LayoutTrongDoiTam`, `_Layout.cshtml` (it stays the no-site fallback), `SiteSeed`, the SiteSettings save hooks or `Areas/Manager`.
- Mega-menu, cart/checkout/account UI, a hardcoded category list or count, amber used decoratively, gradients outside the hero overlay, shadows other than chip/dropdown.
- Build the homepage tile grid/trust band, aggregate page, category pages or PDPs (Stories 6.2–6.5), or restyle `ProductArchive`/`ProductPost` views for Site A.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Hub with categories | Top-level ProductHubPage, 3 published non-empty archive children + 1 empty | Dropdown lists 3 rows + "Xem tất cả 3 nhóm sản phẩm →" | Empty one hidden |
| Hub without categories | ProductHubPage with no qualifying children | Plain nav link, no dropdown/toggle | N/A |
| All contacts set | Phone, ZaloUrl, Email, Address, MapsUrl | Chip: tel + Zalo; footer: Zalo · phone · mailto · address→Maps | N/A |
| Nothing set | All empty | No chip; footer shows brand only | N/A |
| Bad email | Email = `not-an-email` | No mailto link | Treated as unset |
| Hero with photos | Home page, 2 photos | Carousel (2 slides, controls) in a ≥768px `<picture>` source + fallback markup for <768px | N/A |
| Hero no photos | Home page, 0 photos | Fallback only, no carousel controls | N/A |
| Chrome hidden | `HideSiteChrome=true` | No nav/chip/footer; tokens + consent present | N/A |
| Site B | Any page | Byte-identical to pre-change Site B output | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Models/SiteLayout.cs` -- add `TbTruongHoc = "_LayoutTbTruongHoc"`; `ForInternalId` maps `SiteSeed.TbTruongHocInternalId` to it; `Default` stays for no-site.
- `src/TbTruongHoc.Web/Views/Shared/_LayoutTrongDoiTam.cshtml`, `_SiteBNav.cshtml`, `wwwroot/assets/js/site-b-nav.js` -- golden pattern (skip link, `html.js`, `HideSiteChrome`, sitemap loop, disclosure buttons, Escape handling); copy structure, not classes/styles. Do not modify.
- `src/TbTruongHoc.Web/Services/ProductCatalog.cs` -- `GetHubTilesAsync(siteId, hubId)` is the single hide rule; reuse for the dropdown. Page type check via `_api.Pages.GetByIdAsync<PageInfo>(id).TypeId == nameof(ProductHubPage)`.
- `src/TbTruongHoc.Web/Models/ContactLinks.cs` -- `TelHref`, `SafeUrl`; add an email helper here.
- `src/TbTruongHoc.Web/Models/SiteSettings.cs` -- add `Email` region.
- `src/TbTruongHoc.Web/Models/LandingPage.cs` -- pattern for page-type regions, `Trimmed`, alt-text fallback.
- `src/TbTruongHoc.Web/Views/Cms/Page.cshtml` -- sample `<header>`/`.block` markup; style minimally in `site-a.css`, don't edit.
- `src/TbTruongHoc.Web/Data/BlogSeed.cs`, `Program.cs` l.318-355 -- idempotent seed pattern/registration (Q3: seed Site A home).
- `tests/TbTruongHoc.Web.Tests/SiteBShellTests.cs` -- l.206 InlineData and `Site_A_Keeps_The_Shared_Layout` (l.334) assert Site A uses `_Layout`; update to the new layout. Render-test pattern, `HideChromeProbeController` for chrome-hidden.
- `tests/TbTruongHoc.Web.Tests/SiteSettingsTests.cs` -- many Site A asserts on `contact-block__*` (l.95, 218, 606, 654-658…); retarget to Site A footer/chip classes, keeping each test's intent.
- `tests/TbTruongHoc.Web.Tests/SiteBNavScriptTests.cs` -- Jint pattern for JS tests.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Models/SiteSettings.cs`, `Models/ContactLinks.cs` -- `Email` region; `ContactLinks.MailtoAddress(email)` (single address, `MailAddress` parse) -- footer email.
- [x] `src/TbTruongHoc.Web/Models/SiteLayout.cs` -- Site A mapping.
- [x] `src/TbTruongHoc.Web/Models/SiteAHomePage.cs` + `Views/Cms/SiteAHomePage.cshtml` + `Views/Shared/_SiteAHero.cshtml` -- homepage type and hero carousel/fallback.
- [x] `src/TbTruongHoc.Web/Data/SiteAHomeSeed.cs` + `Program.cs` -- seed "Trang chủ" when Site A has zero pages; register after `BlogSeed`; idempotency test.
- [x] `src/TbTruongHoc.Web/Views/Shared/_LayoutTbTruongHoc.cshtml` -- lang=vi, Mulish, Bootstrap CSS, `site-a.css`, analytics, skip link, `_SiteANav`, main, `_SiteAFooter`, consent, `site-a.js`; honor `HideSiteChrome`.
- [x] `src/TbTruongHoc.Web/Views/Shared/_SiteANav.cshtml` -- wordmark, links, dropdown/accordion, chip, hamburger sheet.
- [x] `src/TbTruongHoc.Web/Views/Shared/_SiteAFooter.cshtml` -- footer strip.
- [x] `src/TbTruongHoc.Web/wwwroot/assets/css/site-a.css` -- tokens, Bootstrap overrides, nav/dropdown/sheet/chip/hero/footer/buttons/breadcrumb-free page header + block styles; breakpoints 768/1024; grids stack to 1 column <768px.
- [x] `src/TbTruongHoc.Web/wwwroot/assets/js/site-a.js` -- sheet, accordion, dropdown keyboard/hover/click-away, manual carousel controls; progressive enhancement.
- [x] `tests/TbTruongHoc.Web.Tests/SiteAShellTests.cs` -- every I/O row + no Lato/Be Vietnam Pro/`style.min.css`/`sb-`, `lang="vi"`, no cart/login markup in nav/footer.
- [x] `tests/TbTruongHoc.Web.Tests/SiteAScriptTests.cs` -- Jint: dropdown open/arrow/Escape/click-away, sheet toggle, carousel next/prev.
- [x] `tests/TbTruongHoc.Web.Tests/SiteBShellTests.cs`, `SiteSettingsTests.cs` -- retarget Site A asserts.

**Acceptance Criteria:**
- Given a Site A page at ≥1024px, when "Sản phẩm" is focused and Enter pressed, then the dropdown opens, ArrowDown moves through rows and Escape closes it with focus back on the trigger.
- Given a Site A page at 375px, when the hamburger is activated, then a right-side teal sheet shows all links with "Sản phẩm" expandable inline, and the chip icon remains visible.
- Given keyboard navigation from load, when tabbing, then order is skip link → nav (incl. chip) → main → footer.
- Given the full test suite, when run, then all pre-existing tests pass (with Site A asserts retargeted).

## Implementation Notes

- "Sản phẩm" trigger is a `<button>` (not a link) so Enter/Space open the panel; the hub itself is reached via the "Xem tất cả" row. One panel markup serves both the ≥1024px dropdown and the <1024px accordion.
- Hero text/CTAs render once; `.sa-hero--carousel` layers the photo slides behind them at ≥768px, below that the band is the solid-teal fallback (no duplicated `<h1>`). Slide `<img>` src is an inline 1×1 GIF, real photos only in the `min-width:768px` `<source>`.
- Secondary hero CTA is one outline button with two links ("Gọi ngay" → tel:, "Zalo tư vấn" → Zalo), each omitted when unset; the whole secondary is omitted when neither is set.
- Hero CTA link accepts absolute http(s), site-relative `/path` (not `//`) or `#anchor` (`SiteAHomePage.SafeLink`).
- Sheet opens below the sticky bar (not over it) so the chip and hamburger stay visible; close × inside the sheet.
- Added `CmsController.SiteAHomePage` action (route `/siteahomepage`) and minimal `.quote-request-form` styling under Site A tokens (the shared form on `Cms/Page` was otherwise unstyled).

## Spec Change Log

- Walkthrough patch (2026-09-30), owner-renegotiated after comparing rendered mocks (`_bmad-output/planning-artifacts/ux-designs/ux-tbtruonghoc-2026-09-18/.working/hero-box-mo.html`, chosen option "A1+"). The DESIGN.md teal gradient overlay tinted every photo, and the white text was still hard to read. Changes:
  - **Photos and box.** The hero keeps full-bleed photos (object-fit cover, min-height 560px at >= 768px) with **no overlay**; the `hero-carousel.overlay` token is dropped. All text sits in one frosted box at the bottom-left. The box is about 1/3 of the content width (min 360px; up to 440px at 768–1023px), with teal at 22% opacity, `backdrop-filter: blur(3px)`, a white hairline border at 28% opacity and a text-shadow. Only the photo behind the box is blurred.
  - **Text.** The eyebrow is a solid pill: teal over photos, white on the fallback. The heading is amber `#FFC857`; the owner relaxed DESIGN.md's "amber only for CTAs" rule for this heading. The subtext is white.
  - **CTAs.** The combined secondary "Gọi ngay · Zalo tư vấn" button is replaced by **one row of three ghost buttons**: "Nhận báo giá" (amber text), "Gọi ngay" (white, phone icon) and "Zalo" (Zalo blue `#3D8BFF`, chat icon). Each has a dark frosted fill and the box's hairline border. On hover/focus each fills with its own colour: amber with cta-text, white with teal text, and Zalo `#0068FF` with white text. Each button is omitted when its value is unset; the order is fixed (quote, call, Zalo).
  - **Controls and markup order.** Carousel controls sit bottom-right on dark chips. Before this patch they were unclickable, because the media layer's z-index trapped them under the text. The carousel markup now follows the text, so text and CTAs come first in reading and tab order.
  - **Image sizes.** srcset includes the original width, capped at 2560 and never upscaled, so wide or HiDPI screens don't stretch a 1280px copy.
  - **Phones.** Unchanged: solid teal and no photo downloads.
  - **Gallery.** The 5.2 gallery-block styling is restored for Site A in `site-a.css`. It lived in `style.min.css`, which the new Site A layout no longer loads.
  - **Still to do.** DESIGN.md `hero-carousel` and `cta-buttons` still describe the old overlay and the combined secondary button, and need the same amendment.

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V):

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| B1 | ProductHub/Archive/Post/LandingPage views still emit `sb-*` on Site A | false | Frozen Never excludes restyling those views (6.2–6.5); the no-`sb-` rule covers the shell → rejected. |
| B2/E1 | `SafeLink` accepts `/\t/host`, which browsers turn into `//host` | low | Real (browsers strip TAB/CR/LF); one-line guard → patch. |
| B3 | `banner-link`/`banner-subtext` on teal ≈ 4.20/4.47:1, under AA for small text | medium | Ratios recomputed; locked DESIGN.md tokens and AA itself unconfirmed → defer (design decision). |
| B4 | Hero text over bright photos may lack contrast (overlay 5–10% to 45%) | maybe-false | Needs real photography to check; medium if true → defer, unverified. |
| B5 | Carousel `aria-roledescription` on role-less div; slide changes not announced | low | Real (ARIA forbids on generic); add `role="region"` + `aria-live` → patch. |
| B6 | "Trang chủ (Site A)" type selectable on Site B | low | Editor misuse only; guard adds branches → rejected. |
| B7 | Per-item `Pages.GetByIdAsync` in nav on every request | low | Real extra queries; direct simplification via sitemap item → patch (uses `PageTypeName`, which holds the type title → `ProductHubPage.IsHub`). |
| B8/V1 | `Site_A_Has_A_Page_After_Startup_Seed` vacuous; public seed path untested | medium | Real: deleting the Program.cs call stays green → patch (internal-id seam + tests). |
| B9 | Copy-pasted comment in SiteSettingsTests empty-contact test | low | Real; direct correction → patch. |
| B10/E2 | `MailtoAddress` accepts `%`/`#`; invalid email hidden silently | low | Real (`a%40evil@x.vn` decodes differently); reject chars + description note → patch. |
| B11/E5/E6 | Desktop nav/dropdown may overflow at 1024–1200px | maybe-false | 5 IA items + chip fit; depends on final sitemap → rejected (low if true), same as Site B's B8. |
| B12 | `html.js` set before `site-a.js` loads; failed script hides mobile menu | low | Local asset, rare; moving it causes menu flash → rejected (same as Site B E1). |
| B13 | Hardcoded rgba literals; wrong `.cookie-consent` comment | low | Comment wrong → patch; literals are overlay/alpha variants of tokens → rejected. |
| B14 | srcset advertises 1920w even for narrower originals (upscaled) | low | Real for small uploads; direct condition → patch. |
| B15 | Missing tests: draft/unauthorized, chrome-hidden on home, one-photo hero, resize, no-JS, row aria-current | low | Minor gaps with low regression risk → rejected. |
| E3 | `ResizeImage` null for unsupported media → blank slide | maybe-false | Depends on Piranha behavior for SVG; low if true → rejected. |
| E4 | Sheet open state persists across a resize to ≥1024px | low | Rare; fix adds listeners → rejected (same as Site B B6). |
| E7 | Current category row in dropdown lacks `aria-current` | low | Tile model has no id; fix adds plumbing → rejected. |
| E8 | Concurrent startups could double-seed | low | Single-instance deploy; rejected. |
| E9 | Sheet is full-width, not a partial right panel | false | EXPERIENCE.md: "full-screen teal sheet sliding in from the right". |
| V2 | Site A parent `is-active` untested | low | Real gap; test only → patch. |
| V3 | Maps-only footer ("Xem bản đồ") untested | low | Real gap; test only → patch. |

## Design Notes

The dropdown reuses the hub model rather than a new "Sản phẩm" concept: 6.2's aggregate "Danh mục sản phẩm" page becomes a Site A `ProductHubPage` with the 11 category archives as its sitemap children, so nav, aggregate grid and homepage share one list and one hide rule, and a 12th category needs no code. `GetHubTilesAsync` does one archive query per child per request; acceptable at 11 categories — revisit with caching only if 8.4's CWV baseline flags it.

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test` (repo root) -- expected: all tests pass, including the new Site A tests.
- `dotnet build` -- expected: no new warnings.

**Manual checks (if no CLI):**
- Run the app; open the tbtruonghoc host at 375px, 820px, 1280px: teal/amber + Mulish only, sticky nav with chip, dropdown/sheet behave, hero fallback on phone; trongdoitam host unchanged.
