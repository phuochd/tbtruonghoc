---
title: 'GA4 script gated by environment and cookie consent (Story 1.10)'
type: 'feature'
created: '2026-09-25'
status: 'done'
baseline_commit: 'a39f1e6d1118e6c4160371b6981d51687175234b'
route: 'dispatch'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `_Analytics.cshtml` emits the gtag.js snippet whenever a valid GA4 ID is set, in any environment and before the visitor has agreed to anything. A real ID copied into a dev/staging database pollutes production analytics, and visitors are tracked with no consent step.

**Approach:** Gate GA4 in code. Outside `Production`, never emit anything GA4-related. In Production with a valid ID, render a consent banner instead of gtag.js. A small static JS file injects gtag.js only after the visitor accepts, and on later visits if acceptance is stored. The Search Console verification meta tag is not tracking and stays ungated.

## Boundaries & Constraints

**Always:**
- The environment check is `IWebHostEnvironment.IsProduction()`, evaluated in the view on every render. Nothing in `SiteSettings` or config can override it.
- No request to `googletagmanager.com` or `google-analytics.com` before consent. No `<script src=…gtag…>` in server HTML, so gtag.js is loaded only by the consent JS after acceptance.
- The banner and consent JS render only when GA4 would actually fire (Production + valid ID). With no tracking there is nothing to consent to.
- Keep `SiteSettingsValidation.IsValidGa4MeasurementId` as the ID check. The ID reaches JS only via an HTML-encoded `data-` attribute, never interpolated into inline script.
- Plain dependency-free JS in `wwwroot/assets/js/`, same style as `lead-form.js`. Banner buttons are real `<button>`s, keyboard reachable, and the banner has an accessible label (WCAG 2.1 AA floor).
- Consent is stored per site. Each site is its own hostname, so origin-scoped storage already separates them.
- **Decision (Phước, 2026-09-25): banner.** A fixed bottom bar with short Vietnamese text (e.g. "Chúng tôi dùng cookie Google Analytics để hiểu cách khách truy cập website…") and two equal-weight buttons, **Đồng ý** and **Từ chối**. It uses the current Bootstrap scaffold so later epics can restyle it per site.
- **Decision (Phước, 2026-09-25): persistence.** Both accept and decline are remembered for 180 days, with a timestamp stored so the choice expires. A small "Cài đặt cookie" link in the footer, rendered only when the gate passes, reopens the banner so the visitor can change their mind. Changing from accept to decline stops gtag.js being injected on later page loads. The already-loaded page is not un-tracked.

