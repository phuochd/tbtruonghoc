# Sprint Change Proposal — 2026-09-23

**Project:** tbtruonghoc (Ngọc Anh Multi-Site Rebuild — Phase 1)
**Prepared by:** Amelia (Developer agent), via `bmad-correct-course`
**Mode:** Incremental

## 1. Issue Summary

Story 1.4 ("Form liên hệ/báo giá chung & lưu trữ lead", FR-3) was marked `done` in
`sprint-status.yaml` on 2026-09-23, but one of its published acceptance criteria in
`epics.md` — "an email notification fires automatically to the configured recipient"
— was never implemented.

This was not an oversight during coding. `spec-1-4-contact-quote-form-lead-storage.md`
explicitly excludes email ("Never send or attempt an email notification in this
story — deferred to a follow-up spec"), and the split is documented in
`deferred-work.md`: the story's draft spec ran ~2895 tokens against a 900–1600
target, so email was cut at the token-budget gate on 2026-09-22 to keep the story
independently shippable (the form + `FormSubmission` storage + inline confirmation
is a complete deliverable on its own).

The gap: when the email piece was split out, it was recorded only as a loose note
in `deferred-work.md` — it was never promoted to a tracked story in `epics.md` /
`sprint-status.yaml`. Meanwhile `epics.md`'s Story 1.4 AC text was left unchanged,
so the document still reads as if email is part of the (now `done`) story. Phước
flagged this from memory ("có tách phần gửi mail ra riêng") without the exact
detail; the specifics were reconstructed from `epics.md`, the spec, and
`deferred-work.md`.

**Additional relevant fact:** the architecture's own adversarial review
(`review-adversarial.md`, Finding 3.2) had already flagged that AD-3 mandates the
email-notification behavior but not a shared implementation point, recommending a
single `IFormNotificationService` to prevent multiple ad hoc SMTP implementations
appearing across forms/stories.

## 2. Impact Analysis

**Epic Impact:** Epic 1 ("Nền tảng dùng chung") is `in-progress` (1.1–1.6 done,
only the optional retro left). It can still be completed as planned — this is a
gap-fill, not a redesign. No other epic depends on the email piece directly; Story
1.5 (Manager lead list, already done) noted wanting future visibility into
notification-send status, but this is a nice-to-have, not a blocker.

**Story Impact:** One new story required — Story 1.7 — inserted into Epic 1, after
Story 1.6 and before Epic 2 begins. Story 1.4 needs a clarifying implementation
note (no AC changes) so it doesn't misrepresent its shipped scope.

**Artifact Conflicts:**
- **PRD:** none. FR-3 already states the "destination mechanism is an
  architecture-level decision" — it never hardcoded email.
- **Architecture:** none required to AD-3's Rule text itself (still accurate as the
  end-state). The adversarial review's shared-service recommendation is carried
  forward as a binding constraint inside Story 1.7's AC rather than a spine edit.
- **UX:** none. Email notification is backend-only, no new screens.
- **Other:** `epics.md`, `sprint-status.yaml`, and `deferred-work.md` need updates
  (see Section 4).

**Technical Impact:** New story requires SMTP/notification-service design (no
credentials, package, or config schema exist yet in the repo). Failure policy
already decided (fail-open, log-only, no inline retry) — carried into the new
story's AC so it isn't re-litigated.

## 3. Recommended Approach

**Selected: Option 1 — Direct Adjustment.** Add Story 1.7 within the existing
Epic 1 structure; update `epics.md`, `sprint-status.yaml`, and `deferred-work.md`
to close the loop. No rollback and no MVP/PRD review are warranted — Story 1.4's
shipped work (form + storage + inline confirmation) is correct and independently
valuable, and PRD/MVP scope is unaffected.

- **Effort:** Low — one well-scoped story, failure policy and architecture
  constraint already decided.
- **Risk:** Low — additive change, no rework of shipped code required.
- **Timeline impact:** One story inserted before Epic 2 starts; Site B priority
  sequencing (Tết 2027 deadline) is unaffected since Epic 2 hasn't started yet.

## 4. Detailed Change Proposals

