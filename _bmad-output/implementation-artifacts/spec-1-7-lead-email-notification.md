---
title: 'Email notification on new lead (Story 1.7, FR-3, AD-3)'
type: 'feature'
created: '2026-09-24'
status: 'done'
route: 'dispatch'
baseline_commit: '109e688b0f20a4bbe2a952f25da2b217e6747398'
review_loop_iteration: 1
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A new `FormSubmission` is only visible if someone opens Manager's Leads screen, so sales can miss or answer leads late. Story 1.4 split the email step out, and no SMTP configuration or notification code exists yet.

**Approach:** Add one shared `IFormNotificationService`. `LeadsController` calls it only after `SaveChangesAsync` succeeds. The service drops the submission onto an in-memory queue. A hosted worker then reads the site's recipients from a new `SiteSettings.NotificationEmails` field and sends one plain-text email in Vietnamese through a single SMTP sender (MailKit). SMTP settings come from the `Smtp` config section: user-secrets in dev, `Smtp__*` environment variables in prod.

## Boundaries & Constraints

**Always:** Fail-open. The visitor always gets the same 200 plus inline confirmation, whether queuing or sending fails. Failures are logged only and never retried. Recipients are per site, can be several (comma- or semicolon-separated), and are editable in Manager. Save-time validation of `NotificationEmails` goes into the existing `RejectUnsafeValues` hook, on BOTH the typed `SiteSettings` path and the `DynamicSiteContent` path (the Story 1.9 lesson). Send-time code re-checks each address and skips invalid ones. Visitor-entered values are stripped of CR/LF and control characters before they go into the subject line. Tests never send real email and delete every `FormSubmission` row they create.

