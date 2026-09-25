---
title: 'Per-site tabs in Piranha Manager page list (Story 1.8, AD-1)'
type: 'feature'
created: '2026-09-25'
status: 'done'
route: 'dispatch'
baseline_commit: 'b4f7f3daab594fbbab04c8765820b86381ed14ed'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Manager's Pages screen (`~/manager/pages`) stacks every Site's page tree one below another, so an editor working on Site B must scroll past all of Site A's pages, and nothing shows at a glance how many sites exist. Investigation found Piranha 12.0.0 has no site dropdown on this screen; the "stock site switcher" the story refers to is this stacked multi-site list.

**Approach:** Override Piranha's `PageList.cshtml` with an app-local Razor page at the same path. It renders one Bootstrap tab per entry in the existing `sites` array (from `GET manager/api/page/list`) and shows only the active site's header row (site edit link + "Add page") and page tree. A small script holds the active site id client-side. Piranha's own `piranha.pagelist` Vue app, API and JS stay untouched.

## Boundaries & Constraints

**Always:** Tabs come only from the API's `sites` array, in its order (default site first). No config list. Adding or removing a Site changes the tabs after the next load. Tab label = `site.title`. Switching tabs is client-side only, with no reload and no refetch. Inactive trees are hidden with `v-show`, never `v-if`, because Piranha's `bind()` attaches nestable drag-drop only to `.sitemap-container` elements already in the page when a load finishes. The active tab survives `load()` re-runs (after delete, move or site edit). If the active id no longer exists, fall back to the first site. Decision (2026-09-25): every full page load starts on the default (first) site; the active tab is not persisted in any browser storage. Keep the page's `[Authorize(Policy = "PiranhaPages")]` model (`PageListViewModel`), toolbar actions, `_PageAddModal`, `_SiteModal`, and every script Piranha's page includes, in the same order. Tabs are keyboard-operable (`role="tablist"`/`tab`, `aria-selected`).

**Never:** No change to or copy of `piranha.pagelist.js` or any Piranha API. No search/filter inside a tab (explicitly out of scope). No second route claiming `~/manager/pages`, which would throw `AmbiguousMatchException`. No server-side per-site fetch. The Add-page modal's Copy-tab site dropdown stays: it picks a copy *source*, not the viewed site.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Two sites | Site A (default) + Site B | 2 tabs, Site A active; only Site A's tree visible | N/A |
| Switch tab | Click "Site B" | Site B's header + tree shown, Site A's hidden; no network request | N/A |
| Reload after action | Delete a page while on Site B tab | After `load()`, Site B tab still active | N/A |
| Active site removed | Active id missing from new `sites` | First site becomes active | N/A |
| Site added | Toolbar "Add site" completes | New tab appears after reload | N/A |
| Drag-drop | Reorder pages in Site B tab | Move persists; stays on Site B tab | Piranha's own |
| Anonymous | GET `/manager/pages` without login | 302 to login (not 500) | N/A |

</frozen-after-approval>

## Code Map

