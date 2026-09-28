---
title: 'Story 4.2: Shared config block with editor-chosen input types (FR-11)'
type: 'feature'
created: '2026-09-28'
status: 'done'
baseline_commit: 'a4869994941a31ee8cd9195b22377bc3bda11916'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-4-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** 4.1's config reference is a read-only table hard-wired to `ProductArchive`. Buyers cannot pick options, and other product lines (thùng rượu, bồn tắm) and page types (PDP, landing) cannot use it.

**Approach:** Replace it with a Piranha block group, "Bảng cấu hình" (optional heading, child config rows). The editor gives each row an input type: Chỉ hiển thị / Text / Option / Check / Multi-option. It renders inline wherever the editor places it among the page's blocks on ProductArchive, ProductPost and LandingPage. The visitor's selections travel with the page's one quote form into the stored lead.

## Boundaries & Constraints

**Always:**
- CMS-driven only; no seeded or hardcoded option values. Every selection is optional.
- Row validity: label non-blank, and Chỉ hiển thị needs a value, Option/Multi-option need ≥1 non-blank choice (one per line, trimmed, editor order). Invalid rows are omitted. No valid rows → nothing renders (no heading, disclaimer, form or script).
- Exactly one quote form per page: archive = block brings `_QuoteRequestForm` (prefill "Tư vấn cấu hình – {title}", 200-char cap, formType `general`, as 4.1); PDP/landing = block feeds the existing form, adds none.
- Disclaimer "Bảng tham khảo, không tính giá tự động" exactly once when the block renders. All editor text HTML-encoded.
- Every control has a real `<label>` (Multi-option groups in `<fieldset>`/`<legend>`), is keyboard-operable, ≥44px tap target, fits 375px with no sideways scroll. Without JS the block is a readable reference.
- Lead schema unchanged: selections fit into the existing `Message` (≤2000) field.
- Mộc Trầm `.sb-config*` look (surface-sunken panel, border, rounded.md, body-sm, caption disclaimer).
- Before deleting 4.1's regions, confirm the local DB holds no `DrumConfig`/`DrumConfigTitle` field content; if it does, HALT and ask.

**Never:** price computation/display, cart, "Đặt mua ngay", urgency copy; new lead columns/JSON; changing `LeadSubmissionRequest`, `FormSubmission`, `/api/leads`, Site A views or `Areas/Manager`; a second quote form on any page.

**Decisions (2026-09-28, Phước):**
- **Position = inline, editor-placed.** The block renders in the page's normal block loop at the editor's position, not at a fixed slot. On archives that is the block area above the grid; 4.1's after-pager slot is dropped. This overrides the epic's "fixed, documented position" AC.
- **Option = native `<select>`** with an empty first entry "— Chưa chọn —" (≥44px). Multi-option = checkboxes; Check = one checkbox; Text = a text input.
- **Selections are sent hidden, prepended at submit:** the Lời nhắn box stays the visitor's own. At submit the lead `Message` becomes "Cấu hình đã chọn: {summary}

