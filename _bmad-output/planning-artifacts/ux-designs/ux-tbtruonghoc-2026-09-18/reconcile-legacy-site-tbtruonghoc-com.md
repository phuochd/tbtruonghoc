---
input: imports/legacy-site-tbtruonghoc-com.md
type: user-supplied visual reference (legacy production site)
reconciled: 2026-09-18
---

# Reconciliation — legacy site (tbtruonghoc.com)

What happened to each observation from the legacy-site import, against the finalized spines.

| Observation from import | Outcome |
|---|---|
| Thin nav (6 items) vs. PRD's 11 categories | **Used** — drove the explicit IA decision for a "Sản phẩm" dropdown (all 11) + aggregate "Danh mục sản phẩm" page, since the legacy nav structure could not carry 11 categories as-is. See EXPERIENCE.md Information Architecture. |
| Red "Giảm giá %" discount-badge, retail-promo visual language | **Used as anti-pattern** — named explicitly in DESIGN.md Brand & Style and Do's and Don'ts as a rejected register; directly shaped the "no red, no discount badges" constraint carried through all three color-theme rounds. |
| White background, generic sans hierarchy, dated marketplace look | **Used as anti-pattern (partial)** — the "generic, undesigned" quality motivated the push for a distinctive teal+amber system; the white-background instinct itself was not rejected (final palette's body background `#FCFEFD` is also near-white), only the lack of a deliberate brand system around it. |
| Product photography in context (classroom, outdoor settings), professional but stock-catalogue feel | **Used directly** — the same legacy photo assets (dù che sân trường, mầm non, etc.) were reused as real content in the hero-carousel and product-detail-page mocks, since no new photography exists yet. Not treated as an anti-pattern; the photography itself was fine, only the surrounding chrome (discount badges, thin nav) was rejected. |
| Footer contact details (phone numbers, "hỗ trợ 24/7") | **Not directly used** — Site A's footer-strip pattern is respecified in DESIGN.md Components with new tokens; the specific legacy phone numbers/copy were not re-verified or carried into the spine (this is content, to be confirmed at CMS entry, not a UX structural decision). No idea was dropped here — it was out of scope for this UX session. |

No qualitative ideas from this import were dropped without a stated reason — every row above traces to a spine decision or is explicitly marked out of scope (content, not structure).
