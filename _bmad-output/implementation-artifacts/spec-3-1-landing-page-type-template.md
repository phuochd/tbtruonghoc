---
title: 'Story 3.1: Landing Page type & paid-ads template'
type: 'feature'
created: '2026-09-28'
status: 'done'
baseline_commit: '8d7c8abe71dc171bc9bdc698d718360e7717a240'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Site B has no page type for paid-ad traffic. Ads can only point at organic catalog pages, which carry the nav, the 3-segment sticky bar and no single conversion goal. The Tết 2027 wine-barrel campaign needs a reusable, single-purpose page.

**Approach:** Add a standalone `LandingPage` page type (AD-2/AD-5). It holds hero fields, prose blocks and its own repeatable variant+price region (structure only; Story 3.2 renders the cards). A chrome-less template (`HideSiteChrome`) shows a non-link brand mark, hero, blocks, the inline quote form with phone/Zalo alternatives, and one fixed full-width CTA that jumps to the form. A save hook keeps every landing page out of the nav. A draft thùng rượu gỗ instance is seeded on Site B.

## Boundaries & Constraints

**Always:**
- **Regions:** `Eyebrow` (string), `HeroImage` (image), `Intro` (text), `CtaLabel` (string), `Variants`: `IList<LandingVariant>` with `Image`, `Name`, `Label` (chip), `Price` (free text, like `ProductPost.Price`). Blocks are enabled. Every field is optional, and a blank field renders nothing.
- **Nav exclusion:** an `App.Hooks.Pages.RegisterOnBeforeSave` hook forces `IsHidden = true` whenever `TypeId == nameof(LandingPage)`. The editor cannot un-hide it. The page still resolves at its permalink.
- **Template order:** brand mark "Trống Đọi Tam" (plain text, no link) → eyebrow → h1 → hero image (alt: media `AltText`, then the title) → intro → blocks → form section `id="dat-hang"` (h2, `_QuoteRequestForm` with `ProductOfInterest` = page title and `FormType = "landing"`, then a phone/Zalo line from `SiteSettings` via `ContactLinks`, where each link is omitted when unset) → the fixed CTA.
- **CTA label decision (2026-09-28, Phước: editor-set).** The fixed CTA's text is `CtaLabel`, trimmed; blank → "Nhận báo giá". The region description tells editors to use "Đặt mua ngay" only once the page shows real prices. Its accessible name is the label plus " – đến form đặt hàng".
- **Lead type decision (2026-09-28, Phước: new `landing` type).** The page's form posts `formType=landing`. Add `landing` to the `LeadsController` allow-list, with the email label "Landing page quảng cáo" in `LeadEmailComposer`. The Leads Manager already shows the raw `formType`, so it is unchanged. The form type is a constant in `LandingPage` (e.g. `LandingPage.FormType`).
- **Fixed CTA:** one full-width `<a href="#dat-hang">`, fixed to the viewport bottom and always visible. It uses the `landing-page-cta` tokens (`primary`/`on-primary`, `cta` type, `radius-sm`) and has a touch target ≥ 56px. The `<main>` bottom padding clears it (bar height + safe-area), and the layout stays unchanged.
- `ViewData["HideSiteChrome"] = true`, so there is no nav, footer or 3-segment bar. GA4 and consent stay. `_MetaTags` in the head.
- **Seed:** a draft `LandingPage` on Site B with slug `thung-ruou-go-qua-tet` and title "Thùng rượu gỗ – Quà Tết từ làng nghề Đọi Tam", idempotent per slug, following `CraftsmanStorySeed`. No prices, variants or prose.
- **Indexing decision (2026-09-28, Phước: noindex).** The seed sets `MetaIndex = false` so the ad page never competes with the organic Thùng rượu gỗ category page. The editor can change it in the SEO tab. Instances created by editors keep Piranha's default.
- Mộc Trầm tokens only, mobile-first.