- Piranha source reference: `ilspycmd -p -o <dir> %USERPROFILE%\.nuget\packages\piranha.manager\12.0.0\lib\net8.0\Piranha.Manager.dll`. Base the override on `AspNetCoreGeneratedDocument/Areas_Manager_Pages_PageList.cs` (the markup to reproduce) and `piranha.pagelist.js` (embedded resource: data `sites`, `loading`; methods `load`, `bind`, `add(siteId, pageId, after)`; `updated()` calls `bind()`).
- `src/TbTruongHoc.Web/Areas/Manager/Pages/PageList.cshtml` -- NEW override. Header: `@page "~/manager/pages"`, `@model Piranha.Manager.Models.PageListViewModel`, `@using Piranha; Piranha.Manager; Piranha.Manager.Editor; Piranha.Manager.Extend`, `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`, `@inject ManagerLocalizer Localizer`, explicit `Layout = "~/Areas/Manager/Shared/_Layout.cshtml"` (the RCL `_ViewImports` does not reach app files). Keep `ViewBag.Title = Localizer.Page["Pages"]`, `ViewBag.MenuItem = "Pages"`, the `script` section (EditorScripts Main/Editor, components, pagelist, siteedit with `?v=@Utils.GetAssemblyVersionHash(typeof(Piranha.Manager.Module).Assembly)`, then `piranha.pagelist.load();`), the `partials` section, `Actions.Toolbars.PageList` toolbar, `<partial name="Partial/_PageAddModal" />` inside `#pagelist`, `_SiteModal` outside it.
- `src/TbTruongHoc.Web/Areas/Manager/Pages/Leads.cshtml` -- style reference for app-local Manager pages.
- `src/TbTruongHoc.Web/wwwroot/assets/js/manager-page-tabs.js` -- NEW. Defines `window.managerPageTabs = Vue.observable({ activeId: null })` plus `isActive(siteId, sites)` and `select(siteId)` helpers. Must load **before** `piranha.pagelist.min.js`. The in-DOM template reaches it as a global, the same way it already calls `piranha.permissions...`.
- `tests/TbTruongHoc.Web.Tests/LeadManagerTests.cs` -- pattern for anonymous-302 tests (`AllowAutoRedirect = false`, `[Collection(PiranhaAppCollection.Name)]`). Tests never log into Manager.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/wwwroot/assets/js/manager-page-tabs.js` -- observable state + helpers; active-id fallback to `sites[0]` -- keeps Piranha's Vue app unmodified.
- [x] `src/TbTruongHoc.Web/Areas/Manager/Pages/PageList.cshtml` -- the override: tab strip (`v-for="site in sites"`) above the sitemap; each site `<li>` wrapped with `v-show="managerPageTabs.isActive(site.id, sites)"` -- the core feature.
- [x] `tests/TbTruongHoc.Web.Tests/ManagerPageTabsTests.cs` -- (a) anonymous GET `/manager/pages` → 302 to login (proves no ambiguous route); (b) the app's `EndpointDataSource` has exactly one endpoint whose route is `manager/pages`, and its `PageActionDescriptor.RelativePath` is `/Areas/Manager/Pages/PageList.cshtml`, served from the TbTruongHoc.Web assembly -- proves the override won.
- [x] `tests/TbTruongHoc.Web.Tests/ManagerPageTabsScriptTests.cs` (+ `Jint` test package) -- runs the real `manager-page-tabs.js` under Jint with a stubbed Vue, covering the tab-selection matrix rows (two sites, switch, reload, drag-drop transient `sites = []`, site removed, site added) plus keyboard -- added at the step-03 matrix audit, since those rows had no automated test.

**Acceptance Criteria:**
- Given the full suite runs against real MariaDB, when `dotnet test` finishes, then all existing tests plus the new ones pass.
- Given an editor logged into Manager, when they open Pages, switch tabs, add, drag, delete a page and edit a site, then each works on the active tab exactly as before, with no browser console errors.

## Implementation Notes

- Test (b), "served from the TbTruongHoc.Web assembly": `Program.cs` sets Piranha's `AddRazorRuntimeCompilation = true`, so in Development the page loaded for the endpoint is recompiled from the app's `.cshtml` into a dynamic assembly, not `TbTruongHoc.Web.dll`. The test therefore checks two things: at build time, the first `ViewsFeature` descriptor for `/Areas/Manager/Pages/PageList.cshtml` (the one MVC uses) comes from `TbTruongHoc.Web`, ahead of Piranha.Manager's copy; at runtime, the loaded page type is not from `Piranha.Manager` and its model is `PageListViewModel`.
- `managerPageTabs.resolveId(sites)` is pure and never writes the fallback back to `activeId`. Piranha's drag-drop callback briefly sets `sites = []` before restoring them, and writing back at that moment would lose the selected tab.
- Tabs follow the WAI-ARIA tabs pattern: roving `tabindex`, and Left/Right/Home/End move the selection and focus.
- Matrix audit: rows 1–6 are client-side state, now covered by `ManagerPageTabsScriptTests` (Jint 4.16.3, no DB). Row 7 (anonymous 302) is covered by `ManagerPageTabsTests`. The rendered DOM (v-show wiring, drag-drop on the visible tree) still needs the logged-in manual check. Verified: `dotnet test --artifacts-path .artifacts-test` 106/106 against real MariaDB.
- Review patches applied (see Review Triage Log). The `PiranhaPages` policy assert reads `CompiledPageActionDescriptor.EndpointMetadata` (loaded through `PageLoader`), not `endpoint.Metadata`: the attribute sits on the page model class and only appears on the compiled descriptor. Final verification: build has 0 errors, `dotnet test` 112/112 against real MariaDB.

## Spec Change Log

- 2026-09-25, walkthrough manual check: the Pages screen rendered nothing ("Cannot read properties of undefined (reading 'isActive')"). Vue 2 in-DOM templates resolve identifiers against the instance, not `window`, so the global `managerPageTabs` was undefined. Fix: `manager-page-tabs.js` also sets `Vue.prototype.managerPageTabs`, and a script test pins it. The Jint stub could not catch this because it has no template scoping. After the fix, all five manual checks and a site add/delete passed in the browser.

## Review Triage Log

- **[medium, patch]** (verification-gap + blind-hunter, one root cause: the JS-state-to-template wiring has no automated guard) The `Vue.observable` stub is the identity function, so dropping reactivity would fail no test. Script order (tabs script before `piranha.pagelist.min.js`) and `v-show` (not `v-if`) on the site `<li>` exist only in comments. Fix: the stub records the `observable` call, plus a static markup test on the `.cshtml`. The real logged-in render stays with the manual check.
- **[medium, patch]** (blind-hunter) The override copies Piranha.Manager 12.0.0 markup, and nothing flags an upgrade. A Piranha upgrade is expected soon (.NET 8 EOL 2026-11-10), after which the stale override would keep winning silently. Fix: a test pinning the Piranha.Manager assembly version to 12.0.0, with a "re-sync the override" message.
- **[low, patch]** (edge-case) `onKeydown` ignores modifier keys, so Alt+ArrowLeft/Right on a focused tab is swallowed (`preventDefault`) and switches the tab instead of going browser Back/Forward. Fix: return early when a modifier key is held.
- **[low, patch]** (blind-hunter) The tablist has a hard-coded English `aria-label="Sites"`; every other string goes through `Localizer`. Fix: `@Localizer.Menu["Sites"]`.
- **[low, patch]** (blind-hunter) Keyboard test gaps: no ArrowRight wrap from the last tab, no test with an empty sites list. Fix: add those cases (plus the modifier case above).
- **[low, patch]** (blind-hunter) The anonymous-302 test does not prove the override kept Piranha's `PiranhaPages` policy. Fix: assert that an `AuthorizeAttribute` with Policy "PiranhaPages" is in the endpoint metadata.
- **[false]** (verification-gap) The claim that `ManagerPageTabsScriptTests.cs` being untracked leaves the coverage out of the repo. It is a working-tree state, not a code defect: the story commit stages every story file, and review is forbidden to `git add`.
- **[false]** (blind-hunter) The claim that the missing `key` on the site `<li>` lets Vue reuse panel DOM across sites. This is identical to stock Piranha's markup (the key sits on the inner div there too); `v-show` does not change patching, and `bind()` re-runs over every container after each `load()`.
- **[false]** (blind-hunter + verification-gap) The claim that the literal `data-id="{ site.id }"` is harmful. It is copied verbatim from Piranha 12.0.0, and `piranha.pagelist`'s `bind()` groups containers by index (`group: i`) and never reads `data-id`.
- **[low, rejected]** (blind-hunter) `role="tabpanel"` on an `<li>` is not allowed by ARIA-in-HTML. Moving the role onto a new wrapper div changes the DOM structure that Piranha's `.sitemap` CSS and nestable rely on. That is more than a direct correction, for a screen-reader nuance.
- **[low, rejected]** (blind-hunter) `id="pageGroup"` repeats once per site. This is pre-existing: stock Piranha already renders one per site, and no reference to `#pageGroup` was shown.
- **[low, rejected]** (blind-hunter) The site title appears in both the tab and the header row. The frozen Approach keeps the header row (site edit link + "Add page") on purpose.

## Verification

**Commands:**
- `dotnet build TbTruongHoc.sln` -- expected: 0 errors
- `dotnet test TbTruongHoc.sln` -- expected: all pass (use `--artifacts-path` inside the repo if a running dev instance locks `bin/Debug`)

**Manual checks:**
- Run the dev profile, log into `/manager`, open Pages: two tabs, one tree at a time, and every matrix row behaves as listed.
