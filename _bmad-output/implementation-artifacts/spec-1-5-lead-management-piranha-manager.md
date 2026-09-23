---
title: 'Lead management inside Piranha Manager (Story 1.5, AD-3)'
type: 'feature'
created: '2026-09-23'
status: 'done'
route: 'dispatch'
baseline_commit: 'a07a2f4d0b68666e6dc936aeba0c5c8353a1de7a'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 1.4's `FormSubmission` table has no read UI — sales/ops must query the database directly to see any lead, with no list, filter, or detail view anywhere in the app.

**Approach:** Add a Manager-only screen following Piranha's own built-in "Aliases" screen pattern (verified against piranha.core v12.0 source): a Razor Page (`Areas/Manager/Pages/Leads.cshtml`) rendered inside Manager's `_Layout.cshtml`, backed by a new `LeadApiController` (`manager/api/lead/...`), both gated by `Permission.Admin` — no new build tooling. A hand-written Vue 2 instance (already loaded globally by Piranha) drives the list, site filter, and a Bootstrap detail modal. A "Danh sách khách để lại thông tin" entry is added to `Piranha.Manager.Menu.Items`.

## Boundaries & Constraints

**Always:**
- Gate both the Razor Page (`Leads.cshtml.cs`'s `PageModel`) and every `LeadApiController` action with `[Authorize(Policy = Permission.Admin)]` — the same policy `AliasApiController` uses, consistent with this being a single-Manager-login app (no custom roles/permissions in v1).
- Resolve site names for the list/detail views via `IApi.Sites.GetAllAsync()` server-side — never hardcode site names, and never let the client guess a site name from `SiteId`.
- List query orders by `CreatedAt` descending (most recent first) by default, unconditionally.
- HTML-encode/rely on Vue's default mustache escaping for every rendered field — same convention as `_QuoteRequestForm.cshtml`.

**Never:**
- Never build a Vue-Router SPA route or add a webpack/gulp build step — this repo has no JS build pipeline, and piranha.core's compiled Manager bundle has no supported extension point for a new component (only `Module.Scripts`/`Module.Styles`).
- Never add edit/delete/export actions for leads — read-only (list, filter, detail) only; no CRM/webhook integration.
- Never introduce a new Piranha permission/role — reuse `Permission.Admin`.
- Never attempt an HTTP "log in as Manager admin" step in tests — admin credentials are randomly generated at first boot (`README.md`) and unknown to test code.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| List with rows across both sites | 2+ `FormSubmission` rows, mixed `SiteId` | All rows returned, each with resolved site name, ordered `CreatedAt` desc | N/A |
| Filter by one site | `siteId` query param set | Only that site's rows returned | N/A |
| Detail of a general submission | row with `LocationAddress`/`IsOutsideServiceArea` both null | Full record returned; those two fields present as null, not omitted | N/A |
| Detail of a survey submission | row with `LocationAddress` set and `IsOutsideServiceArea` true/false | Full record returned including both fields | N/A |
| Detail of a non-existent id | random `Guid` | 404 | Empty/error body, no exception |
| Unauthenticated request | no Manager auth cookie | Page and API both reject with a 302 redirect to the login page — never the data (Piranha's own `SecurityMiddleware`, in place since Story 1.1, rewrites every 401 app-wide into a 302; renegotiated 2026-09-23 after implementation confirmed this is existing platform behavior, not story-specific) | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Controllers/LeadsController.cs` -- existing public write-path controller; do not modify — the new read-side controller is separate and Manager-scoped
- `src/TbTruongHoc.Web/Data/LeadDbContext.cs`, `Models/FormSubmission.cs` -- existing schema this story reads from; add no columns
- `src/TbTruongHoc.Web/Program.cs:80-151` (`app.UsePiranha` block) -- add `Piranha.Manager.Menu.Items` registration for the new "Danh sách khách để lại thông tin" entry after `App.Init(options.Api)`; mirror existing inline-registration style already used here (hooks, seeding) rather than introducing a new `IModule` class
- `tests/TbTruongHoc.Web.Tests/PiranhaWebApplicationFactory.cs`, `LeadSubmissionTests.cs` -- reuse real-MariaDB `WebApplicationFactory<Program>` + Host-header pattern for seeding; for authenticated-success assertions, resolve `LeadApiController` via `factory.Services.CreateScope()` and call its actions directly (bypasses `[Authorize]`, which needs real login, not app logic)
- (reference only) piranha.core v12.0 `core/Piranha.Manager`: `Areas/Manager/Pages/AliasEdit.cshtml(.cs)`, `assets/src/js/piranha.alias.js`, `Controllers/AliasApiController.cs`, `Permissions.cs`, `Menu.cs` -- structural pattern mirrored here; confirm exact `Permission.Admin` constant name via IDE go-to-definition first

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/Models/LeadListItemModel.cs`, `LeadDetailModel.cs` -- DTOs: list item (`Id, SiteId, SiteName, FormType, Name, Phone, CreatedAt`) and detail (all `FormSubmission` fields + `SiteName`) -- keeps the wire shape stable independent of the EF entity
- [x] `src/TbTruongHoc.Web/Controllers/LeadApiController.cs` -- `[Area("Manager")] [Route("manager/api/lead")] [Authorize(Policy = Permission.Admin)]`; `GET list/{siteId?}` (all rows or filtered, joined to site names, `CreatedAt` desc) and `GET {id}` (404 if missing) -- the two reads the AC requires
- [x] `src/TbTruongHoc.Web/Areas/Manager/Pages/Leads.cshtml`, `Leads.cshtml.cs` -- new Razor Page, `[Authorize(Policy = Permission.Admin)]` `PageModel`, renders inside Manager's `_Layout.cshtml`; passes the site list (for the filter dropdown) from `IApi.Sites.GetAllAsync()` into the view
- [x] `src/TbTruongHoc.Web/wwwroot/assets/js/manager-leads.js` -- vanilla Vue 2 instance: loads `/manager/api/lead/list`, renders the table, site-filter dropdown re-fetches with `siteId`, row click fetches `/manager/api/lead/{id}` into a Bootstrap modal -- mirrors `piranha.alias.js`'s `fetch`-based load pattern
- [x] `src/TbTruongHoc.Web/Program.cs` -- register the new `MenuItem` (`InternalId = "Leads"`, `Name = "Danh sách khách để lại thông tin"`, `Route = "~/manager/leads"`, `Policy = Permission.Admin`) into `Piranha.Manager.Menu.Items`
- [x] `tests/TbTruongHoc.Web.Tests/LeadManagerTests.cs` -- covers the I/O Matrix: seed rows (via `POST /api/leads` or direct `LeadDbContext` inserts) across both sites, assert `LeadApiController.List`/`.Get` directly; separately assert anonymous `GET manager/leads` / `GET manager/api/lead/list` reject rather than return data

**Acceptance Criteria:**
- Given rows in `FormSubmission` across both sites, when the Manager screen loads, then all rows are listed with site, form type, name, phone, and date, most recent first.
- Given the list, when a row is clicked, then a detail view shows every field, including `LocationAddress`/`IsOutsideServiceArea` when present.
- Given the list, when a site filter is applied, then only that site's rows show.

## Implementation Notes

- All tasks implemented as specified, following piranha.core v12.0's `AliasApiController`/`AliasEdit.cshtml`/`Menu.cs` source (fetched from the `v12.0` tag to match the installed `Piranha.Manager` 12.0.0 NuGet package byte-for-byte -- confirmed the `PiranhaAdmin` permission string literally in the installed DLL before relying on `Permission.Admin`). `dotnet build TbTruongHoc.sln` -- 0 errors (only the same pre-existing Piranha 12.0.0 NuGet advisory warnings Story 1.4 already noted). `dotnet test TbTruongHoc.sln` against real MariaDB (`docker compose up -d mariadb`, `ConnectionStrings__piranha` exported per README) -- **40/40 passed**, including all 8 new `LeadManagerTests` (one per I/O Matrix row, plus a menu-registration check). `docker compose up -d --build piranha-app` -- image built and started cleanly with no startup errors; `GET /manager/leads` and `GET /manager/api/lead/list` both redirected to `/manager/login` as expected (never the data), and a `POST /api/leads` smoke-seed on `tbtruonghoc.local` still returned `200` (Story 1.4's write path untouched).
- **`Piranha.Manager.Menu.Items` is a strictly two-level structure** (verified against `Menu.cs`/`MenuExtensions.cs`/`_Menu.cshtml` source): top-level entries render only as a non-clickable header; the actual link always comes from a child in that group's own `Items` list. The spec's single described `MenuItem` (`InternalId = "Leads"`, with `Route`/`Policy` set directly on it -- properties that only mean anything on a leaf/child item) is therefore added as a child of the existing built-in `"Content"` group in `Program.cs`, not appended to `Menu.Items` directly -- appending it directly would have silently rendered as an inert header with no link. Guarded with an idempotency check (`contentGroup.Items["Leads"] == null`) before adding.
- **Manual UI click-through (the Verification section's last step -- "open `/manager` with the console-printed admin login") was not performed.** The already-running dev container's MariaDB volume was seeded with an admin account on a prior first boot (before this session), so no fresh console-printed credentials were available this session, and per this spec's own Boundaries ("Never attempt an HTTP log in as Manager admin step in tests") no login was attempted to work around that. Verified everything short of the logged-in visual check instead: `LeadApiController.List`/`.Get` exercised directly against real seeded rows (via `LeadManagerTests`, which resolves the controller's dependencies from DI and calls its actions directly, bypassing only the `[Authorize]` filter itself) covers every I/O Matrix row's actual data/logic; a live anonymous curl against the rebuilt container confirms the page and API routes exist, compile, and reject correctly. The user should do one manual click-through with their own Manager credentials to visually confirm the table/filter/modal render as intended.
- **Spec discrepancy found during implementation, flagged for human attention (not fixed unilaterally since the I/O Matrix is inside the frozen block):** the matrix's unauthenticated-API row states "401/403 for the API." Empirically this app never returns a raw 401/403 for *any* Manager route, page or API: `options.UseCms()` (Story 1.1) registers Piranha's own `SecurityMiddleware` globally around the entire pipeline (`core/Piranha.AspNetCore/Http/SecurityMiddleware.cs`), which rewrites *any* 401 response anywhere in the app into a 302 redirect to its configured login URL before it reaches the client -- confirmed both by a passing/failing test run and by curling the pre-existing, unrelated `manager/api/alias/list` endpoint on the already-running container with and without an `X-Requested-With: XMLHttpRequest` header (both came back 302, to different login URLs). This is pre-existing platform behavior from Story 1.1's scaffold, not something this story's code introduced or should change. `LeadManagerTests.Anonymous_Request_To_Lead_Api_List_Never_Returns_Data` asserts the behavior that actually holds and matters -- rejected, never the data -- rather than the specific status code the frozen matrix names.

- After the review's patch round (4 findings routed to `patch`; see `## Review Triage Log`): re-engaged the implementing subagent with the exact fix list. It added `Anonymous_Request_To_Lead_Api_Detail_Never_Leaks_Seeded_PII` (seeds a real row, asserts an anonymous `GET manager/api/lead/{id}` 302s to login and never leaks the seeded name/phone), an `error` flag in `manager-leads.js` (set in both `load()`'s and `showDetail()`'s `.catch`, cleared on success) with a matching Bootstrap alert in `Leads.cshtml`, a `startupLogger?.LogWarning(...)` in `Program.cs`'s `contentMenuGroup == null` branch (mirroring the file's existing `siteContentCache` resolve-once pattern), and rewrote `Detail_Of_General_Submission_...`'s null-field assertion to serialize with the app's actual registered `Microsoft.AspNetCore.Mvc.JsonOptions.JsonSerializerOptions` (resolved from `_factory.Services`, including naming-policy-aware property lookup) instead of `JsonSerializer.Serialize`'s bare defaults. Re-verified independently in this session: `dotnet build TbTruongHoc.sln` — 0 errors; `dotnet test TbTruongHoc.sln` against real MariaDB — **41/41 passed** (9 `LeadManagerTests`, no regressions).

## Spec Change Log

- 2026-09-23: implementation found the I/O Matrix's "401/403 for the API" unauthenticated-request row does not match reality — Piranha's `SecurityMiddleware` (since Story 1.1) rewrites every 401 app-wide into a 302 redirect-to-login. Human confirmed accepting existing platform behavior rather than changing global security middleware (out of scope, higher risk). Amended the row's wording to describe the 302 behavior. KEEP: `LeadManagerTests`'s anonymous-rejection tests, which already assert the real behavior (rejected, never the data) rather than a specific status code.

## Review Triage Log

- **[false]** (blind-hunter) `List`'s `list/{siteId?}` route has no `:guid` constraint unlike `Get`'s `{id:guid}`, claimed to "silently fall back to no filter" on a malformed `siteId` segment. Refuted: `[ApiController]` (both actions live on the same `[ApiController]`-annotated `LeadApiController`) automatically returns 400 via its `ModelState`-invalid filter when a route value fails type conversion for a scalar-typed parameter (`Guid?` included) — the exact mechanism `LeadsController` (Story 1.4) already documented itself relying on for `[Required]` failures. A malformed segment 400s; it does not silently bind to null.
- **[low]** (blind-hunter) No test asserts anonymous `GET manager/api/lead/{id}` redirects rather than leaking a specific lead's PII — only the list endpoint and the page are covered. Verified: `LeadManagerTests.cs` has no such test. Low: the same class-level `[Authorize(Policy = Permission.Admin)]` on `LeadApiController` already protects `Get` identically to `List` (verified by reading the attribute's placement), so the risk is coverage-completeness, not an actual gap in protection. Routed to patch (trivial test addition).
- **[low, rejected]** (blind-hunter + edge-case-hunter) `List` has no pagination/limit, and only `SiteId` is indexed (`LeadDbContext.cs`'s existing `HasIndex(e => e.SiteId)`) — `CreatedAt`, the sort column, is not. Verified: `LeadApiController.List` calls `.OrderByDescending(s => s.CreatedAt).ToListAsync()` unbounded. Real at scale, but both sites are pre-launch with near-zero current submission volume (same reasoning Story 1.4's review already used to reject rate-limiting/CAPTCHA hardening for this app's current stage), and the fix (pagination + a new migration for a `CreatedAt` index) is more than a direct correction. Rejected per the low-finding rule; worth revisiting once real lead volume exists.
- **[false]** (blind-hunter) No end-to-end test exercises a real authenticated HTTP request through `[Authorize]` — only anonymous-rejection and direct-controller-invocation tests exist. Refuted as a defect: the frozen Boundaries explicitly forbid the only way to build such a test ("Never attempt an HTTP 'log in as Manager admin' step in tests" — credentials are randomly generated at first boot). The intent itself excludes this, so its absence is the deliberate, sanctioned state.
- **[reject: spec-wording only]** (blind-hunter) Frozen I/O Matrix's filter row said "`siteId` query param" but the implementation uses a route segment (`list/{siteId?}`), undocumented in the Spec Change Log. Filtering itself is verified correct by `List_Filtered_By_SiteId_Returns_Only_That_Sites_Rows`; the only fix here is editing spec wording, which the triage rules exclude from routing. No code or behavior defect.
- **[false]** (edge-case-hunter) Claimed the Tasks list' description ("into `Piranha.Manager.Menu.Items`") misleads readers about where the `MenuItem` actually lands. Refuted: `## Implementation Notes` already documents, in detail, that the item is nested under the `"Content"` group's own `Items` rather than appended to `Menu.Items` directly, and why.
- **[medium]** (blind-hunter + edge-case-hunter, shared root cause) `manager-leads.js`'s `load()` and `showDetail()` both swallow fetch failures with `.catch(function (error) { console.log('error:', error); })` only — no on-screen error state, no `loading` reset. Verified directly in the diff. A mid-visit session expiry makes `fetch` follow the 302 to the HTML login page; `response.json()` then throws, is caught, and logged to the console only — sales sees a stale/blank screen with no cue to re-authenticate, undermining the story's own point of making leads reliably visible. Routed to patch (add an `error` flag + a small inline message; no public surface change).
- **[maybe-false, deferred]** (blind-hunter) `items.length !== 0` decides the empty-state message with no separate loading check, so "Chưa có khách để lại thông tin nào." could flash before the first fetch resolves. Could not verify: `:class="{ ready: !loading }"` on the wrapping `.app` div mirrors Piranha's own built-in screens' convention, which may hide the whole content area via CSS until ready — but that CSS lives compiled inside the `Piranha.Manager` DLL, not source-browsable in this session. If true, severity would be low/medium (a brief, self-correcting visual flash, no data loss). Settled by either inspecting Piranha's compiled manager CSS for an `.app:not(.ready)` rule, or the still-outstanding manual Manager click-through (see Implementation Notes).
- **[low, rejected]** (edge-case-hunter) Rapid double-switching the site filter before the previous fetch resolves can let an out-of-order response overwrite `items` with a different site's rows than the currently-displayed filter label. Verified: `load()` has no request-token/`AbortController` guard against out-of-order responses. Low: rare trigger (fast repeated clicks) on a low-traffic internal tool, self-correcting on the next successful load; the fix (track and ignore stale responses) is more than a direct correction. Rejected per the low-finding rule.
- **[low]** (edge-case-hunter) `Program.cs`'s `Piranha.Manager.Menu.Items["Content"]` lookup could theoretically return null if the "Content" group isn't registered yet when this callback runs, silently skipping Leads-menu registration with no diagnostic. Verified: the `if (contentMenuGroup != null && ...)` guard is silent on the null branch. Low probability given confirmed ordering (Piranha's own `Module.Init()`, which registers "Content", runs as part of the preceding `App.Init(options.Api)` call in the same block), but worth a defensive log line mirroring this file's existing `siteContentCache` resolve-once pattern. Routed to patch.
- **[medium]** (verification-gap, pre-verified, filed disposition: patch) `LeadManagerTests.Detail_Of_General_Submission_Returns_Full_Record_With_LocationFields_Null_Not_Omitted` asserts the "null fields present, not omitted" guarantee by calling `System.Text.Json.JsonSerializer.Serialize(detail)` with default options, never the app's actual registered `Microsoft.AspNetCore.Mvc.JsonOptions.JsonSerializerOptions`. A future `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` tweak anywhere in the app would silently break this story's frozen I/O Matrix guarantee on the real endpoint while this test kept passing. Routed to patch: resolve the app's real `JsonOptions` from `_factory.Services` and serialize with those instead.

## Design Notes

Detail view is a Bootstrap modal driven by the same page-level Vue instance (matches Manager's own `_PreviewModal.cshtml` convention), not a second Razor Page/route — no deep-linkable URL is required.

## Verification

**Commands:**
- `dotnet build TbTruongHoc.sln` -- expected: 0 errors
- `dotnet test TbTruongHoc.sln` (against `docker compose up -d mariadb`) -- expected: all tests pass, including new `LeadManagerTests`
- `docker compose up -d --build piranha-app` -- expected: app starts; open `/manager` with the console-printed admin login, confirm the menu entry lists/filters/details leads seeded via `/api/leads`, with Manager's own chrome/CSS intact