**Never:**
- Rendering `Variants` or any price (Story 3.2). Hardcoded or invented prices, variant names or phone numbers.
- Cart, checkout, login, countdowns, auto-rotating carousels, modals, exit-intent popups.
- Changing `_LayoutTrongDoiTam.cshtml`, `_QuoteRequestForm`, `lead-form.js`, `_SiteBNav`, Site A views or `Areas/Manager`. Modeling a product as a landing page.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Published landing page | Site B, published, `IsHidden` saved as false | 200 at its permalink. No `sb-nav`, `sb-footer` or `sb-contact-bar`. `sb-lp__cta` → `#dat-hang`. Form present. Absent from the nav on other Site B pages | Hook re-hides it |
| Bare page | Title only | Brand, h1, form and CTA. No eyebrow, `<img>` or intro | N/A |
| Hero + intro | Image (no AltText) + intro set | `<img>` with alt = title, intro text encoded | N/A |
| Variants filled | 2 variants with prices | No price or variant name in the HTML (3.2 renders them) | N/A |
| CTA label | `CtaLabel` blank / "Đặt mua ngay" | Text "Nhận báo giá" / "Đặt mua ngay", encoded | N/A |
| Landing lead | POST `/api/leads` with `formType=landing` | Stored as `landing`; the email reads "Loại form: Landing page quảng cáo" | Unknown type still → `general` |
| Seeded page | Fresh start | One draft, slug `thung-ruou-go-qua-tet`, `MetaIndex=false`, hidden; a second run adds nothing | N/A |
| Contact settings blank | Phone and Zalo unset | No alternatives line and no empty `tel:` link | N/A |
| Draft | Unpublished | 404 for anonymous visitors | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Models/CraftsmanStoryPage.cs` -- pattern for the new `Models/LandingPage.cs`: `[PageType(Title = "Landing page (quảng cáo)")]`, `[ContentTypeRoute(Title = "Default", Route = "/landingpage")]`, trimmed-null helpers, a nested item class like `StoryPhoto`. Vietnamese region titles and descriptions. The `Variants` description says the cards show in Story 3.2 and prices come from the client.
- `src/TbTruongHoc.Web/Controllers/CmsController.cs` -- add a `[Route("landingpage")]` action, a copy of `CraftsmanStory`.
- `src/TbTruongHoc.Web/Views/Cms/CraftsmanStory.cshtml` -- view pattern (`_MetaTags`, `ImageAltFallback` set and removed around blocks, `ResizeImage`, `SiteSettings` + `ContactLinks.TelHref`/`SafeUrl`). New `Views/Cms/LandingPage.cshtml`. Load `lead-form.js` as `ProductPost.cshtml:131` does. Zalo link: `target="_blank" rel="noopener noreferrer"`, and every link has an accessible name as in `_StickyContactBar`.
- `src/TbTruongHoc.Web/Views/Shared/_LayoutTrongDoiTam.cshtml` -- already honours `HideSiteChrome` (drops nav, footer, bar and `sb-has-bar`). Do not edit it.
- `src/TbTruongHoc.Web/Program.cs:179` -- existing `App.Hooks.SiteContent.RegisterOnBeforeSave` style. Add the Pages hook next to it. Around line 315: `LandingPageSeed.EnsureSeededAsync` after `CraftsmanStorySeed`.
- `src/TbTruongHoc.Web/Data/CraftsmanStorySeed.cs` -- copy to `Data/LandingPageSeed.cs`.
- `src/TbTruongHoc.Web/Controllers/LeadsController.cs:37` -- `AllowedFormTypes`: add `"landing"` and update the doc comment. `Models/LeadSubmissionRequest.cs:31` -- doc comment. `Notifications/LeadEmailComposer.cs:84` -- `FormTypeLabel` case. `LeadDbContext` column is 50 chars, so no migration is needed. Existing tests: `LeadSubmissionTests.cs`, `LeadNotificationTests.cs`.
- `src/TbTruongHoc.Web/wwwroot/assets/css/site-b.css` -- add `.sb-lp*` near `.sb-contact-bar` (~1124). Reuse `--sb-bar-shadow` and `env(safe-area-inset-bottom)` as the bar does.
- `tests/TbTruongHoc.Web.Tests/HideChromeProbeController.cs` -- keep it. `CraftsmanStoryTests.cs` -- reuse the helpers (`GetHtmlAsync`, `HostnameOf`, TinyPng media upload, per-test create/delete in `finally`).

## Tasks & Acceptance

**Execution:**
- [x] `Models/LandingPage.cs` -- type, `LandingVariant`, helpers.
- [x] `Program.cs` -- nav-hiding hook + seed call.
- [x] `Controllers/LeadsController.cs` + `Notifications/LeadEmailComposer.cs` + `Models/LeadSubmissionRequest.cs` -- `landing` form type.
- [x] `Data/LandingPageSeed.cs` -- draft seed.
- [x] `Controllers/CmsController.cs` + `Views/Cms/LandingPage.cshtml` -- action + template.
- [x] `wwwroot/assets/css/site-b.css` -- `.sb-lp*` styles.
- [x] `tests/TbTruongHoc.Web.Tests/LandingPageTests.cs` -- one test per matrix row, the hook (saved `IsHidden=false` reads back true), seed idempotency, a `landing` lead through the API and the email label, and page-wide rules on every render.

**Acceptance Criteria:**
- Given any landing-page render, then every `<img>` has a non-empty alt, and there is no `<iframe`, `autoplay`, "giỏ hàng" or login UI.
- Given a second `LandingPage` created in the Manager with a new slug, when published, then it renders with the same template with no code change.
- Given the full suite, when run, then all pre-existing tests pass.

## Implementation Notes

## Spec Change Log

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V).

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| B1 | `.sb-lp` bottom padding overridden, so the fixed CTA covers the submit button | high | Confirmed: `.sb-main main` (site-b.css:516, 0,1,1) beats `.sb-lp` (0,1,0), leaving 32px → patch (raise specificity). |
| B2 | Leads from different campaigns can't be told apart (no page id/utm) | low | Only one campaign exists; ad attribution lives in GA4 (epic: measurement only); the prefilled title identifies the page; the fix adds form fields → rejected, surfaced at presentation. |
| B3 | Manager help text mentions "Story 3.2" | low | Confirmed in the Variants region Description; direct correction → patch. |
| B4/E3/E6 | Blocks allow any type, so an editor can add iframes, autoplay or escape links (PageBlock "Read more") | low | Editor content, same as the story page and PDP; editors are unlikely to paste embeds; restricting block types adds surface → rejected. |
| B5 | No test for authenticated draft preview | low | The action is a verbatim copy of the tested `CraftsmanStory` pattern; V found no gap → rejected. |
| B6 | Hero served at one 1140px size (LCP on a mobile ad page) | low | Every mobile visitor pays it; `srcset` is a small, surface-free change → patch. |
| B7 | "Đặt mua ngay" allowed before prices render | false | Frozen CTA-label decision: editor-set, with guidance in the description. |
| B8 | Fixed CTA stays visible while the form is in view | false | Frozen: "fixed to the viewport bottom and always visible". |
| B9 | Brand name hardcoded; a LandingPage on Site A would misrender | low | Unlikely editor action; the guard adds a branch (same call as 2.5 B5/E8) → rejected. |
| B10 | `"landing"` and the seed slug duplicated as literals | low | Confirmed in `LeadEmailComposer` and the tests; direct correction → patch. |
| B11 | The Manager "hidden" toggle silently reverts | low | Cosmetic; the nav exclusion is the intended behavior; no natural spot for the text → rejected. |
| V1 | GA4/consent equality asserts can never fail | low | Test host is Development, where GA4 never loads; real coverage is in `SiteBShellTests` → patch (delete). |
| E1 | Deleting or renaming the seeded slug re-seeds a draft | low | Same accepted per-slug behavior as 2.3/2.5; the re-seed is a hidden, noindex draft → rejected. |
| E2 | Landing page dragged to SortOrder 0 becomes the start page | low | Unlikely editor action; the guard adds a branch → rejected. |
| E4 | Long CTA label clipped by fixed height + `overflow:hidden` | low | Confirmed in the CSS; direct correction (`min-height`) → patch. |
| E5 | Whitespace-only MetaTitle gives a blank `<title>` | low | Pre-existing pattern in every Cms view; unlikely → rejected. |

## Verification

**Commands:**
- `dotnet test --artifacts-path bin/test-art` (repo root, MariaDB up) -- expected: all tests pass.
- `dotnet build` -- expected: 0 new warnings.

**Manual checks:**
- Publish the seeded page with a hero image and an intro. At 375px and 1280px: no nav, footer or 3-segment bar; the fixed CTA scrolls to the form and never covers the submit button; the page is absent from the Site B nav; a test lead appears in the Leads Manager.