{typed message}".
- Default heading when blank: "Bảng cấu hình".
- Spec size ~2300 tokens kept whole by user choice.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| No block | Archive/PDP/landing without the block | Page identical to today; archive has no form and no `lead-form.js` | N/A |
| Archive full | Block with valid rows of all 5 types | At its block position: heading, rows in editor order, disclaimer, one prefilled form; `lead-form.js` + `sb-config.js` included | N/A |
| PDP / landing | Block with valid rows | Block at its block position with no form of its own; the page's existing form is the only one | N/A |
| Invalid rows | Blank label; Option with no choices; Chỉ hiển thị with blank value | Those rows omitted | N/A |
| All invalid | Rows exist, none valid | Treated as "No block" | N/A |
| Two blocks | Editor adds two config blocks | Only the first block with valid rows renders; the others render nothing | N/A |
| Selections submitted | Kích thước=60cm (Option), Loại 2 (Option), Bánh xe checked, Sơn text "đỏ" | Lead `Message` starts with "Cấu hình đã chọn: Kích thước: 60cm; Loại: 2; Bánh xe: Có; Sơn: đỏ" | N/A |
| Nothing selected | Visitor submits without touching the block | `Message` exactly what the visitor typed | N/A |
| Over length | Summary + typed message > 2000 chars | Summary truncated so the total ≤ 2000; the typed message is kept whole | N/A |
| HTML in fields | `<b>x</b>` in label/choice/heading | Literal text | Encoded |
| Unsupported page | Block added to a StandardPage/Post/ProductHub | Renders nothing | N/A |
| Heading blank | Valid rows, heading empty | `<h2>` = "Bảng cấu hình" | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Models/ProductArchive.cs:32-69` -- delete `DrumConfigTitle`, `DrumConfig`, `ConfigTitleText`, `ConfigRows`, `DefaultConfigTitle`, `DrumConfigRow`; update the class summary. Types update on startup via `ContentTypeBuilder...DeleteOrphans()` (`Program.cs:276-279`).
- New `Models/ConfigBlock.cs` -- `[BlockGroupType(Name="Bảng cấu hình", Category="Sản phẩm", Icon=…)]` + `[BlockItemType(typeof(ConfigRowBlock))]` `ConfigBlock : BlockGroup` with `StringField Heading`. `[BlockType(Name="Dòng cấu hình", IsUnlisted=true, IsGeneric=true, ListTitle="Label")]` `ConfigRowBlock : Block` with `StringField Label`, `SelectField<ConfigInputType> InputType`, `TextField Choices` (one per line; value for Chỉ hiển thị). Enum values carry `[Display(Description="Chỉ hiển thị")]` etc. for Manager titles. Put the validity/trim/split logic here (a `ValidRows` helper) so views and tests share it.
- `Program.cs` inside `UsePiranha` after `App.Init` (~L131) and before the `ContentTypeBuilder` -- `App.Blocks.Register<ConfigBlock>()`, `App.Blocks.Register<ConfigRowBlock>()`, `App.Fields.RegisterSelect<ConfigInputType>()`. The project uses no SelectField yet.
- Rendering: new `Views/Cms/DisplayTemplates/ConfigBlock.cshtml`, reached by the existing `@Html.DisplayFor(m => block, block.GetType().Name)` loops. The host view tells it what to do through ViewData set before its loop. `Views/Cms/ProductArchive.cshtml`: own form, prefill "Tư vấn cấu hình – {Model.Title}". `ProductPost.cshtml:108` and `LandingPage.cshtml:125`: feed the existing form. With no host flag (Page/Post/Archive/ProductHub/CraftsmanStory views) it renders nothing. DisplayFor copies ViewData, so a child cannot signal back. Track "already rendered one" in `HttpContext.Items`.
- `ProductArchive.cshtml`: remove the `_DrumConfigReference` call (L71) and `hasConfig`. Include `lead-form.js` + `sb-config.js` when `Model.Blocks` has a `ConfigBlock` with valid rows. PDP/landing: add `sb-config.js` under the same condition (they already load `lead-form.js`). Delete `Views/Shared/_DrumConfigReference.cshtml`.
- Control names/ids must be unique per row (`sb-config-{i}`, `sb-config-{i}-{j}`) and must sit outside the quote `<form>` (lead-form.js reads fields by `name` inside its form).
- New `wwwroot/assets/js/sb-config.js` -- builds "Label: value" parts (Multi-option values joined ", ", Check = "Có", blank Text omitted, Chỉ hiển thị excluded), joined "; ". Minimal hook in `lead-form.js` `submitForm()`: if the page has `[data-sb-config]` with a non-empty summary, message = "Cấu hình đã chọn: {summary}\n\n{message}", summary trimmed so the total ≤ 2000. Include `sb-config.js` only when the block renders.
- `wwwroot/assets/css/site-b.css:947-1034` -- keep `.sb-config*` panel/disclaimer/form styles; replace table rules with row/control rules (select/checkbox rows ≥44px, inputs full width, `overflow-wrap:anywhere`). Fix the `.sb-spec` comment at L905.
- Tests: rewrite `tests/TbTruongHoc.Web.Tests/DrumConfigReferenceTests.cs` → `ConfigBlockTests.cs` (reuse `WithCatalogAsync`, `GetHtmlAsync`, `Decode`, `Section` from `ProductCatalogTests`); update `CraftsmanStoryTests.cs:134` (4.1 region use) to the block, or drop its now-obsolete order assertion. Landing-page tests live alongside Story 3.x tests.

## Tasks & Acceptance

**Execution:**
- [x] `Models/ConfigBlock.cs` -- block group, row block, enum, `ValidRows` helper.
- [x] `Program.cs` -- register blocks and the select field.
- [x] `Models/ProductArchive.cs` -- after the DB check, remove the 4.1 regions/helpers/row class.
- [x] `Views/Cms/DisplayTemplates/ConfigBlock.cshtml` -- render per the matrix; delete `_DrumConfigReference.cshtml`.
- [x] `Views/Cms/ProductArchive.cshtml`, `ProductPost.cshtml`, `LandingPage.cshtml` -- set the host ViewData flag, include scripts.
- [x] `wwwroot/assets/js/sb-config.js`, `lead-form.js` -- summary + message prepend.
- [x] `wwwroot/assets/css/site-b.css` -- control styles.
- [x] `tests/.../ConfigBlockTests.cs` (+ `CraftsmanStoryTests.cs`) -- one test per matrix row (JS-only rows assert the markup/data attributes that feed the summary), plus the ACs.

**Acceptance Criteria:**
- Given the Manager, when an editor edits a ProductArchive/ProductPost/LandingPage, then "Bảng cấu hình" is in the block picker, its rows offer the 5 input types, and "Dòng cấu hình" is not listed at top level.
- Given any page with the block, then there is no "Đặt mua", no price text from the block, no `<form action`, and the page has exactly one `data-quote-request-form`.
- Given the full suite, when run, then all pre-existing tests (except the replaced 4.1 tests) pass.

## Implementation Notes

- **DB check (2026-09-28).** The local DB held 4.1 content: 5 `DrumConfig` rows on the ProductArchive "Chùa" (Kích thước/NULL, Loại/"Loại 1, Loại 2, Loại 3", Bánh xe/"Có, Không", Sơn/"Trần, Sơn đỏ", Vẽ mặt trống/NULL) and NULL `DrumConfigTitle` rows on "Chùa", "Thùng rượu gỗ", "Bồn tắm gỗ sồi". Work halted and asked. **Phước confirmed they are throwaway walkthrough data: remove the regions as specced, no migration, no backup.** The rows remain orphaned in `Piranha_PageFields`; "Chùa" still renders 200 with no config section.
- Hosts pick the block with `ConfigBlock.FirstRenderable(Model.Blocks)` and skip every other `ConfigBlock` in their loop, so invalid/extra blocks leave no empty `.block` wrapper. The Code Map's `HttpContext.Items` flag was dropped in review as dead code. The other page views (Page/Post/Archive/ProductHub/CraftsmanStory) skip `ConfigBlock` in their loops. `ConfigInputType.Text` is the 0/default member, so a new row defaults to a valid type.
- Block fields default to non-null instances: Piranha's block serializer throws on a null field (seen when a block is built in code without a heading).
- Script behaviour (summary, nothing selected, over length) is tested by running the real `sb-config.js` + `lead-form.js` in Jint (`ConfigBlockScriptTests.cs`); the summary is cut with a trailing "…" so the total is exactly ≤ 2000; if the typed message leaves no room, it is sent as typed.

## Spec Change Log

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V):

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| E1/E7 | Non-host views (Page/Post/Archive/ProductHub/CraftsmanStory) still emit an empty padded `.block` wrapper for a ConfigBlock | medium | The generic loops wrap every block before calling the template that renders nothing. The block is in the picker everywhere, so this is a visible gap on those pages → patch (skip ConfigBlock in those loops; test asserts no wrapper). |
| B2 | Picker offers the block on every page type with no hint that it only shows on archive/PDP/landing | low | Real, and the fix is one line of editor help text → patch (Heading field Description). |
| B3/E5 | Default input type is `Display` (enum 0): a row with only a label is silently hidden, and an unparsable value also falls back to Display | medium | `SelectField` defaults to `default(T)`, so every new row starts as Chỉ hiển thị, and a blank Choices drops it. That happens in everyday editing → patch (make `Text` the 0/default member; no stored content exists). |
| B10/V1 | Jint harness `querySelector`/`querySelectorAll` ignore the selector, so selector drift in sb-config.js goes undetected | medium | Pre-verified gap → patch (stub filters on tag/type). |
| B6/V2 | `MESSAGE_MAX = 2000` duplicates the DTO limit with no test linking them | low | Real; the fix is a small test → patch (assert the JS constant equals `LeadSubmissionRequest.Message`'s `StringLength`). |
| B11 | "Only one block renders" is enforced twice (host `FirstRenderable` skip plus the template's `HttpContext.Items` flag) | low | Every host already skips the non-first blocks, so the Items flag is dead → patch (delete it). |
| B1/E6 | 4.1 DrumConfig content is dropped with no migration | false | Phước confirmed 2026-09-28 that it is throwaway walkthrough data. Nothing is deployed yet (the launch is Epic 8). |
| B4 | A tiny budget yields "Cấu hình đã chọn: …" with no data | low | This needs the typed message to be about 1980 characters or more, and the fix adds a guard → rejected. |
| B5/E3 | Truncation can split a surrogate pair | low | This needs over-length plus an emoji at the cut point; it adds a guard → rejected. |
| B7 | Summary lookup is page-global, not tied to a form | low | The spec guarantees exactly one form per page and tests assert it; this is speculative → rejected. |
| B8 | Visitor is not told the selections are sent | false | Phước chose hidden prepend-at-submit (a frozen decision). |
| B9 | `;`/`:`/`,` in editor text makes the summary ambiguous; choices are not de-duplicated | low | This is editor-controlled content, and the fix adds escaping → rejected. |
| B12 | 4.1's rendered long-value and non-Trống tests were dropped; attribute encoding is untested | low | Long-value was a CSS check in 4.1 too and is still covered by CSS; Razor always encodes attributes → rejected. |
| B13 | Block and form repeat on archive page 2+ | low | This is the same call as 4.1 B2/E2: it matches the existing blocks → rejected. |
| B14 | Spec is missing from the diff | false | It was excluded from the review diff on purpose. |

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test --artifacts-path tests/TbTruongHoc.Web.Tests/obj/art` (repo root) -- expected: all pass.
- `dotnet build` -- expected: 0 new warnings.

**Manual checks:**
- In the Manager, add the block with one row of each type to "Trống chùa", a PDP and a landing page. At 375px and 1280px: controls are reachable by keyboard, taps hit ≥44px, there is no sideways scroll, and a submitted lead's Message shows the summary. Remove the block and the section disappears.