**Never:** No inline/synchronous SMTP call in the request. No retry or durable outbox. No DB migration or "notification status" column. No HTML email body. No per-form or per-site SMTP configuration. No hardcoded recipient or credential. No fallback to a global recipient.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Happy path, Site A | POST on Site A; its `NotificationEmails` = `a@x.vn; b@x.vn` | 200. One email to both addresses containing site title, form type, name, phone, product, message | N/A |
| Site isolation | POST on Site B | Email goes only to Site B's recipients, never to Site A's | N/A |
| Survey fields | Submission has `LocationAddress` / `IsOutsideServiceArea` | Body includes the address and the service-area line. Both lines are omitted when null | N/A |
| No recipient | Site's field is empty | 200, no send | Log warning |
| SMTP not configured | `Smtp:Host` or `Smtp:FromAddress` empty | 200, no send | Log warning |
| Send throws | SMTP down or auth failure | 200, row intact, worker keeps running | Log error, no retry |
| Invalid recipient saved | `not-an-email` or `a@x.vn\r\nBcc:` via either save path | `ValidationException`, value not persisted | Cache evicted, like the existing fields |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Controllers/LeadsController.cs` -- `Create`: the only write path. Call the service after `SaveChangesAsync`. A failure in that call must not move execution into the existing catch block that returns 500.
- `src/TbTruongHoc.Web/Models/SiteSettings.cs` -- add the `NotificationEmails` StringField region, same style as the existing fields.
- `src/TbTruongHoc.Web/Models/SiteSettingsValidation.cs` -- add `IsValidNotificationEmailList` and `ParseNotificationEmails` (split on `,;`, trim, drop blanks; each entry must parse with `MailAddress` and equal its `.Address`, i.e. no display name and no CR/LF).
- `src/TbTruongHoc.Web/Program.cs` -- extend `RejectUnsafeValues` (both branches, using `Raw(...)` for the dynamic one). Register the options, the queue/service singleton, the hosted worker and the SMTP sender.
- `src/TbTruongHoc.Web/Models/FormSubmission.cs` -- read-only input.
- `IApi.Sites.GetByIdAsync` / `GetContentByIdAsync<SiteSettings>` -- how the worker resolves the site title and recipients inside its own DI scope.
- `tests/TbTruongHoc.Web.Tests/PiranhaWebApplicationFactory.cs` -- the only factory; `App.Init` cannot run twice, so no `WithWebHostBuilder`. Swap in the fake sender here via `ConfigureTestServices`.
- `tests/TbTruongHoc.Web.Tests/SiteSettingsTests.cs` -- reuse the `SnapshotAsync`/`RestoreAsync`/`LoadDynamicContentAsync`/`SetRegionValue` patterns. The snapshot must also capture `NotificationEmails`.

## Tasks & Acceptance

**Execution:**
- [x] `src/TbTruongHoc.Web/TbTruongHoc.Web.csproj` -- add `MailKit` (latest stable) -- `System.Net.Mail.SmtpClient` is discouraged by Microsoft.
- [x] `src/TbTruongHoc.Web/Models/SiteSettings.cs`, `SiteSettingsValidation.cs` -- add the field plus parse/validate helpers.
- [x] `src/TbTruongHoc.Web/Notifications/` -- `SmtpOptions`; `NotificationEmail` record (To, Subject, Body); `INotificationEmailSender` + `SmtpNotificationEmailSender` (MailKit, 30s timeout, `SecureSocketOptions.SslOnConnect` when `Port` is 465, otherwise `SecureSocketOptions.StartTls` — TLS is mandatory, never `Auto`/`StartTlsWhenAvailable`, which silently fall back to cleartext; skip + warn when not configured); `IFormNotificationService` + `FormNotificationService` (bounded `Channel` of 100 items, `TryWrite` never throws, warn when the channel is full); `FormNotificationWorker : BackgroundService` (per item: new scope, resolve recipients and site title, compose, send; catch and log everything); `LeadEmailComposer` (pure function) -- core feature.
- [x] `src/TbTruongHoc.Web/Program.cs` -- DI registration and hook extension.
- [x] `src/TbTruongHoc.Web/Controllers/LeadsController.cs` -- inject the service and call it after the commit.
- [x] `src/TbTruongHoc.Web/appsettings.json`, `README.md` -- add an empty `Smtp` section; document the user-secrets and `Smtp__*` env keys.
- [x] `tests/.../PiranhaWebApplicationFactory.cs`, `RecordingEmailSender.cs`, `LeadNotificationTests.cs` -- cover every matrix row. Composer and validation get direct unit tests. The rest go through real HTTP POSTs plus a wait on the fake (5s timeout). Update `SiteSettingsTests`' snapshot.

**Acceptance Criteria:**
- Given the full suite runs against real MariaDB, when `dotnet test` finishes, then all existing tests plus the new ones pass and no real SMTP connection is attempted.
- Given a lead email, when a salesperson reads it, then the subject reads `[<site title>] Khách mới: <name> – <phone>` and times in the body are shown as Vietnam time (UTC+7) in `dd/MM/yyyy HH:mm` format.

## Design Notes

A background queue rather than an awaited send, because MailKit connect/auth can hang for tens of seconds and the visitor would feel that delay. Queued items are lost if the process restarts before sending. That is acceptable: the lead is already committed and Manager is the source of truth. Composition happens in the worker, not in the controller, so the controller only depends on `IFormNotificationService`.

## Verification

**Commands:**
- `dotnet build TbTruongHoc.sln` -- expected: 0 errors
- `dotnet test TbTruongHoc.sln` (MariaDB up, connection string in user-secrets) -- expected: all pass

**Manual checks:**
- Fill in a Site's "Notification emails" field in Manager, set `Smtp:*` in user-secrets, submit the form, and check the inbox.

## Implementation Notes

- MailKit 4.18.0. Sender opens one connection per email (low volume, no stale socket state).
- Queue = `Channel.CreateBounded(100, FullMode=Wait)` so `TryWrite` returns false on full (logged warning) rather than silently dropping. The service enqueues a detached copy of the tracked entity.
- Controller: notify call sits after the existing try/catch (so it can never reach the 500 branch) and has its own try/catch guard.
- Validation also rejects any control character in the raw field value (not just per trimmed entry), so a trailing CR/LF can't be persisted.
- Composer sanitizes site title, name and phone (subject and body lines); Message/Product stay verbatim in the plain-text body. Time uses a fixed UTC+7 offset (Vietnam has no DST).
- "SMTP not configured" matrix row is covered by a direct `SmtpNotificationEmailSender` unit test (the test host swaps the sender out). "Survey fields" row is covered at the composer level (the endpoint never populates those columns yet).
- Pre-existing `LeadSubmissionTests` still leave their rows behind (not touched by this story); new tests delete every row they create.

- Loop 1: re-derived by applying the iteration-0 patch, then (a) `SmtpNotificationEmailSender.SelectTls(port)` = `SslOnConnect` on 465, `StartTls` otherwise (a relay without STARTTLS now fails loudly, which is intended), README TLS sentence updated to match; (b) every `[patch]` row in the Review Triage Log applied, with tests for each. Verified: `dotnet test` 92/92 against real MariaDB. It ran with `--artifacts-path` inside the repo because a running dev instance (PID 19000) locks `bin/Debug`.

## Spec Change Log

- **Loop 1 (2026-09-24).** Trigger: blind-hunter finding, verified against MailKit 4.18.0's own XML docs. `SecureSocketOptions.Auto` means `SslOnConnect` only on port 465, and `StartTlsWhenAvailable` on every other port. So on the default port 587, a relay that doesn't offer STARTTLS, or an attacker who strips it, gets `AuthenticateAsync` sending the SMTP username and password in cleartext, followed by the visitor's name and phone. Amended: the Execution task for `SmtpNotificationEmailSender` now requires `SslOnConnect` on 465 and `StartTls` otherwise, and never `Auto`/`StartTlsWhenAvailable`. Known-bad state avoided: SMTP credentials and lead PII sent unencrypted. **KEEP:** the iteration-0 implementation passed 80/80 tests and reviewers found it structurally sound. It is preserved as a unified diff against `baseline_commit` at `C:/Users/phuoc/AppData/Local/Temp/claude/d--project-tbtruonghoc/5f06c6ef-91a0-42eb-b7a5-715ec753f5ea/scratchpad/story-1-7-iteration-0.patch`. Re-derive by applying that patch (`git apply`) and keep everything in it: queue/worker/composer design, the controller call placed after the try/catch, validation on both save paths, `RecordingEmailSender` in the shared factory, and every existing test. Then change the TLS selection as amended and update the README's TLS sentence to match (TLS required; no cleartext fallback).

## Review Triage Log

- **[medium, bad_spec]** (blind-hunter) `SecureSocketOptions.Auto` can send SMTP credentials and lead PII in cleartext. Verified in MailKit 4.18.0's XML docs: `Auto` becomes `StartTlsWhenAvailable` on every port except 465. The spec's Execution task prescribed `Auto`. That drove the loop-1 amendment (see Spec Change Log).
- **[medium, patch]** (verification-gap + blind-hunter, one root cause: the hand-written detached copy in `FormNotificationService.NotifyNewSubmission` is never verified end to end) Dropping `CreatedAt`, `LocationAddress` or `IsOutsideServiceArea` from the copy fails no test. The happy-path HTTP test never asserts the `Thời gian:` line, and the survey lines are tested only at the composer. Fix: assert the Vietnam-time line against the saved row's `CreatedAt` in the happy-path test, and add one test that calls the resolved `IFormNotificationService` directly with every field set and checks the time, address and service-area lines in the recorded email.
- **[low, patch]** (edge-case + blind-hunter, one root cause: the composer's empty-value handling is inconsistent) `LocationAddress` is checked with `!= null`, so an empty or whitespace address prints a bare `Địa chỉ: ` line, while Product and Message use `IsNullOrWhiteSpace`. Fix: use `IsNullOrWhiteSpace` for the address as well.
- **[low, patch]** (edge-case + blind-hunter) Duplicate recipients (`a@x.vn; A@x.vn`) are all added to `To`. Fix: a case-insensitive `Distinct` on the worker's recipient list. The "no entry cap" part is rejected: the list is admin-entered, and a cap would add a new rule and a validation branch.
- **[low, patch]** (edge-case) `SanitizeForHeader` only replaces `char.IsControl`, so U+2028/U+2029, tab and NBSP survive and the subject is not collapsed to one line as its doc comment claims (MimeKit's RFC 2047 encoding still blocks header injection, so this is cosmetic). Fix: also replace `char.IsWhiteSpace` characters with a space before collapsing.
- **[low, patch]** (verification-gap) The composer's `"Website"` fallback title (null, empty or control-only title) is untested. Fix: add one pure `[Theory]`.
- **[low, patch]** (verification-gap + blind-hunter) The queue-full branch is untested, and it is the path that keeps the POST non-blocking when the queue backs up. Fix: a unit test that creates `FormNotificationService` with no reader, calls it `Capacity + 1` times, and asserts that each call returns synchronously and that exactly one warning is logged.
- **[low, patch]** (blind-hunter) The README's SMTP section is inserted between "...overrides user-secrets." and "Then visit:", which breaks the run-then-visit flow. Fix: move the section after the "Then visit" list.
- **[low, patch]** (blind-hunter) Queued notifications dropped on shutdown leave no log trace. Fix: log `Reader.Count` as a warning in the worker's existing shutdown `catch (OperationCanceledException)` when it is > 0.
- **[low, patch]** (blind-hunter) `RecordingEmailSender.DefaultTimeout` of 5s may be tight on a cold machine with shared MariaDB (each wait covers POST, commit, two Piranha lookups and the send). Fix: raise the constant to 15s. This is a direct constant change.
- **[low, rejected]** (edge-case) If one recipient is rejected at RCPT TO, or fails `MailboxAddress.Parse`, the whole email is aborted, so valid co-recipients get nothing. This is real only when an admin saves a mailbox the relay rejects, and the error is logged. The fix needs MailKit's per-recipient override: new branching, more than a direct correction.
- **[low, rejected]** (edge-case + blind-hunter + verification-gap, one root cause: the real MIME/SMTP path has no automated test, and the validator (`MailAddress`) and sender (MimeKit) parse differently) Current code reads correctly: From, To loop, subject and plain-text UTF-8 body. No address was shown that passes `MailAddress` with `.Address == entry` yet fails `MailboxAddress.Parse`. Covering it needs an in-process SMTP server as a new test dependency. It is left to the spec's Manual check (real SMTP inbox), which the user must run before release.
- **[low, rejected]** (edge-case) Recipients are read at send time, not at enqueue time. The gap is seconds, and reading current settings is the intended behaviour.
- **[low, rejected]** (edge-case) If `DisconnectAsync` throws after a successful `SendAsync`, a delivered email is logged as failed. Rare; the only harm is a misleading log line, and the fix adds a try/catch guard.
- **[false]** (edge-case) The claim that `FormType` could be null or unmapped and print a blank label. `LeadsController.Create` always coerces it to `general` or `survey` before saving, and the column is `IsRequired`.
- **[low, rejected]** (edge-case) The claim that Name or Phone could be empty after sanitisation (control-character-only input). It needs a deliberately crafted request; the lead is still delivered with an odd subject, and the fix adds branches.
- **[low, rejected]** (blind-hunter) SMTP options are not validated at startup, and "not configured" only warns per lead. That per-lead warning is exactly the frozen matrix's specified handling; startup validation adds new surface.
- **[low, rejected]** (blind-hunter) The worker's invalid-stored-address filter, the no-recipient warning, and the controller's defensive catch are untested. Invalid stored values can't be persisted: both save paths reject them, as tested. The controller catch guards a service that never throws.
- **[low, rejected]** (blind-hunter) The email has no direct Manager link and no `tel:` phone. This is a content enhancement, not a defect. It needs a host/base-URL decision outside this change, and the plain-text body already shows the phone number.
- **[false]** (blind-hunter) The claim that leaving the spec and sprint-status out of the review diff is an error. The exclusion is deliberate: the claims file goes only to the edge-case layer, by path.

**Loop 1 review pass**

- **[low, patch]** (verification-gap) No test proves that a failed save sends no email. The persistence-failure 500 path never checks `Sender.Attempts`, so moving `NotifyNewSubmission` before `SaveChangesAsync` would fail no test. The controller is correct today. Fix: a test that configures recipients, POSTs an oversized-message lead (same trigger as `LeadSubmissionTests`' persistence-failure test), then a sentinel lead, and asserts no attempt carries the failed lead's name.
- **[low, patch]** (blind-hunter) The README's "leave Username blank for an unauthenticated relay" now conflicts with mandatory STARTTLS: a plaintext-only local relay on port 25 fails on every lead. Fix: state in the README that the relay must offer STARTTLS (or implicit TLS on 465). No opt-out setting (new surface; TLS was made mandatory in loop 1).
- **[low, rejected]** (edge-case + blind-hunter) carried: recipients are validated with `MailAddress` but sent with `MailboxAddress.Parse`; one unparseable recipient aborts the whole email.
- **[low, rejected]** (blind-hunter + verification-gap) carried: the real MIME/SMTP path has no automated test; left to the spec's manual inbox check.
- **[low, rejected]** (blind-hunter + edge-case) carried: SMTP options (malformed `FromAddress`, port range, password without username) are not validated at startup.
- **[low, rejected]** (blind-hunter) carried: the worker's invalid-stored-address filter is untested; both save paths reject such values.
- **[low, rejected]** (blind-hunter) The copy in `NotifyNewSubmission` may miss future `FormSubmission` fields, and detachment isn't tested by mutating the original. Current fields are covered end to end by `Service_Copies_Every_Field_And_Worker_Dedupes_Recipients`. A reflection-based guard adds new test machinery for a hypothetical field.
- **[low, rejected]** (blind-hunter) An SMTP outage or full queue drops emails with only log output, and there is no metric or health signal. This is the frozen matrix's specified handling (log only); the lead stays in Manager.
- **[false]** (blind-hunter) The claim that `Queue_Full_Returns_Immediately_And_Warns_Once` is misnamed. The test overflows by exactly one item, so "warns once" is exactly what it asserts; the service's per-drop warning is intended.
- **[low, rejected]** (blind-hunter) Timed-out waiters are never removed from `RecordingEmailSender._waiters`. That only happens after a test has already failed on timeout, and the leak is limited to the test process.
- **[false]** (blind-hunter) The claim that the subject length is uncapped. `LeadSubmissionRequest` caps Name at 200 and Phone at 50 (`[StringLength]`), so the subject is bounded to about 280 characters.
- **[false]** (blind-hunter) carried: the claim that an unmapped form type prints raw. The controller coerces every value to `general` or `survey`.
- **[low, rejected]** (blind-hunter) The README doesn't document the queue cap, skipping of invalid addresses, de-duplication or the 30s timeout. These are operational details, not defects.
- **[low, rejected]** (edge-case) A notification in flight when shutdown cancels it is not counted in the shutdown warning. It is at most one item, and fixing it adds in-flight tracking state.
- **[low, rejected]** (edge-case) The claim that `RecordingEmailSender.DefaultTimeout` (15s) contradicts the spec's "5s timeout". 15s was the triage-approved fix in the first pass; the only remaining fix would be editing this build's spec.