### 4.1 `epics.md` — add Story 1.7 (after Story 1.6, before "## Epic 2")

```
### Story 1.7: Thông báo email khi có lead mới (FR-3, AD-3)

As a sales/ops user,
I want to receive an email the moment a new lead comes in,
So that I can follow up quickly without needing to check Piranha Manager proactively.

**Acceptance Criteria:**

**Given** a shared `IFormNotificationService` (single implementation, single outbound
email/SMTP configuration — never per-form ad hoc SMTP/API calls)
**When** a new `FormSubmission` row is created, from either form_type ("general" or
"survey"), on either site
**Then** an email notification is sent to the configured recipient(s) for that
submission's site, containing the submission's key fields (site, form type, name,
phone, product of interest/message, and location/service-area flag when present).

**Given** the email send fails (SMTP unavailable, misconfigured, or any transient error)
**When** the failure occurs
**Then** the `FormSubmission` row itself is unaffected (already committed) and the
visitor's inline success confirmation is unaffected — the failure is logged only,
never retried inline, never surfaced to the visitor (fail-open, per the 2026-09-22
decision recorded in deferred-work.md).

**Given** the notification recipient
**When** a site needs a different recipient than the other
**Then** the recipient address is configurable per site (via `SiteSettings` or
equivalent), not hardcoded.

**Given** no SMTP credentials or config keys currently exist in the repo
**When** this story is implemented
**Then** SMTP/API credentials are read from environment/`.env`-sourced configuration,
never hardcoded — consistent with the existing connection-string pattern.
```

### 4.2 `epics.md` — clarifying note appended to Story 1.4 (before "### Story 1.5")

```
> **Implementation note (2026-09-23, correct-course):** the "email notification"
> AC above is realized by Story 1.7, not this story. Story 1.4's shipped scope is
> the form + `FormSubmission` storage + inline confirmation only — the email send
> was split out at spec time (token-budget gate) and tracked separately.
```

### 4.3 `sprint-status.yaml` — add Story 1.7 entry (after Story 1.6, before
`epic-1-retrospective`)

```
  1-7-thông-báo-email-khi-có-lead-mới-fr-3: backlog
```

### 4.4 `deferred-work.md` — mark the email-notification entry resolved

Prefix the existing entry's `summary` with a resolved pointer to Story 1.7 (full
text kept for context); see approved diff in conversation.

## 5. Implementation Handoff

**Scope classification: Minor.** Direct implementation by Developer agent
(this workflow applies the four approved document edits directly; actual code
for Story 1.7 follows the normal `bmad-build` flow when picked up, on its own
branch per the project's branch-per-story convention starting Story 1.5).

**Responsibilities:**
- Developer (this session): apply the four approved edits to `epics.md`,
  `sprint-status.yaml`, `deferred-work.md`.
- Developer (future, via `bmad-build`): implement Story 1.7 — `IFormNotificationService`,
  SMTP config, fail-open error handling, per-site recipient — on branch
  `story/1-7-thong-bao-email-khi-co-lead-moi`.

**Success criteria:** `epics.md` and `sprint-status.yaml` both reflect Story 1.7
as a tracked backlog item with no AC ambiguity about what Story 1.4 actually
shipped; `deferred-work.md` no longer has an orphaned email-notification note.

---

## Addendum: Second trigger, same session — Story 1.8 (Manager site-tab switcher)

### Issue Summary

Two related items were logged in `deferred-work.md` during Story 1.3's walkthrough
(2026-09-20) but never promoted to tracked stories: (1) Piranha Manager's page list
becomes hard to navigate once a site has many pages — reaching the second site's
pages requires a lot of scrolling; (2) a request for a tab-based site switcher
instead of the stock dropdown. Phước recalled this from memory in this same
session and proposed the fix directly: make each Site its own tab in Manager's
page list, which resolves both — no more cross-site scrolling, and the tab row
itself shows how many sites exist at a glance.

### Impact Analysis

Same pattern as the email-notification gap: no PRD/architecture/UX conflict, Epic 1
absorbs it cleanly as a shared-platform-foundation item. Binds to AD-1 (single
Piranha instance, multi-site) rather than any single FR. One technical uncertainty
flagged for spec time: Piranha's stock page-list view is core Manager UI, not an
explicit extension point the way Story 1.5's Leads module was — the actual
implementation may need a Manager UI override rather than a clean add-on module.

### Recommended Approach

Option 1 — Direct Adjustment, same as the email item. Effort: Low–Medium (pending
the UI-override question above). Risk: Low.

### Detailed Change Proposal

**`epics.md`** — added Story 1.8 "Site switcher dạng tab trong Piranha Manager
(AD-1)" after Story 1.7, before "## Epic 2". Scope explicitly excludes
search/filter within a site's tab (Phước's choice, to keep this story focused on
the tab-isolation problem only).

