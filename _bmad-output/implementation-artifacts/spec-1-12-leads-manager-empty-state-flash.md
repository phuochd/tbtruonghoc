---
title: 'Story 1.12: Confirm (and fix if real) Leads Manager empty-state flash'
type: 'bugfix'
created: '2026-09-25'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 1.5's code review flagged (maybe-false, unverified) that the Leads Manager screen (`Leads.cshtml` / `manager-leads.js`) might briefly show the empty-state message "Chưa có khách để lại thông tin nào." before `/manager/api/lead/list` resolves, because `v-if="items.length !== 0"` has no loading check of its own. At the time nobody could inspect Piranha's compiled Manager CSS to settle it.

**Approach:** Settle the question from evidence: the CSS actually shipped in the installed `Piranha.Manager` 12.0.0 package, plus the Vue lifecycle behavior of `manager-leads.js`. If the flash is real, gate the empty-state branch on the first fetch having completed. If it is not real, close the story with the finding recorded and change no code (per the story's own third acceptance criterion).

</frozen-after-approval>

## Implementation Notes

**Finding: the flash does NOT happen. No code change made.**

Evidence:

1. **Piranha's compiled Manager CSS hides `.app` until it is ready.** The stylesheet embedded in `~/.nuget/packages/piranha.manager/12.0.0/lib/net8.0/Piranha.Manager.dll` (the same in `net9.0`) contains `.app{opacity:0;transition:opacity ease-in-out .2s}.app.ready{opacity:1}`. The DLL embeds four stylesheets: `full.min.css`, `slim.min.css` and their RTL variants. The rule appears exactly four times, once in each, so it applies whichever one Manager's `_Layout.cshtml` loads. Found with a byte search over the DLL. `ilspycmd` was not needed because the CSS is stored as plain text. The whole list/empty-state area in `Leads.cshtml` sits inside `<div class="app" :class="{ ready: !loading }">`, so both `v-if` branches have opacity 0 while `loading` is true. The raw template in the DOM before Vue mounts also has `class="app"`, so it is hidden as well.
2. **Nothing else adds `ready` early.** A search of the same DLL for `addClass('ready')`, `classList.add('ready')` and quoted `"ready"`/`'ready'` found no matches. Only this page's own `:class` binding controls it.
3. **`loading` only turns false after the first fetch settles.** `manager-leads.js` sets `loading: false` only in Vue 2's `updated` hook. Vue 2 does not call `updated` on the initial mount. The first `managerLeads.load(null, 'Tất cả site')` call writes `siteId = null` and `siteTitle = 'Tất cả site'`, which are identical to the initial data, so it triggers no re-render. It also runs only after `piranha.permissions.load` calls back. The first re-render therefore happens only when the fetch writes `items` (always a new array reference, so it re-renders even for `[]`) or `error`. At that point the correct branch is already in the DOM when `.ready` fades it in.
4. **Later filter switches cannot flash either.** `load()` never sets `loading` back to true or clears `items`, so the previous rows stay visible until the new response replaces them.

**Known edge case, left as is:** if the first fetch fails, `error` becomes true, the page re-renders, and `.app` becomes ready. It then shows the red error alert *and* the empty-state text underneath it. That is the failure path, not the pre-fetch flash this story covers. It is recorded in `deferred-work.md` (see Review Triage Log).

**Acceptance status:** AC3 (not real, close with the finding recorded) is met on the static evidence above. AC2 does not apply. AC1 asks for a manual, throttled click-through, which needs a Manager admin login. The agent does not have one (see Story 1.5's Implementation Notes), so AC1 is left for the human walkthrough. If the walkthrough *does* show a flash, this conclusion is wrong: reopen the story and apply the AC2 fix.

**AC1 result (human walkthrough, 2026-09-27):** Passed. With Slow 3G throttling and the cache disabled, a hard reload of `/manager/leads` showed no empty-state flash, and switching the site filter showed no empty box between result sets. The session-expiry path showed the red "Không thể tải dữ liệu…" alert as expected. That failure path is still covered by the deferred follow-ups. All three ACs are now settled (AC1 met, AC2 not applicable, AC3 met).

**Walkthrough steps for AC1:**
1. Make sure at least one lead exists (`POST /api/leads` or the public form). The flash only matters when there are real leads.
2. Log in to Manager. In DevTools → Network, disable the cache and set throttling to "Slow 3G".
3. Hard-reload `/manager/leads`. The list area should stay blank, with no "Chưa có khách…" box, until the table fades in. On a fast local server the load can finish before anything is visible. If so, use a custom throttle profile with extra latency.
4. Switch the site filter. The previous rows should stay visible until the new ones replace them, with no empty box in between.

Files changed: this spec file, `sprint-status.yaml`, and `deferred-work.md` (review follow-ups only).

## Review Triage Log

- **[medium, deferred]** When the first list fetch fails, the page shows the red error alert *and* the "Chưa có khách…" empty-state text below it. That text is false in this case, and it stays on screen rather than flashing. Verified in `Leads.cshtml`: the empty-state `v-else` has no `!error` condition. This is Story 1.5 code that this story did not introduce. The frozen intent rules out code changes when the flash is not real, so it goes to `deferred-work.md`. The fix is one line: `v-else-if="!error"`.
- **[low, deferred]** The no-flash result depends on an unwritten rule: `loading` turns false in Vue's generic `updated` hook, and nothing reactive may change before the first fetch settles. Verified in `manager-leads.js`. A later change could silently bring the flash back, for example a different default `siteTitle` or a preselected site. Deferred rather than patched because the frozen intent rules out code changes. The hardening is to set `loading = false` explicitly in `load()`'s `.then`/`.catch`.
- **[medium, deferred]** No loading indicator exists; the list area is blank (opacity 0) until data arrives. `epic-1-context.md` asks for "a proper loading state". This is Story 1.5 code, not introduced here. Deferred.
- **[low, rejected]** During a filter switch, the dropdown shows the new site's title above the old site's rows until the response arrives. The behavior is real, but it lasts one round-trip and is not caused by this story.
- **[low, rejected]** Fast filter switches can race, so an older response overwrites a newer one. Already triaged and rejected in Story 1.5's Review Triage Log for the same reason: a rare trigger on an internal tool, and it corrects itself on the next load.
- **[low, patched]** The CSS evidence identified the stylesheet only by the rule next to it. Fixed above: the rule is confirmed in all four embedded stylesheets.
- **[low, patched]** No per-AC status and thin walkthrough steps. Fixed above with **Acceptance status** and **Walkthrough steps for AC1**.
- **[low, rejected]** `deferred-work.md` and the Story 1.5 spec still show the item as open. The workflow forbids editing old deferred entries, and Story 1.5's spec is closed history. This spec and `sprint-status.yaml` are the record of the outcome.
