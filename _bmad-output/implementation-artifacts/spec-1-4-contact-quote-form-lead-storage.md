---
title: 'General contact/quote-request form with lead storage (Story 1.4, FR-3)'
type: 'feature'
created: '2026-09-22'
status: 'done'
route: 'dispatch'
baseline_commit: 'NO_VCS'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No mechanism exists yet for a visitor on any product/category page, on either site, to submit a lightweight contact/quote request without leaving the page — there is no lead-storage table, no submission endpoint, and no client-side form anywhere in the app.

**Approach:** Add a reusable `_QuoteRequestForm.cshtml` partial (labeled Name/Phone/Product-of-interest/Message fields) submitted via vanilla-JS `fetch` to a new `POST /api/leads` endpoint on a new `LeadsController`. The controller resolves the current site the same way Piranha's own pipeline does (`GetByHostnameAsync` → `GetDefaultAsync` fallback), validates, and persists a `FormSubmission` row through a new lightweight `LeadDbContext` (EF Core Migrations, applied automatically at startup) pointed at the same MariaDB connection string. The client swaps in an inline success/error state with no page reload, retaining entered values on failure. Email notification on new submissions is deferred to a follow-up spec (see `deferred-work.md`) to keep this spec within a reviewable size.

## Boundaries & Constraints

**Always:**
- Resolve `site_id` server-side via `api.Sites.GetByHostnameAsync(Request.Host.Host)`, falling back to `GetDefaultAsync()` — the exact fallback Piranha's own pipeline uses (mirrored by `HostnameResolutionTests`). Never trust a client-supplied site id.
- Submit via client-side `fetch` only — no full-page reload or redirect on submit.
- Validation errors are announced in text adjacent to the invalid field, both in the server's JSON error shape and the client's DOM update — never color-only.
- On a failed submission, retain the visitor's entered field values in the form (client-side; do not clear on error).
- Manage the `FormSubmission` schema via EF Core Migrations on a new `LeadDbContext`, applied automatically at startup — mirrors how Piranha's own tables ship pre-built migrations that apply on first startup.
- HTML-encode all rendered/echoed values — same hand-rolled-partial convention as `_MetaTags.cshtml`/`_ContactBlock.cshtml`.

