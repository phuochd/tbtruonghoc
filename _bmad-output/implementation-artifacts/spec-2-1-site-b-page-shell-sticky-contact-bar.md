---
title: 'Story 2.1: Site B (Mộc Trầm) page shell, nav & sticky contact bar'
type: 'feature'
created: '2026-09-27'
status: 'done'
baseline_commit: '4f6c0b39e28754c9739426f96accd8daecb5c385'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-trongdoitam-2026-09-17/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Both sites render through the shared Piranha sample `_Layout` (Lato/Raleway, Bootstrap navbar, unstyled `_ContactBlock`), so trongdoitam.net has no Mộc Trầm identity, no real nav, and no always-visible contact path for its phone/Zalo-first audience.

**Approach:** Give Site B its own layout (selected by site in `_ViewStart`) carrying the Mộc Trầm tokens, a fixed 56px CMS-sitemap-driven nav with a one-level submenu and tablet/desktop hotline, a minimal footer, and a bottom-fixed 3-segment sticky contact bar fed by `SiteSettings`. Site A's render stays byte-for-byte on the existing `_Layout`.

## Boundaries & Constraints

**Always:**
- Site B pages use only Be Vietnam Pro (Google Fonts, `display=swap`, Vietnamese subset) and the Mộc Trầm tokens as CSS custom properties; Site B layout does not load `style.min.css` or Lato/Raleway. Bootstrap CSS stays (existing partials use its utilities) but its font/color variables are overridden by tokens.
- Nav items come from `WebApp.Site.Sitemap` (non-hidden top-level items; a top-level item's non-hidden children form its one-level submenu — this is how the Trống hub + 5 subpages appear once Story 2.2 creates them). Nothing hardcoded.
- Phone/Zalo/Maps come from the current site's `SiteSettings`, sanitized by the same rules as `_ContactBlock` (tel: digits + leading "+", ≥7 digits; Zalo/Maps via `SiteSettingsValidation.IsSafeAbsoluteUrl`). Extract those rules into one shared C# helper used by both partials — no copy-paste.
- Unset contact values: segment/hotline omitted; if all three bar segments are unset, the bar and its body bottom-padding are omitted.
- Sticky bar is in DOM order after `<main>` and the footer, is a labeled `<nav>`, never auto-hides, and body has bottom padding equal to its height. The layout starts with a skip-to-content link.
- `_Analytics` and `_CookieConsent` remain included on Site B; the cookie banner sits above the sticky bar (not behind or under it).
- `<html lang="vi">` on Site B.
- Footer shows the wordmark, address and phone from `SiteSettings` (keeps Story 1.3's per-site address render on Site B).
- Layout honors `ViewData["HideSiteChrome"] = true` by omitting nav, footer and sticky bar (tokens/analytics/consent stay) — the hook Epic 3's landing-page template will set.

**Never:**
- Change Site A's rendered output, `_Layout.cshtml`, `SiteSeed`, SiteSettings save hooks, or `Areas/Manager`.
- Account/login icons, cart UI, auto-rotating anything, modal-on-modal, or any Site A palette/typeface/component styling.
- Rewrite the Zalo URL (use `ZaloUrl` as-is; `https://zalo.me/<n>` already falls back to web).
- Build homepage, category, PDP or story-page content (Stories 2.2–2.5), or a LandingPage type (Epic 3).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Site B, all set | Phone, ZaloUrl, MapsUrl set | Bar: Gọi ngay `tel:`, Chat Zalo (primary fill, new tab), Bản đồ (new tab); nav hotline shows Phone | N/A |
| Partial settings | Only Phone set | Bar has Gọi ngay only; no Zalo/Maps anchors | N/A |
| Nothing set | Phone/Zalo/Maps empty | No bar, no hotline, no bottom padding | N/A |
| Unsafe URL | ZaloUrl = `javascript:…` | Zalo segment omitted | Treated as unset |
| Sitemap w/ children | Top-level page with 2 child pages | Parent link + disclosure button; submenu lists both children | Hidden items skipped |
| Chrome hidden | `ViewData["HideSiteChrome"]=true` | No nav/footer/bar; tokens + consent present | N/A |
| Site A | Any page | Identical to pre-change `_Layout` output (contact-block, Piranha navbar) | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Views/_ViewStart.cshtml` -- picks layout; Site B = site whose `InternalId == SiteSeed.TrongDoiTamInternalId` (resolve via `IApi.Sites.GetByIdAsync(WebApp.Site.Id)`, Piranha-cached).
- `src/TbTruongHoc.Web/Views/Shared/_Layout.cshtml` -- Site A/shared layout; DO NOT modify.
- `src/TbTruongHoc.Web/Views/Shared/_ContactBlock.cshtml` -- phone/URL sanitizing logic to extract; its markup/classes must stay (SiteSettingsTests assert `contact-block__*` on Site A).
- `src/TbTruongHoc.Web/Views/Shared/_CookieConsent.cshtml` -- `.cookie-consent position-fixed bottom-0 z-3`; offset on Site B via CSS only.
- `src/TbTruongHoc.Web/Models/SiteSettings.cs` -- `Phone`, `ZaloUrl`, `MapsUrl`, `Address` (no separate hotline field; hotline = Phone). `SiteSettingsValidation.IsSafeAbsoluteUrl`.
- `src/TbTruongHoc.Web/Data/SiteSeed.cs` -- `TrongDoiTamInternalId = "trongdoitam-net"` (internal, same assembly as views).
- `src/TbTruongHoc.Web/Views/Cms/Page.cshtml` -- uses sample `<header>`/`.block` markup; give it minimal token-based styling in Site B CSS, don't edit the view.
- `tests/TbTruongHoc.Web.Tests/SiteSettingsTests.cs` -- pattern for per-site render tests (`GetHtmlAsync(permalink, HostnameOf(site))`, snapshot/restore settings, `[Collection(PiranhaAppCollection.Name)]`); l.46 asserts Site B html contains its Phone, Zalo, Maps and Address.
- `tests/TbTruongHoc.Web.Tests/AnalyticsConsentScriptTests.cs` -- Jint pattern for JS behavior tests.
- `_bmad-output/planning-artifacts/ux-designs/ux-trongdoitam-2026-09-17/DESIGN.md` -- token values (frontmatter), `nav`/`sticky-contact-bar` specs.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Models/ContactLinks.cs` -- new static helper: `TelHref(phone)`, `SafeUrl(url)` with the existing rules -- single source for sanitizing.
- [x] `src/TbTruongHoc.Web/Views/Shared/_ContactBlock.cshtml` -- use `ContactLinks`; markup unchanged -- dedupe.
- [x] `src/TbTruongHoc.Web/Views/_ViewStart.cshtml` -- choose `_LayoutTrongDoiTam` for Site B -- per-site shell without touching Site A.
- [x] `src/TbTruongHoc.Web/Views/Shared/_LayoutTrongDoiTam.cshtml` -- lang=vi, fonts, Bootstrap CSS, `site-b.css`, `_Analytics`, head section, skip link, `_SiteBNav`, main body, `_SiteBFooter`, `_StickyContactBar`, `_CookieConsent`, `site-b-nav.js`; honor `HideSiteChrome`.
- [x] `src/TbTruongHoc.Web/Views/Shared/_SiteBNav.cshtml` -- wordmark "Trống **Đọi Tam**" (second word `secondary`), sitemap links + one-level submenus (disclosure button, `aria-expanded`), hotline `tel:` link hidden below tablet, hamburger opening full-height single-level sheet.
- [x] `src/TbTruongHoc.Web/Views/Shared/_StickyContactBar.cshtml` -- 3 segments with icons (inline SVG, 20px), accessible labels ("Gọi ngay tới Đọi Tam" etc.).
- [x] `src/TbTruongHoc.Web/Views/Shared/_SiteBFooter.cshtml` -- wordmark, address, phone.
- [x] `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css` -- tokens as custom properties, Bootstrap var overrides, nav/sheet/submenu/bar/footer/page-header/block styles, cookie-banner offset; mobile-first, breakpoints ≥768px tablet.
- [x] `src/TbTruongHoc.Web/wwwroot/assets/js/site-b-nav.js` -- hamburger + submenu disclosure toggles, Escape closes and returns focus, no hover-only behavior; works as progressive enhancement.
- [x] `tests/TbTruongHoc.Web.Tests/SiteBShellTests.cs` -- render tests for every I/O matrix row except "Chrome hidden" (no page type sets it yet — cover via a unit-level check only if cheap), plus: Site B html has no Lato/Raleway/`style.min.css`, has `lang="vi"`, no login/account markup.
- [x] `tests/TbTruongHoc.Web.Tests/SiteBNavScriptTests.cs` -- Jint tests for toggle/Escape logic following the AnalyticsConsent pattern.

**Acceptance Criteria:**
- Given a Site B page at 375px, when the hamburger is activated, then a full-height sheet lists all nav items with submenu children listed flat beneath their parent, and no hotline appears in the nav bar.
- Given a Site B page at ≥768px, when rendered, then the 56px nav shows the hotline in 20/700 primary, and the submenu opens by click/Enter (not hover-only).
- Given any Site B page with the cookie banner visible, when viewed, then the banner and sticky bar are both fully visible and operable.
- Given keyboard navigation from page load, when tabbing, then order is skip link → nav → main content → footer → sticky bar.
- Given the full test suite, when run, then all pre-existing tests still pass.

## Implementation Notes

- Layout choice lives in public `Models/SiteLayout.cs` (`ForSiteAsync` / `ForInternalId`), called from `_ViewStart`. Reason: `SiteSeed.TrongDoiTamInternalId` is `internal`, and Razor runtime compilation (on in Program.cs) recompiles edited views into a separate assembly that could not see it.
- `analytics-consent.js` sets `body.style.paddingBottom` inline to the banner height while the banner shows, which replaces the bar padding. `site-b.css` handles this with CSS only: `body.sb-has-bar:has(> [data-consent-banner]:not([hidden]))` gets `margin-bottom` equal to the bar height, and `.cookie-consent` is offset to `bottom: <bar height>`.
- Keyboard order: the hamburger comes before the menu in DOM order, so the opened sheet is next in the tab order. Without JS (no `html.js` class) the mobile nav becomes static and fully expanded, and desktop submenus open on `:focus-within`.
- "Chrome hidden" row: covered by `SiteBShellTests.Hide_Site_Chrome_Drops_Nav_Footer_And_Bar_But_Keeps_Tokens_Analytics_And_Consent`. It renders the real `Cms/Page.cshtml` through `_ViewStart` on the Site B host, via a test-only `HideChromeProbeController` that is registered as an MVC application part in `PiranhaWebApplicationFactory`. The environment is switched to Production for that request so the GA4-gated consent banner renders.
- "Unsafe URL" row: covered at the `ContactLinks.SafeUrl` unit level. The Story 1.9 save hooks reject unsafe URLs on both save paths, so a page render can't reach that state.

## Spec Change Log

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V):

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| B1 | `_CookieConsent` emits a 2nd `<footer>` (reopen button) after the sticky bar | low | Real (2 contentinfo landmarks, only when GA4 gate on); fix needs changing the shared partial, which would alter Site A output (Never) → rejected. |
| B2/E8 | No scroll-padding for fixed nav/bar; focused/anchored content can sit under them | low | Real for keyboard users; one CSS rule → patch. |
| B3/E2/E15/E16 | ≥768 no-JS: submenu only on `:focus-within`, mouse users can't reach children; nav comment overclaims | low | Real; trivial CSS + comment → patch. |
| B4/B5/E4/E5/E6 | Escape handler is document-global and steals focus; no close when focus leaves nav → submenu/sheet stay open, focus goes under sheet | medium | Real: open submenu, Tab out, Escape in form → focus jumps to nav. Small JS fix → patch. |
| B6/E3 | Resize across 768 keeps open state; Escape can focus hidden button; `init` has no double-call guard | low | Rare; fix adds listeners/branches → rejected. |
| B7/V2 | `hasContactBar` duplicated between layout and bar partial; phone-only test doesn't check `sb-has-bar` | low | Drift risk real; guard with test assertion → patch. |
| B8/E7 | Tablet (768–1000px) nav may overflow with many items + hotline | maybe-false | Depends on final sitemap count/titles; needs browser check with real IA → defer (medium, unverified). |
| B9 | No print styles | low | Unlikely use; rejected. |
| B10 | No hover/pressed states on bar/nav; hover shade hardcoded | low | Real every tap on mobile; trivial CSS + token → patch. |
| B11 | Footer borrows `sb-nav__brand-accent` class; no Zalo/Maps in footer | low | Class coupling trivial → patch; footer content matches spec → not a defect. |
| B12 | No favicon/theme-color/title suffix | false | Not in spec; favicon absent sitewide (pre-existing); title is per-page SEO (Story 1.2). |
| B13 | Missing tests: Zalo-only, address-only, child-page active, `sb-has-nav`, 7-digit boundary | low | Real gaps; tests only → patch (merged with V2–V4). |
| B14 | Whole-page `login`/`account` DoesNotContain is fragile | low | Real (any CMS/analytics text trips it); scope to nav/footer → patch. |
| B15 | Whole test assembly added as app part; comment overclaims | low | Direct comment correction → patch. |
| E1 | `js` class set before script loads; failed script leaves mobile menu hidden | low | Local asset, rare; moving it causes full-menu flash → rejected. |
| E9 | viewport-fit=cover without horizontal safe-area padding | low | Landscape-notch only, cosmetic → rejected. |
| E10 | `:has()` unsupported → page end under bar while banner shown | low | Old browsers only, transient until consent; fix touches shared script → rejected. |
| E11/E17 | Mobile sheet (z1030) covers cookie banner while open | low | Closing sheet reveals banner; nothing lost → rejected. |
| E12 | Admin editing Site B InternalId falls back to Site A layout | low | Same pre-existing coupling as SiteSeed; rejected. |
| E13 | Phone with extension/two numbers → concatenated tel: | maybe-false | Pre-existing Story 1.3 rule, extracted unchanged → defer. |
| E14 | `HideSiteChrome` as string "true" ignored | false | Contract is a bool; documented in layout comment. |
| V1 | Site B `<title>` never asserted | low | Real regression gap → patch. |
| V3 | `sb-has-nav` never asserted present | low | Real gap → patch. |
| V4 | Parent `is-active` on child page untested | low | Real gap → patch. |

## Design Notes

Layout selection lives in `_ViewStart` rather than a flag inside `_Layout` so Site A's file is untouched and Epic 6 can later add its own `_LayoutTbTruongHoc` the same way. Plain CSS (not the gulp SCSS pipeline) keeps Site B free of the sample theme and avoids a build step; `style.min.css` is Piranha sample styling, not Site A's design.

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test` (repo root) -- expected: all tests pass, including new SiteBShellTests/SiteBNavScriptTests.
- `dotnet build` -- expected: 0 warnings introduced.

**Manual checks (if no CLI):**
- Run the app, open `http://trongdoitam.local:<port>/` at 375px and 1280px: Mộc Trầm colors/font only, nav + sheet behave, sticky bar fixed and never hides on scroll, Zalo segment filled primary; open `tbtruonghoc` host and confirm unchanged look.
