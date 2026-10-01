---
title: 'Story 6.5: Site A installation-required PDP and free-survey form'
type: 'feature'
created: '2026-10-01'
status: 'done'
baseline_commit: 'a453140c6376ec7f46d3961ba223c7741795bea4'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-tbtruonghoc-2026-09-18/mockups/key-chi-tiet-can-thi-cong.html'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-tbtruonghoc-2026-09-18/mockups/key-form-khao-sat.html'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A Site A product ticked "Cần thi công (Site A)" only loses its shipping note (6.4). The visitor sees no service area and no install process, and the only form is the general quote form. That form cannot record an installation address, so a technician cannot schedule a survey from it (FR-8).

**Approach:** On an installation-required product, `SiteAProductPost` swaps the shippable pieces for the installation variant. That variant has a primary CTA "Đăng ký khảo sát miễn phí" that opens a survey `<dialog>` through 6.4's generic modal opener, plus a `service-area` block and a `process-strip`. The survey form posts to `/api/leads` with `formType=survey` and a location. The server stores `location_address` and decides `is_outside_service_area` itself. A live, non-blocking amber warning appears in the form when the location reads as outside Miền Bắc – Thanh Hóa.

## Boundaries & Constraints

**Always:**
- Installation variant info column: H1 → Mã → excerpt → `price-block` → CTA row → trust chips. The CTA row is the amber "Đăng ký khảo sát miễn phí" (`data-sa-modal-open="sa-survey-modal"`), then call/Zalo exactly as in 6.4. It has no "Yêu cầu báo giá" and no quote dialog.
- `price-block` keeps the same block, label and 6.4 states. On an installation product only, the blank-price note reads "Giá phụ thuộc điều kiện thi công thực tế — báo giá chính xác sau khi khảo sát hiện trường." The price-present note is unchanged.
- After the specs table and before the editor blocks (focus order nav → breadcrumb → gallery/info → specs → service-area → process-strip):
  - **`service-area`:** a bordered panel with heading-xs "Khu vực phục vụ: Miền Bắc – Thanh Hóa" in primary. Text: "Đội thi công của Ngọc Anh trực tiếp khảo sát và lắp đặt trong phạm vi từ Miền Bắc đến Thanh Hóa." It ends with a nested warning-bg note: "Ngoài khu vực này, quý khách vẫn có thể gửi đăng ký khảo sát — đội kinh doanh sẽ liên hệ tư vấn phương án phù hợp."
  - **`process-strip`:** "Quy trình thi công", an `<ol>` of 3 steps: Khảo sát / Hợp đồng / Thi công. Step copy comes from the mockup. Numbered primary circles, with arrows between steps that are hidden from assistive tech. On mobile the steps stack.