**Never:**
- Never send or attempt an email notification in this story — deferred to a follow-up spec (`deferred-work.md`); a saved lead and its inline confirmation must not depend on any mail capability.
- Never build the Manager list/detail view for submissions (Story 1.5's scope) — this story only creates and stores rows.
- Never build the `survey-form-modal`/`is_outside_service_area` UX (UX-DR8) — the schema reserves the nullable columns, but that modal and its warning-banner belong to a later story.
- Never build a dedicated product/category page template to host this form (Epic 2/3/6's scope) — render the partial from the existing `StandardPage`/`StandardPost` views so the flow is end-to-end testable now; later epics include the same partial from their own PDP templates with a real `product_of_interest` prefill.
- Never add a Piranha Manager module/permission for this table in this story.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Visitor submits complete form on Site A | name, phone, message filled | New `FormSubmission` row, `site_id` = Site A's id, `form_type='general'` | N/A |
| Visitor submits complete form on Site B | same, on Site B | Row has Site B's `site_id` — never Site A's | N/A |
| `product_of_interest` prefilled from page context | partial rendered with a product name passed in | Field is pre-filled and saved verbatim in the row | N/A |
| Required field left blank (name or phone) | e.g. phone empty | Nothing saved; inline error text shown next to that field | 400 response with field-level error(s) |
| Submission fails (network/server error) | e.g. DB unreachable | Entered values retained in the form; inline error message with phone/Zalo fallback text | Handled without an unhandled exception reaching the visitor; no half-saved row |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Program.cs:33-37` -- existing `connectionString`/`serverVersion` resolution for Piranha's `MySqlDb`; reuse the same values for the new `LeadDbContext` registration
- `src/TbTruongHoc.Web/Program.cs:123-131` -- existing startup seed-call site (`SiteSeed`, `SiteSettingsSeed`); add `LeadDbContext.Database.MigrateAsync()` in the same block
- `src/TbTruongHoc.Web/Controllers/CmsController.cs` -- only existing controller; MVC-view-returning, not `[ApiController]`/JSON — the new `LeadsController` is a different shape (JSON in/out), don't mirror its view-rendering style
- `tests/TbTruongHoc.Web.Tests/HostnameResolutionTests.cs:38-87` -- confirms the exact site-resolution fallback (`GetByHostnameAsync` → `GetDefaultAsync`) `LeadsController` must replicate, since a plain API action isn't covered by Piranha's own CMS routing middleware
- `tests/TbTruongHoc.Web.Tests/SiteSettingsTests.cs:525-533`, `PerPageSeoFieldsTests.cs:347-349` -- `request.Headers.Host = hostname` pattern to mirror for POSTing to `/api/leads` under each site's hostname in the new test class
- `src/TbTruongHoc.Web/Views/Shared/_ContactBlock.cshtml`, `_MetaTags.cshtml` -- hand-rolled, HTML-encoded-by-default Razor partial convention to mirror for `_QuoteRequestForm.cshtml`
- `src/TbTruongHoc.Web/Views/Shared/_Layout.cshtml:39` -- do NOT include the new partial here (sitewide, like `_ContactBlock`) — include it from `Views/Cms/Page.cshtml` and `Views/Cms/Post.cshtml` instead, the two existing per-content-type templates
- `src/TbTruongHoc.Web/TbTruongHoc.Web.csproj:14-25` -- add `Pomelo.EntityFrameworkCore.MySql` pinned to `8.0.3` (the version already resolved transitively via `Piranha.Data.EF.MySql`, confirmed in `obj/project.assets.json`) and `Microsoft.EntityFrameworkCore.Design` (migrations tooling)

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/TbTruongHoc.Web.csproj` -- add `Pomelo.EntityFrameworkCore.MySql` (8.0.3) and `Microsoft.EntityFrameworkCore.Design` package references -- needed before the new DbContext/migrations can compile
- [x] `src/TbTruongHoc.Web/Models/FormSubmission.cs` -- new entity: `Id (Guid), SiteId (Guid), FormType (string), Name, Phone, ProductOfInterest (nullable), Message (nullable), LocationAddress (nullable), IsOutsideServiceArea (bool?), CreatedAt (DateTimeOffset)` -- the exact schema the AC's `Given` clause specifies
- [x] `src/TbTruongHoc.Web/Data/LeadDbContext.cs` -- new `DbContext` with a `DbSet<FormSubmission>` mapped to a `FormSubmission` table, isolated from Piranha's own `MySqlDb`
- [x] `src/TbTruongHoc.Web/Data/Migrations/` (via `dotnet ef migrations add InitialCreate --context LeadDbContext`) -- first migration for the `FormSubmission` table
- [x] `src/TbTruongHoc.Web/Program.cs` -- register `LeadDbContext` against the existing `connectionString`/`serverVersion`; call `Database.MigrateAsync()` at startup alongside the existing seed calls
- [x] `src/TbTruongHoc.Web/Models/LeadSubmissionRequest.cs` -- request DTO with `[Required]` on Name/Phone -- server-side validation source of truth
- [x] `src/TbTruongHoc.Web/Models/QuoteRequestFormViewModel.cs` -- small view model carrying the partial's optional `ProductOfInterest`/`FormType` parameters (not named in the original task list; added because the partial needs a strong type to accept these optional parameters)
- [x] `src/TbTruongHoc.Web/Controllers/LeadsController.cs` -- new `[ApiController]`, `POST api/leads`: resolves the current site (see Code Map), validates, saves the row, returns 200/400 JSON -- the single write path for every lead
- [x] `src/TbTruongHoc.Web/Views/Shared/_QuoteRequestForm.cshtml` -- new partial: labeled fields, error text associated via `aria-describedby`, optional `productOfInterest`/`formType` parameters
- [x] `src/TbTruongHoc.Web/wwwroot/assets/js/lead-form.js` -- new vanilla-JS fetch handler: submits JSON to `/api/leads`, swaps in inline success/error markup in place of the form, retains field values on failure
- [x] `src/TbTruongHoc.Web/Views/Cms/Page.cshtml`, `Views/Cms/Post.cshtml` -- include `_QuoteRequestForm.cshtml` and reference `lead-form.js`
- [x] `tests/TbTruongHoc.Web.Tests/LeadSubmissionTests.cs` -- new test class covering the I/O Matrix above via real HTTP POSTs (Host-header pattern) against the real MariaDB-backed `LeadDbContext`, asserting the resolved `site_id` per site

**Acceptance Criteria:**
- Given a visitor submits the general contact form on either site, when the request completes, then a `FormSubmission` row exists with the correct `site_id` and every submitted field intact, with no full-page reload or redirect.
- Given a successful submission, when the request completes, then an inline confirmation replaces the form in place, with no page reload and no separate modal.
- Given the form's fields, when it renders, then each is individually labeled, and a validation failure is announced in text adjacent to the invalid field.

## Implementation Notes

- All tasks implemented as specified. `dotnet build TbTruongHoc.sln` -- 0 errors (only pre-existing Piranha 12.0.0 NuGet advisory warnings, unrelated to this change). Ran `dotnet ef migrations add` twice more as a drift probe after `InitialCreate` -- both produced an empty `Up`/`Down` body, confirming the model and migration are in sync -- then deleted the probe files.
- `LeadsController` relies on `[ApiController]`'s automatic 400 `ValidationProblemDetails` response for `[Required]` failures on `LeadSubmissionRequest` -- no explicit `ModelState` check needed in the action body.
- Docker Desktop's daemon was not reachable in the implementing subagent's sandbox, so the spec's real-MariaDB `dotnet test` and manual `docker compose` verification could not be run there. Completed independently in this session instead: started Docker Desktop, ran `docker compose up -d mariadb`, then `dotnet test TbTruongHoc.sln`.
  - **First attempt failed all 32 tests** (not just the 6 new ones) with `System.ArgumentException: The string argument 'connectionString' cannot be empty` at `Program.cs`'s `UseEF<MySqlDb>` call -- a pre-existing environment-setup requirement, not a defect in this story's diff: per `README.md`'s "Running tests" section, `ConnectionStrings__piranha` must be exported in the shell session before `dotnet test` (it is deliberately not committed in `appsettings.json`). Re-ran with the env var built from the local `.env` file's `MARIADB_*` values pointed at `localhost:3307` -- **32/32 passed**, including all 6 new `LeadSubmissionTests` (one per I/O Matrix row, `Persistence_Failure_...` standing in for "DB unreachable" via an oversized `Message` that MariaDB's mapped `varchar(2000)` column rejects).
  - Then ran the spec's second verification command for real: `docker compose up -d --build piranha-app`. Image built and started cleanly, `LeadDbContext`'s migration applied automatically alongside `SiteSeed`/`SiteSettingsSeed` with no startup errors. Manually POSTed to `/api/leads` with `Host: tbtruonghoc.local` and `Host: trongdoitam.local` -- both returned `200 {"id": ...}`; a follow-up SQL query joining `FormSubmission` to `Piranha_Sites` confirmed each row's `SiteId` matched its own site's `InternalId` (`tbtruonghoc` / `trongdoitam-net`) and never the other's. Also POSTed a blank-phone payload -- got `400` with a `Phone` field error, matching `LeadSubmissionTests`. Deleted the three manually-created rows afterward and stopped the `piranha-app` container (per Story 1.3's precedent of not leaving verification clutter in the shared dev DB).

- After the review's patch round (8 findings routed to `patch`, 1 to `defer`, the rest `false`/rejected-`low`): re-engaged the implementing subagent with the exact fix list. It added `ILogger<LeadsController>` logging to both failure branches, `[StringLength]` on all four `LeadSubmissionRequest` fields matching `LeadDbContext`'s mapped column lengths, a `FormType` allow-list (`{"general","survey"}`, case-insensitive, anything else defaults to `"general"`), a `request is null` guard, widened the `try/catch` to cover site resolution as well as `SaveChangesAsync`, an `AbortSignal.timeout(15000)` on the `fetch()` call, `data-quote-request-form`/`lead-form.js` assertions in `PerPageSeoFieldsTests`' two existing Page/Post render tests, and a `GetSavedSubmissionByPhoneAsync` + `Assert.Null` pair in `Blank_Name_Saves_Nothing_...` (adapted to look up by phone rather than name, since name is blank in that test — a sound adjustment to the exact ask). Re-verified independently in this session: `dotnet build TbTruongHoc.sln` — 0 errors; `dotnet test TbTruongHoc.sln` against real MariaDB — **32/32 passed**, no regressions.

## Spec Change Log

## Review Triage Log

- **[patch]** `LeadsController.Create`: no `ILogger` anywhere — both the "no site configured" branch and the `catch (Exception)` around `SaveChangesAsync` return a generic `Problem()` but never log the underlying failure. Verified: no `ILogger` field/injection exists in the class. Medium: a real DB/config outage in the lead pipeline is invisible to ops until someone notices missing leads.
- **[patch]** `LeadSubmissionRequest.cs`: only `[Required]` on Name/Phone — no `[StringLength]`/`[MaxLength]` matching `FormSubmission`'s mapped column lengths (Name 200, Phone 50, ProductOfInterest 200, Message 2000). Verified: an oversized field falls through to the generic DB-exception catch and a 500, instead of clean 400 model validation (confirmed by `LeadDbContext.cs`'s `HasMaxLength` calls and by `Persistence_Failure_Returns_Error_...`'s own doc comment, which relies on MariaDB rejecting the INSERT rather than the model validating it). Medium: a real UX/correctness gap — a visitor who writes a long message gets a generic "call us" error instead of a field-level "too long" message — but the fallback path is still safe.
- **[patch]** `LeadsController.cs:59`: `FormType` is persisted verbatim from client input (`request.FormType!`) with no allow-list, contradicting the class's own doc comment that treats it as a closed set (`"general"`/`"survey"`). Verified: no validation restricts the value. Low: no current caller sends anything but the default, but the public endpoint accepts arbitrary strings into a field Story 1.5's Manager will filter/group by.
- **[patch]** `LeadsController.Create`: a literal JSON `null` POST body binds `request` to `null` (neither the file nor the project has nullable-reference-types enabled, so `[ApiController]`'s automatic "required non-nullable parameter" 400 does not apply here) and `request.FormType` at line 59 throws an unguarded `NullReferenceException` before the try/catch. Verified: confirmed the project has no `<Nullable>enable</Nullable>` and `LeadsController.cs` has no file-level `#nullable enable`. Medium: a real, reachable case (malformed/buggy client) that produces a raw 500 instead of the graceful handling the rest of the endpoint provides.
- **[patch]** `LeadsController.Create`: `_api.Sites.GetByHostnameAsync`/`GetDefaultAsync` run outside the `try/catch` that wraps `SaveChangesAsync`. Verified: if the shared MariaDB is genuinely unreachable, the site-resolution call throws first and unguarded — the I/O Matrix's "DB unreachable → handled without unhandled exception" row is not actually covered for this earlier failure point (the existing `Persistence_Failure_...` test only reaches a MariaDB-rejects-the-INSERT case, not a true DB-down case). Medium: a real infra-outage scenario this story's own matrix promises to handle gracefully.
- **[patch]** `wwwroot/assets/js/lead-form.js`: `fetch()` has no timeout, so a stalled network leaves the submit button disabled indefinitely with no feedback. Verified: no `AbortSignal`/timeout anywhere in the fetch call. Low: real but narrow (visitor can reload); fix is a one-line `AbortSignal.timeout(...)` addition.
- **[patch]** No test asserts that `_QuoteRequestForm`/`lead-form.js` actually render on a published Page/Post. Verified (verification-gap layer read `PerPageSeoFieldsTests.cs` in full): the only tests that render these exact views assert solely on `<title>`/meta-description; `LeadSubmissionTests.cs` never renders a page. A future edit that drops the partial/script include from `Page.cshtml`/`Post.cshtml` would pass every existing test while the form silently disappears site-wide.
- **[patch]** `LeadSubmissionTests.Blank_Name_Saves_Nothing_And_Returns_400_With_Field_Level_Error` never calls `GetSavedSubmissionAsync`/`Assert.Null`, unlike its sibling `Blank_Phone_...` test which does. Verified by reading both tests side by side. A regression that saves a row on the blank-Name path specifically would pass this test.
- **[defer]** Non-default `FormType` (e.g., a future `"survey"` value) passthrough has no test coverage today. Verified (verification-gap layer): every `LeadSubmissionTests` payload omits `formType`, only exercising the default-to-`"general"` branch. No current caller ever sends another value (neither `Page.cshtml` nor `Post.cshtml` sets it) — worth a test when Story 6.5 wires up the survey form, not blocking for 1.4.
- **[low, rejected]** No spam/abuse protection (rate limiting/CAPTCHA/honeypot) on the public `/api/leads` endpoint. Real in principle, but both sites are pre-launch and not yet publicly indexed, and the real fix (rate-limiting middleware or CAPTCHA integration) is not a trivial, scope-contained change — better addressed as a pre-public-launch hardening pass than blocking this story.
- **[low, rejected]** `lead-form.js`'s `showFieldErrors` could silently show nothing if a 400 response's `errors` dictionary had a key matching neither `FIELD_MAP` nor any input name. Verified: today's only `[Required]` validators (Name/Phone) and this review's own planned `[StringLength]` additions (Name/Phone/ProductOfInterest/Message) all key by names already present in `FIELD_MAP` — no current or soon-added path produces an unmapped key.
- **[false]** `QuoteRequestFormViewModel.cs` lacks `#nullable enable`, flagged as "likely a CS8618 warning source." Refuted: neither the project (`TbTruongHoc.Web.csproj` has no `<Nullable>enable</Nullable>`) nor the file itself opts into nullable-reference-type checking, so no such warning is possible — confirmed by `dotnet build TbTruongHoc.sln` completing with 0 warnings beyond pre-existing Piranha NuGet advisories.
- **[false]** No CSS added for `.quote-request-form`/`.quote-request-form__error`/`.quote-request-form__success` — form renders unstyled. Refuted: matches this repo's own established precedent — Story 1.3's `_ContactBlock.cshtml` was explicitly built "minimal/unstyled," deferring visual polish to Epic 2/6 Story 6.1/2.1. This story's Boundaries never asked for styling either; unstyled-by-design is the intended state, not a defect.
- **[false]** `Pomelo.EntityFrameworkCore.MySql` pinned to `8.0.3` "risks a silent downgrade/conflict" against Piranha's own transitive dependency. Refuted: `8.0.3` is the exact version already resolved transitively via `Piranha.Data.EF.MySql` (confirmed in `obj/project.assets.json` during planning), and both `dotnet build`/`dotnet test` completed with 0 errors and no NuGet version-conflict warnings.
- **[false]** `_Layout.cshtml`'s sitewide `_ContactBlock` "isn't addressed," implying a related lead-capture surface left unintegrated. Refuted: `_ContactBlock.cshtml` contains only `<a>`/`<span>` elements (phone/Zalo/Maps/address) — no `<form>`, no submission mechanism, nothing shared with `_QuoteRequestForm`/`lead-form.js` for this diff to have left unaddressed.
- **[false]** `Program.cs:139`'s `LeadDbContext.Database.MigrateAsync()` call has no try/catch/logging around a startup failure. Refuted as a defect introduced by this story: it exactly mirrors the pre-existing unguarded `.GetAwaiter().GetResult()` pattern already used immediately below it by `SiteSeed.EnsureSeededAsync`/`SiteSettingsSeed.EnsureSeededAsync` (Stories 1.1/1.3) — consistent with, not worse than, this file's established risk tolerance.
- **[false]** Concurrent-migration race if two app instances start simultaneously. Refuted: `docker-compose.yml` runs exactly one `piranha-app` replica with no scaling configuration anywhere in this repo — the trigger condition cannot occur in the current deployment topology.
- **[false]** `.Trim()`-ing Name/Phone and coercing blank `ProductOfInterest`/`Message` to `null` violates the AC's "every submitted field intact." Refuted: read in the context of its full Given/When/Then (site_id correctness, no page reload), "intact" was never a byte-for-byte whitespace guarantee; trimming and blank-to-null normalization is standard sanitization, consistent with how `_ContactBlock.cshtml` already treats `IsNullOrWhiteSpace` as "unset" elsewhere in this codebase.

## Design Notes

No second-DbContext precedent exists in this repo yet. `LeadDbContext` shape:

```csharp
public class LeadDbContext : DbContext
{
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
    public LeadDbContext(DbContextOptions<LeadDbContext> options) : base(options) { }
}
```

Registered in `Program.cs` next to Piranha's own `UseEF<MySqlDb>` call, reusing the same `connectionString`/`serverVersion`:

```csharp
builder.Services.AddDbContext<LeadDbContext>(db => db.UseMySql(connectionString, serverVersion));
```

## Verification

**Commands:**
- `dotnet build TbTruongHoc.sln` -- expected: 0 errors
- `dotnet test TbTruongHoc.sln` (against `docker compose up -d mariadb`) -- expected: all tests pass, including the new `LeadSubmissionTests`
- `docker compose up -d --build piranha-app` -- expected: app starts cleanly, migration applies; manually POST to `/api/leads` for each site's hostname and confirm a row appears with the correct `site_id`