**`sprint-status.yaml`** — added
`1-8-site-switcher-dạng-tab-trong-piranha-manager-ad-1: backlog` after Story 1.7's
entry.

**`deferred-work.md`** — both source entries (page-list scrolling, tab-switcher
request) marked resolved, pointing to Story 1.8.

### Implementation Handoff

Scope: Minor. Same as the email item — applied directly in this session;
implementation follows via `bmad-build` on its own branch
(`story/1-8-site-switcher-dang-tab-trong-piranha-manager`) when picked up.

---

## Addendum 2: Third trigger, same session — Story 1.9 (Security: validation bypass)

### Issue Summary

Phước reported still being able to save `javascript:...`/script values into
Zalo URL/Maps URL through Piranha Manager, despite Story 1.3/1.6's save-time
validation hook (`RegisterOnBeforeSave` in `Program.cs`) existing in code —
matching a "NEEDS RE-VERIFICATION" note already logged in `deferred-work.md`
from 2026-09-22. Live investigation in this session:

1. Restarted the `dotnet run` process from current source (ruled out "stale
   process serving old code").
2. Found and cleared two invalid placeholder GA4 IDs (`"xxxxxxxxxx"`,
   `"yyyyyyyyyyy"`) left in the dev database from manual Story 1.6 testing —
   real but unrelated finding; they were blocking `SiteSettingsTests`'
   cleanup step, not causing the reported XSS bug.
3. Ran the full `SiteSettingsTests` suite (11 tests) against the real dev
   MariaDB — all passed, including the two tests that specifically assert
   `javascript:`/`data:` scheme rejection.
4. Despite (3) passing, Phước re-tested against the freshly-restarted app and
   still reproduced the bug — meaning the passing tests were not representative
   of the real Manager UI save path.
5. Decompiled `Piranha.Manager.dll` (`ilspycmd`, a globally-installed dotnet
   tool) and found the actual root cause: Piranha Manager's own
   `SiteService.SaveContent()` always saves a `DynamicSiteContent` object, never
   the app's own `SiteSettings` POCO. The hook's `model is not SiteSettings
   settings` type-check always short-circuits for that type, so validation
   never runs for real Manager-driven saves — only for the app's own direct-save
   code path, which is exactly what `SiteSettingsTests` exercises.

**This is a confirmed, currently-exploitable, stored scheme-injection/XSS gap
in already-`done` Story 1.3/1.6 work**, not a deployment or test-infrastructure
problem.

### Impact Analysis

Affects Epic 1 (Stories 1.3, 1.6) — both already `done`, both need no AC
changes (their stated behavior — "unsafe URL/value is rejected" — is still the
correct target, just not actually met). No PRD/architecture conflict; this is
a defect against already-agreed requirements, not a scope change. No UX impact
(no visible change beyond the save now being correctly rejected).

### Recommended Approach

Option 1 — Direct Adjustment, tracked as **Story 1.9**, but flagged to be
picked up **ahead of** Stories 1.7/1.8 despite the story numbering, given it is
a live security gap in shipped work rather than a net-new feature. Phước chose
to track this as a backlog story rather than fix it live in this session.

### Detailed Change Proposal

**`epics.md`** — added Story 1.9 "[SECURITY] Sửa lỗ hổng bypass validation khi
lưu SiteSettings qua Piranha Manager" after Story 1.8, with full root-cause
detail embedded in the story so `bmad-build` doesn't need to re-investigate.

**`sprint-status.yaml`** — added
`1-9-security-sửa-lỗ-hổng-bypass-validation-sitesettings-manager: backlog`.

**`deferred-work.md`** — the original "NEEDS RE-VERIFICATION" entry updated
with the real root cause and a pointer to Story 1.9; its Docker-rebuild theory
noted as moot (app no longer runs via Docker). The entry's other half (182
debris test pages) is flagged as still open and NOT covered by Story 1.9.