- Both blocks use fixed copy (it is the company's service promise, not per-product content). They never appear on a shippable product.
- **Survey dialog:**
  - It reuses 6.4's `sa-modal` structure and opener: title "Đăng ký khảo sát miễn phí", sub-line "Để lại thông tin, kỹ thuật viên Ngọc Anh sẽ liên hệ hẹn lịch khảo sát tận nơi." and a "Đóng" close button whose label names this dialog.
  - Fields, in order:
    - sản phẩm quan tâm: `readonly`, surface-tint, with the hint "Tự động liên kết từ trang sản phẩm đang xem"
    - "Địa điểm / địa chỉ lắp đặt"
    - "Họ tên"
    - "Số điện thoại"
  - The last three are required. Each has a visible label, an error-colored `*` and `aria-required="true"`.
  - Below the fields: a full-width amber submit "Gửi đăng ký" and the note "Miễn phí khảo sát · Không phát sinh chi phí trước hợp đồng".
  - It is a new partial (`_SurveyRequestForm`) with ids distinct from `_QuoteRequestForm`'s. It still carries `data-quote-request-form`, so lead-form.js submits it and handles success/error/kept values.
- **Server:**
  - `LeadSubmissionRequest` gains `LocationAddress` (≤500).
  - For `formType=survey`, a blank location → 400 with a field error shown under that field. The row stores the trimmed `LocationAddress`, and `IsOutsideServiceArea` is computed **server-side** (true/false) from that text. Any client-sent flag is ignored.
  - Other form types keep both fields null and behave exactly as today.
- **Warning:**
  - It runs live as the visitor types in the location field. When the text reads as out-of-area, an amber `warning-banner` appears between địa điểm and họ tên: "!" icon, bold headline "Ngoài khu vực phục vụ trực tiếp", and the mockup's body text. It is linked to the field via `aria-describedby` and announced politely. The field's border/background tint to warning colors.
  - Submit stays enabled, and an out-of-area submission succeeds.
- Out-of-area detection uses one place-name list, used by both the server and the page (the view hands it to site-a.js; no second copy in JS). The match ignores case and diacritics.
- **Decision (Q1, option A):** the location stays a single free-text field, as in the mockup.
  - The list holds province/city names **outside** the area, both pre- and post-2025-merger names, plus common short forms ("TP HCM", "HCM", "Sài Gòn").
  - Matching is word-bounded. A recognisable out-of-area name → warn + outside=true. No recognisable name → no warning, outside=false.
  - A rare false warning (e.g. a street named after a southern province) is accepted, because the warning never blocks.

**Never:**
- Change Site B output, `_QuoteRequestForm`, or the general/landing lead behavior. All existing tests pass unchanged.
- Block, disable, or reject a survey because of its location, or color-only warnings.
- Add a DB migration (the columns already exist).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Installation PDP | "Cần thi công" ticked, blank price | Survey CTA + call/Zalo, installation price note, service-area, process-strip; no shipping-note, no quote dialog | N/A |
| Installation + price | price "12.000.000đ" | Price-present state; rest as above | Encoded |
| Shippable PDP | flag off | Exactly 6.4's output; no survey dialog/blocks | N/A |
| In-area survey | "Trường MN Hoa Sen, TP. Thanh Hóa" | No banner; row formType survey, location stored, outside=false | N/A |
| Out-of-area survey | "Quận 1, TP. Hồ Chí Minh" / "Da Nang" | Banner + amber field live; submit works; row outside=true | N/A |
| Missing location | survey, location blank | 400 → error under địa điểm, values kept | No row |
| General lead w/ location | formType general + locationAddress | Stored with both fields null | N/A |
| Unknown formType | "foo" | Coerced to general (unchanged) | N/A |

</frozen-after-approval>

## Code Map

- `Controllers/LeadsController.cs` `Create`: the `formType` coercion is at l.89. Add the survey branch: require the location (`ModelState.AddModelError(nameof(LocationAddress))` → `ValidationProblem()`), set `LocationAddress` and `IsOutsideServiceArea = ServiceArea.IsOutside(...)`.
- `Models/LeadSubmissionRequest.cs`: add `LocationAddress` with `[StringLength(500)]` (the `LeadDbContext` limit).
- New `Models/ServiceArea.cs`: a static list of place names + `IsOutside(string)`. Diacritic fold mirrors site-a.js `fold()` (l.275: NFD, strip marks, đ→d, collapse spaces).
- `Models/FormSubmission.cs`, `LeadDetailModel`, Leads Manager (`formatOutsideServiceArea` → Có/Không/—) and `LeadEmailComposer` (l.46–53) already render both fields. No change.
- `Views/Cms/SiteAProductPost.cshtml`:
  - `shippable` already branches the shipping-note.
  - Branch the CTA row, the price note and the dialog. Add service-area/process-strip after `.sa-pdp__specs`.
  - Emit the place-name list as a JSON `data-*` attribute on the survey form (`System.Text.Json`, HTML-encoded by Razor).
- `Views/Shared/_QuoteRequestForm.cshtml`: the markup/error-slot pattern to copy. Don't modify.
- New `Views/Shared/_SurveyRequestForm.cshtml`: model `QuoteRequestFormViewModel` (product + formType) plus the list (ViewData or a new small view model).
- `wwwroot/assets/js/lead-form.js`:
  - Payload: add `locationAddress` (empty when the field is absent, so the general form is unchanged).
  - `FIELD_MAP`: add `LocationAddress: 'locationAddress'`.
  - The `input, textarea` aria-invalid reset already covers new inputs.
- `wwwroot/assets/js/site-a.js`: modal opener is l.38 (generic, by id). Add `initSurveyWarning` on `[data-sa-survey-areas]`: the `input` event toggles banner `hidden` and a field class, and reuses `fold()`.
- `wwwroot/assets/css/site-a.css`: tokens at the top, quote form l.1534, `sa-modal` l.1842. Add `sa-service-area`, `sa-process-strip`, `sa-survey-form` (readonly linked field, req asterisk, field-gap 14px), `sa-warning-banner`, and the field warn state.
- Tests:
  - `tests/TbTruongHoc.Web.Tests/SiteAProductPageTests.cs`: builders + `GetHtmlAsync`; shared DB → scope to own ids / `<main>`.
  - `LeadSubmissionTests.cs`: POST patterns. This is where the deferred "non-default formType saved verbatim" test lands.
  - `SiteAPdpScriptTests.cs`: the Jint pattern for site-a.js.

## Tasks & Acceptance

**Execution:**
- [x] `Models/ServiceArea.cs`, `Models/LeadSubmissionRequest.cs`, `Controllers/LeadsController.cs` -- survey location validation + server-computed flag.
- [x] `Views/Shared/_SurveyRequestForm.cshtml` -- survey fields, warning banner (hidden by default), areas data attribute.
- [x] `Views/Cms/SiteAProductPost.cshtml` -- installation variant: CTA, price note, service-area, process-strip, survey dialog; shippable output unchanged.
- [x] `wwwroot/assets/js/lead-form.js` -- send `locationAddress`, map its field errors.
- [x] `wwwroot/assets/js/site-a.js` -- live out-of-area warning.
- [x] `wwwroot/assets/css/site-a.css` -- new blocks + survey form + warning styles under Site A tokens; mobile stacking.
- [x] Tests -- `ServiceArea` matching cases, lead POSTs (survey in/out/missing location, general ignores location, survey saved verbatim), PDP render rows of the matrix, Jint warning toggle (shows, hides, submit never disabled).

**Acceptance Criteria:**
- Given an installation-required product page, when a visitor taps "Đăng ký khảo sát miễn phí", then the survey dialog opens in-page with the product prefilled read-only, and focus returns to the CTA on close.
- Given a submitted survey, when it is viewed in the Leads Manager, then its form type (`survey`), address and "Ngoài khu vực" Có/Không are shown (the Manager shows raw form-type values; existing behavior).
- Given the full suite, when run, then all pre-existing tests pass.

## Implementation Notes

- Implemented directly in the orchestrating session (no subagent was authorised).
- `ServiceArea` collisions found while building the list and left out of it:
  - "Vinh" matches every "Vĩnh …" (Vĩnh Phúc).
  - "Đông Hà": "Hà Đông, Hà Nội" folds to "ha dong ha noi".
  - "Miền Trung"/"Trung Bộ": Thanh Hóa itself is Bắc Trung Bộ.
  - Tests pin in-area negatives (Hà Đông, Vĩnh Phúc, Long Biên, Quảng Ninh).
- The server coerces `formType` before validating, so " survey " (unrecognised) stays general and never demands a location. Stored location is trimmed; `IsOutsideServiceArea` is `ServiceArea.IsOutside` (false when nothing matches). A client-sent flag is ignored.
- `aria-describedby`:
  - The survey fields list their error slot first. lead-form.js now takes the first id, which is unchanged for single-id general forms.
  - The warning id is added to the location field only while the banner shows. A hidden element referenced by `aria-describedby` is still read, so it is never permanently listed.
- 6.4's `Installation_Required_Product_Never_Shows_Shipping` asserted "Yêu cầu báo giá" on an installation product; that was 6.4's interim behaviour, which the frozen 6.5 Boundaries replace. The single assertion now expects the survey CTA. No other existing test changed.
- AC 2 wording corrected: the Leads Manager shows the raw form type (`survey`), not "Khảo sát". Only the email composer maps it. Left as is (out of scope).
- Tests:
  - `SiteASurveyScriptTests`: Jint on the real site-a.js, fed the real `OutsideTerms`, plus C#/JS fold parity.
  - `LeadFormSurveyScriptTests`: Jint on lead-form.js, covering the 400 → error under địa điểm with values kept.
  - New lead POST rows in `LeadSubmissionTests` and 2 render tests in `SiteAProductPageTests`.
  - Suite 473/473 + 2.
- Review patches (pass 1): `SurveyLocationAttribute` replaces the in-action check and `[StringLength]`. The list drops Phú Yên/Tân An and adds run-together spellings. Suite 485/485.

## Spec Change Log

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V):

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| B1/E1 | Location error only arrives on a second submit | medium | `[ApiController]` auto-400s on Name/Phone before the action runs, and object-level validation only runs once properties pass → patch: property-level `SurveyLocationAttribute`, test posts all-blank and expects all 3 errors. |
| E5/V-other | `[StringLength(500)]` 400s a general/landing lead for a field it ignores | low | Real for hand-built calls; folded into the same attribute (survey-only) → patch. |
| V1 / B8 | 500-char limit untested | low | Pre-verified gap → patch: 501-char survey 400 + nothing saved; general passes and stores null; `maxlength="500"` asserted on render. |
| E3/B4 | Fold collisions: Phù Yên (Sơn La), Tân An, đường Đà Nẵng (HP), Bình Định/Long Xuyên/Đồng Tháp communes | low | Real. Frozen Q1 accepts false warnings, but the district- and ward-level ones are common → patch: drop "Phú Yên"/"Tân An" (deletion, as with Vinh), pin with tests, document the accepted rarer ones. |
| B5 | Run-together spellings (Saigon, Danang, HCMC…) never warn | low | Real; data-only addition → patch + tests. |
| B6 | `OutsidePlaces` doc comment is two drafts glued together | low | Direct fix → patch. |
| B7 | Clear-warning test never asserts the warning appeared | low | Direct fix → patch. |
| B10 | Fold parity test lacks horn/breve/dot-below cases | low | Both sides strip U+0300–036F (all Vietnamese marks); test-only addition → patch. |
| E2 | No client-side required check | low | Same server-only pattern as the general form (Story 1.4); after B1 one round-trip shows every error → rejected. |
| E4 | No `String.prototype.normalize` → warning never shows | low | ES2015, in every supported browser; the server flag is unaffected → rejected. |
| B2 | Survey success text is the generic quote one | low | Frozen spec leaves success to lead-form.js; a per-form message adds a parameter → rejected, flagged for walkthrough. |
| B3 | Banner announced twice / re-announced while typing | low | The live region fires only when hidden toggles, which needs the match state to change; `aria-describedby` isn't re-read during typing → rejected. |
| B9 | "Miền Bắc – Thanh Hóa" hard-coded in several places | low | Frozen: fixed copy; a shared constant adds surface for a promise that won't change → rejected. |
| B11 | CSS literals / 11px text | false | 11px is DESIGN's `caption` token; white numerals match DESIGN's "primary/white" circles (`--sa-on-primary` is a tint, not white). |

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test` (repo root) -- expected: all tests pass.
- `dotnet build` -- expected: no new warnings.

**Manual checks:**
- At 375/1280px, an installation product shows service-area + process-strip. The survey sheet/modal warns live on "TP. Hồ Chí Minh" and still submits.