**Never:**
- No Google Consent Mode "default denied" approach: it still sends cookieless pings pre-consent, violating the AC.
- No third-party CMP library, no server-side consent cookie, no Manager-editable banner text (out of scope).
- No IP-anonymization or other GA4 config changes beyond gating.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Non-production, valid ID | `Development`/`Staging`, `Ga4MeasurementId=G-XXXX` | No banner, no consent JS, no gtag markup; verification meta still renders if set | N/A |
| Production, no/invalid ID | ID empty or fails validation | No banner, no consent JS, no gtag | N/A |
| Production, first visit | valid ID, no stored choice | Banner visible; zero GA4 requests | N/A |
| Visitor accepts | clicks accept | Choice stored; banner hidden; gtag.js injected and `config` sent for that site's ID | N/A |
| Returning, accepted | stored = granted | No banner; gtag.js injected on load | N/A |
| Returning, declined | stored = denied, < 180 days | No banner, no GA4 request | N/A |
| Stored choice expired | stored > 180 days old | Treated as first visit: banner shown | N/A |
| Visitor reopens settings | clicks footer "Cài đặt cookie" | Banner shown again; new choice overwrites old | N/A |
| Visitor declines / ignores | clicks decline, or no click | No GA4 request for the page/session | N/A |
| Storage unavailable | `localStorage` throws (private mode) | Banner still works for the current page; choice just isn't remembered | try/catch, fail closed (no tracking) |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Views/Shared/_Analytics.cshtml` -- currently emits verification meta + inline gtag. Keep the meta. Replace the inline gtag with the gated consent-JS include.
- `src/TbTruongHoc.Web/Views/Shared/_Layout.cshtml` -- `_Analytics` partial goes in `<head>`. The banner markup needs `<body>`: add a new partial before the bootstrap script, plus the "Cài đặt cookie" footer link.
- `src/TbTruongHoc.Web/Models/SiteSettingsValidation.cs` -- `IsValidGa4MeasurementId`: reuse, do not modify.
- `src/TbTruongHoc.Web/wwwroot/assets/js/lead-form.js` -- style reference for the new JS (IIFE, `'use strict'`, `ready()` helper).
- `tests/TbTruongHoc.Web.Tests/AnalyticsSearchConsoleTests.cs` -- `Each_Site_Renders_Only_Its_Own_Ga4_And_Verification_Values` asserts the inline `gtag('config', …)` under the factory's `Development` env. It will fail by design. Rework it to flip env to Production and assert the new markup (each site's own ID in the `data-` attribute, never the other's).
- `tests/TbTruongHoc.Web.Tests/PiranhaWebApplicationFactory.cs` / `PiranhaAppCollection.cs` -- only ONE factory can exist per process (Piranha `App.Init` is process-static), so a separate Production-env factory is impossible. Instead, tests temporarily set `IWebHostEnvironment.EnvironmentName = "Production"` on `_factory.Services` and restore it in `finally`. The collection runs serially, and nothing else reads the env name per-request.
- `tests/TbTruongHoc.Web.Tests/ManagerPageTabsScriptTests.cs` -- Jint pattern (stub `window`/`document`, execute the real file) to reuse for testing the consent JS.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Models/AnalyticsGate.cs` -- static `ShouldLoadGa4(IWebHostEnvironment env, string? ga4Id)` = `env.IsProduction() && IsValidGa4MeasurementId(ga4Id)` -- one gate shared by both partials.
- [x] `src/TbTruongHoc.Web/Views/Shared/_Analytics.cshtml` -- keep verification meta; when the gate passes, emit `<script src="~/assets/js/analytics-consent.js" data-ga4-id="…" defer asp-append-version="true">`; otherwise nothing.
- [x] `src/TbTruongHoc.Web/Views/Shared/_CookieConsent.cshtml` + `_Layout.cshtml` -- banner markup (initially `hidden`), rendered only when the gate passes; include it in the layout body.
- [x] `src/TbTruongHoc.Web/wwwroot/assets/js/analytics-consent.js` -- read stored choice (try/catch); if granted, inject gtag.js + `config`; else reveal banner; wire accept/decline (store `{choice, timestamp}`, 180-day expiry) and the footer "Cài đặt cookie" link to re-show the banner. Expose a small `window.analyticsConsent` surface for Jint tests.
- [x] `tests/TbTruongHoc.Web.Tests/AnalyticsSearchConsoleTests.cs` -- rework the per-site fact under Production; add facts for Development-with-valid-ID (no GA4 markup, verification still present) and Production-with-empty-ID (no banner).
- [x] `tests/TbTruongHoc.Web.Tests/AnalyticsConsentScriptTests.cs` -- Jint facts for the matrix's first-visit / accept / returning / decline / storage-throws rows plus expired-choice (>180 days → banner again) and reopen-via-footer-link, asserting whether a gtag.js script element was injected.

**Acceptance Criteria:**
- Given any non-Production environment with a valid GA4 ID saved, when a page on either site renders, then its HTML contains no gtag/consent markup.
- Given Production with a valid ID, when a page renders, then its HTML contains no `googletagmanager.com` URL, only the consent JS include with that site's own ID.
- Given the full suite, when `dotnet test` runs, then all tests pass and the environment name is restored after each test.

## Implementation Notes

- Implemented directly in the main session (no implementation subagent), on branch `story/1-10-ga4-env-consent-gate`.
- `AnalyticsGate.ShouldLoadGa4` takes `IHostEnvironment` (the base of `IWebHostEnvironment`) so it can be unit-tested with a stub. Both `_Analytics.cshtml` and `_CookieConsent.cshtml` `@inject IWebHostEnvironment` and call it on every render.
- `_Analytics.cshtml` now emits only `<script src="…/analytics-consent.js?v=…" data-ga4-id="…" defer>` when the gate passes. The old inline gtag snippet is gone. The verification meta is unchanged.
- `_CookieConsent.cshtml` renders a `<footer>` holding a "Cài đặt cookie" `<button class="btn btn-link">` (a button, not an `<a>`, since it performs an action), plus the banner (`role="region"`, `aria-label="Thông báo cookie"`, starts `hidden`). The banner root deliberately has no `d-*` utility: Bootstrap's `d-flex` is `!important` and would override `[hidden]`. Flex layout lives on an inner container. Styling uses Bootstrap utilities only (`position-fixed bottom-0 … z-3 bg-dark`), with no SCSS changes.
- `analytics-consent.js` reads `document.currentScript` synchronously, before the `ready()` callback, where it would be null. It stores `{ choice, at }` under `analyticsConsent` and treats unknown, malformed, timestamp-less or older-than-180-days values as "no choice" (banner shown). `loadGa4` is idempotent. `gtag` pushes the real `arguments` object, as gtag.js requires. The banner does not take focus on page load; reopening via the footer moves focus to **Đồng ý**.
- Added `tests/TbTruongHoc.Web.Tests/AnalyticsGateTests.cs` (not in the task list). The matrix row "Production, invalid ID" cannot be reached over HTTP, because the OnBeforeSave hook refuses to persist an invalid ID, so the gate is covered directly (8 cases, including `Staging` and a near-miss env name).
- The Development/Staging fact renders both sites under both environments and also asserts that neither saved ID appears anywhere in the HTML.
- Build note: the user's own `dotnet run` process (PID 20652) locked `bin/Debug/net8.0/TbTruongHoc.Web.exe`, so verification ran with `--artifacts-path bin/story-1-10` (git-ignored via `bin/`) instead of stopping that process.
- Verified: `dotnet build TbTruongHoc.sln --artifacts-path bin/story-1-10` gave 0 errors (the only warnings are the pre-existing CS8632s in `Program.cs` and the Piranha NuGet advisories). `dotnet test … --no-build` against the real MariaDB: **138/138 passed** before review; **146/146 passed** after the review patches (8 more Jint cases plus the markup-contract asserts). Each new/reworked analytics fact was confirmed by name in a filtered run: 8 gate cases, 16 Jint cases and 3 reworked/new HTTP facts.
- Not done: a manual browser check in Production (DevTools Network tab). This is listed under Verification → Manual checks.

## Spec Change Log

## Review Triage Log

Layers run: `blind-hunter`, `edge-case-hunter`, `verification-gap` (all three reported). No intent_gap/bad_spec entries, so there was no loopback. Patches were applied in the main session, since there was no implementation subagent to re-engage.

- **[medium, patch]** (blind-hunter + edge-case-hunter) `readChoice` let a future or non-finite `at` bypass the 180-day expiry, because `Date.now() - at` is negative or NaN and never exceeds `MAX_AGE_MS`. A clock moved back or a tampered value kept a choice forever, contrary to the frozen 180-day decision. Fixed: `!isFinite(age) || age < 0 || age > MAX_AGE_MS` counts as "no choice". New Jint theory covers `+1e12` and `+60 s`.
- **[medium, patch]** (blind-hunter + edge-case-hunter) After reopening via "Cài đặt cookie", choosing hid the focused button and focus fell to `<body>`, a keyboard/SR regression against the spec's WCAG floor. Fixed: a `reopened` flag makes `hideBanner` return focus to the reopen button. First-visit choices still never move focus. Two new Jint facts.
- **[low, patch]** (blind-hunter + edge-case-hunter) The fixed bottom banner covered the end of every page, including the contact block and footer link, until the visitor chose. Fixed: `showBanner` pads `body` by the banner's `offsetHeight` and `hideBanner` clears it. `style.min.css` sets no `padding-bottom` on `body`, so clearing restores the stylesheet value. New Jint fact.
- **[low, patch]** (blind-hunter) The server-rendered "Cài đặt cookie" button did nothing when the consent JS was blocked or failed to load, which is plausible given ad-block filters that match "analytics" paths. Fixed: rendered `hidden`, revealed in `init()` once wired. New Jint fact, and the HTTP test asserts the server renders it `hidden`.
- **[medium, patch]** (verification-gap, pre-verified; blind-hunter concurred) The HTTP tests did not lock the markup contract the JS depends on: the accept/decline hooks, the banner's initial `hidden`, `role`/`aria-label`, the script `src`/`defer`. A rename or dropped attribute would have kept the suite green. Fixed: an `AssertConsentMarkup` regex helper, applied to both sites' Production HTML.
- **[low, patch]** (blind-hunter + edge-case-hunter) The `AnalyticsGate` doc claimed the two partials "can never disagree", but each reads `SiteSettings` separately. Verified: a save between the two reads is possible in theory, and either half alone fails closed (a hidden banner with no script, or a script with no banner). Fixed: the comment now says exactly that. Caching the gate per request was rejected as added machinery for a benign race.
- **[low, patch]** (blind-hunter) There was no test that a choice just under 180 days is still honoured, so a unit error that expired choices early would have passed. Added a 179-day fact.
- **[low, defer]** (blind-hunter) The banner has no link to a privacy/cookie policy. No such page exists on either site yet, so the missing policy page is pre-existing content work, not caused by this change. Logged in deferred-work.md.
- **[low, rejected]** (blind-hunter) The `_ga`/`_ga_<ID>` cookies stay after declining post-accept. They are real but inert: gtag.js is never injected again, so nothing reads or sends them. The frozen decision already scopes withdrawal to "stops gtag.js on later loads". Deleting GA cookies across cookie-domain variants is new machinery for a rare path. Rejected.
- **[low, rejected]** (blind-hunter) `z-3` could let other fixed elements paint over the banner. Verified that no bottom-anchored fixed element exists: the only fixed element is the top navbar (`fixed-top`, z 1030), which does not overlap. Rejected as a hypothetical.
- **[low, rejected]** (blind-hunter) Private-mode visitors see the banner on every page load. This is the frozen matrix row's intended behavior ("choice just isn't remembered", fail closed).
- **[false]** (blind-hunter) "Second `<footer>` landmark." `_Layout.cshtml` has no footer and `_ContactBlock` renders a `<div>`, so this partial's `<footer>` is the only one.
- **[false]** (blind-hunter) "`sprint-status.yaml` says in-progress while the spec says in-review." The sprint file is advanced by step-05's own status sync. It is not stale at review time.
- **[false]** (blind-hunter + verification-gap other) "Flipping the `IWebHostEnvironment` singleton may affect per-request code." Grep shows the only other environment read is `Program.cs:94` (`IsDevelopment()` at startup). No `<environment>` tag helper is used in any view. The flip is restored in `finally`, and the shared collection is serial.
- **[false]** (blind-hunter) "`ga4Id is not null` is redundant." `IsValidGa4MeasurementId(string value)` is declared non-nullable under `#nullable enable`, so the guard is what keeps the call warning-free.
- **[false]** (edge-case-hunter) "`document.currentScript` null → accept stores granted with no tracking, suppressing the banner for 180 days." `currentScript` is set for classic `defer` scripts in every supported browser. Even if it were null, the stored "granted" is the visitor's real consent, and the next load with a readable ID honours it by loading GA4. Consent is not suppressed.
- **[low, rejected]** (blind-hunter) "No test that `EnvironmentName` is restored after the helper throws." The restore is an unconditional `finally` in a four-line helper. A test of the test helper adds nothing.

## Design Notes

Env flip in tests (serial collection, restore in `finally`):

```csharp
var env = _factory.Services.GetRequiredService<IWebHostEnvironment>();
var previous = env.EnvironmentName;
env.EnvironmentName = Environments.Production;
try { /* render + assert */ } finally { env.EnvironmentName = previous; }
```

## Verification

**Commands:**
- `dotnet build TbTruongHoc.sln` -- expected: 0 errors.
- `docker compose up -d mariadb` then `dotnet test TbTruongHoc.sln` -- expected: all pass (baseline plus the new facts).

**Manual checks:**
- Run with `ASPNETCORE_ENVIRONMENT=Production` and a GA4 ID set. The DevTools Network tab shows no Google request until you accept, and after reload with consent stored, gtag.js loads without the banner.