### Implementation Handoff

Scope: Minor (well-scoped fix, root cause already identified) but
**security-sensitive — recommend prioritizing ahead of 1.7/1.8** when next
picking a story via `bmad-build`. Branch:
`story/1-9-security-sua-lo-hong-bypass-validation-sitesettings-manager`.

### Session housekeeping

The local dev app (`dotnet run`) was stopped and restarted during this
investigation and is left running on `http://localhost:5000` with current
source. Two invalid placeholder GA4 values were cleared from the dev database
(non-production, pre-launch data only).

**Scope update (same session):** at Phước's request, the separately-tracked
"182 test-debris pages in the shared dev database" item (originally logged in
`deferred-work.md` alongside the validation-bypass note, same Story 1.3
walkthrough on 2026-09-20) was folded into **Story 1.9** rather than kept or
tracked separately — re-checked during this session at ~105 debris rows (down
from 182, but not resolved). Story 1.9's title and AC were updated to cover
both: the validation-bypass security fix, and a one-time DB cleanup + test-suite
fix (tests must delete the pages they create, or use an isolated database) so
future `dotnet test` runs stop adding new debris. `epics.md`, `sprint-status.yaml`,
and `deferred-work.md` all updated accordingly.

---

## Addendum 3: Full deferred-work.md sweep, same session

Phước asked whether an empty `deferred-work.md` would mean everything was
addressed — it wasn't empty yet (6 items beyond the 3 already handled above
were still open). At Phước's direction, all but one were triaged into tracked
work in this same pass:

- **Piranha version wording ("12.2.0" vs "12.0.0")** — resolved directly as a
  docs-only fix, no story needed. Confirmed via `dotnet test` build-warning
  text that every `Piranha.*` package actually pinned in this repo is `12.0.0`
  (the `.csproj` pins were never bumped to the later `12.2.0` NuGet release
  that inspired the original stack-table wording). Updated: `README.md`,
  `ARCHITECTURE-SPINE.md` (stack table + EOL-risk note), `epics.md`
  (constraints line + Story 1.1's own AC), `epic-1-context.md`, and
  `spec-tbtruonghoc/SPEC.md`.
- **Story 1.10** — "GA4 script an toàn — chặn theo môi trường & cookie-consent
  gate trước go-live". Two product decisions Phước made this session: (1)
  env-gate the GA4 script in code so it never fires outside
  `ASPNETCORE_ENVIRONMENT=Production`, regardless of what's in `SiteSettings`;
  (2) add a cookie-consent gate before go-live (not a hard legal requirement
  for Vietnamese SMB sites, but chosen as the safer default). Combined into one
  story since both touch `_Analytics.cshtml`.
- **Story 1.11** — "Smoke-test script — MariaDB restart/recovery". A
  `.ps1`/`.sh` script (outside the shared-DB xUnit suite) that stops/restarts
  the `mariadb` container and confirms the app recovers with no duplicate
  `Site` rows — automating what was previously only verified once by hand
  during Story 1.1.
- **Story 1.12** — "Xác nhận & sửa hiệu ứng nháy màn hình rỗng ở Leads
  Manager". First confirms whether the empty-state flash is real (Piranha's
  compiled Manager CSS wasn't source-browsable during Story 1.5's review), now
  that `ilspycmd` is known to be available for decompiling it; fixes
  `Leads.cshtml`'s loading-state check only if confirmed real.
- **FormType "survey" test coverage** — the one item left genuinely deferred.
  Correctly blocked on Story 6.5 (Epic 6) actually wiring up the survey form;
  nothing to build against yet.

After this pass, `deferred-work.md` has exactly one open entry (the
FormType/Story 6.5 item), down from 9. Epic 1 now totals **12 stories**
(1.7–1.12 all added this session via correct-course).
