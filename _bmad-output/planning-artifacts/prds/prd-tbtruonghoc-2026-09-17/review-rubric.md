# PRD Quality Review — prd-tbtruonghoc-2026-09-17

## Overall verdict
This PRD holds up well: it has an earned thesis (the site split), honest trade-off documentation (cart declined with a named counter-metric, cross-domain redirect equity loss acknowledged, link-scheme risk called out with a specific NOTE FOR PM), and a clean Assumptions/Open-Questions/Glossary mechanical spine. The main risks are not strategic but executional — a couple of FR consequences (FR-3, FR-8) lean on soft language instead of bounds, the performance counter-metric (SM-C2) has no baseline to regress from, and two stale section cross-references (pointing to "§5 Non-Goals" when Non-Goals is §6) will cost a downstream reader a few seconds of confusion. None of this blocks use; it's a PRD in good working order that is honest about not yet being ready for Architecture/Sprint Planning (§7.1, Open Question 6).

## Decision-readiness — strong
Trade-offs are named with what was given up, not just what was chosen. §6 Non-Goals doesn't just list "no cart" — it says the cart was "Evaluated and explicitly declined in favor of the phone/Zalo-finalized flow (SM-C1)," giving the reader the reasoning and the metric that would surface if the decision were wrong. The FR-5 `[NOTE FOR PM]` sits at a genuine tension (commonly-owned domains being link-scheme risk under Google's guidelines) rather than a safe checkpoint, and it's backed by real detail in `addendum.md`'s "Cross-site backlink risk" section rather than asserted and dropped. The nine Open Questions are substantively open — e.g., OQ2 ("hard-block out-of-area submissions or just warn") and OQ6 (cross-site resourcing/sequencing) have no answer smuggled into the next sentence. OQ8 (whether to formally update the stale Product Brief) is a real governance decision surfaced honestly rather than resolved unilaterally.

No findings — this dimension does what it should.

## Substance over theater — strong
Four UJ protagonists (Cô Lan, Bác Thành, Chị Hương, plus the unnamed owner JTBD), each driving a specific FR (UJ-1→FR-8, UJ-3→FR-10/FR-11, UJ-4→FR-12/FR-13) rather than sitting decoratively. The Vision (§1) is specific to this business — the father-in-law/guild-chairman craftsman detail, the Tết 2027 deadline, the "ranks for almost nothing" framing — and could not be swapped into another PRD unchanged. No boilerplate NFR language ("scalable," "secure," "user-friendly," "seamless") appears anywhere in the document; a scan for these terms returned nothing. The addendum's legacy-site audit (actual URLs, SKUs, breadcrumb paths, and the "thin SEO but intent-matching content ranks anyway" hypothesis) is evidence-based product thinking, not template filler.

No findings.

## Strategic coherence — strong
The thesis is explicit and load-bearing: "two products, not one" (§0), and the entire document structure (§5.1 shared foundations / §5.2 Site A / §5.3 Site B) follows from it rather than being an arbitrary grouping convenience. Feature sequencing follows the thesis, not ease: the Tết 2027 deadline drives FR-12/FR-13 priority, and FR-11 (drum configurator) is explicitly demoted to should-have because it competes with that deadline for the same build capacity (§7.2, OQ4). Success Metrics validate the thesis rather than measuring generic activity — SM-2 specifically measures whether categories *other than* the one already-working niche start generating search-attributed leads, which tests SEO diversification, not vanity traffic. Counter-metrics (SM-C1 phone/Zalo volume, SM-C2 Core Web Vitals) are present and tied to specific SMs.

No findings.

## Done-ness clarity — adequate
Most FRs carry genuinely testable, often quantified consequences: FR-2 ("100% of published page templates"), FR-6 ("ranks for at least one assigned target keyword within the SM-1 measurement window"), FR-13 (named variants with per-item pricing), FR-12 (a hard date). This is above average for the dimension.

### Findings
- **medium** SM-C2 counter-metric has no baseline to regress from (§8) — "Page load speed/Core Web Vitals must not regress on either site as SEO content volume grows" is a relative bound with no anchor value. The legacy site is live today, so a pre-rebuild CWV baseline is capturable before this becomes unmeasurable. *Fix:* add an acceptance step to capture legacy-site CWV/PageSpeed scores before Site A/B launch, and state the specific metrics (LCP, CLS, INP) the counter-metric tracks against that baseline.
- **low** FR-3's consequence is under-specified relative to its sibling FRs (§5.1) — "Form submissions are captured and routed (destination mechanism is an architecture-level decision)" doesn't state what "captured" means as a testable condition (stored where, confirmed how) even though routing destination is legitimately deferred. *Fix:* add one consequence line establishing capture itself is verifiable (e.g., "every submission is retrievable with all submitted fields intact"), independent of the routing-destination decision.
- **low** FR-8's data-sufficiency consequence uses soft language — "Captures enough data... for a technician to act without a basics-gathering follow-up call" is somewhat self-defining since it lists the fields (location, product, contact info) but "enough" is still doing interpretive work. *Fix:* either enumerate the required fields as the acceptance criterion directly, or state the follow-up-call elimination as the explicit test.

## Scope honesty — strong
§6 Non-Goals is doing real work, not a token section — seven items, each with a reason (cart declined "in favor of ... SM-C1," Site C "confirmed deferred," no link exchange "see the link-scheme risk note in addendum.md"). All five inline `[ASSUMPTION]` tags (FR-6, FR-9, FR-10, FR-14, SM-1) round-trip cleanly to the Assumptions Index (§10) with no orphans either direction. Open-item density is high for the document's size — 9 Open Questions + 5 Assumptions + 3 `[NOTE FOR PM]` callouts — but that density is earned and self-acknowledged: §7.1 states outright that sequencing "needs to be resolved before Architecture/Sprint Planning can commit to a timeline," so the PRD isn't presenting itself as green-light-ready while carrying this many unresolved items. That's the honest version of a high-density PRD, not the red-flag version.

No findings.

## Downstream usability — adequate
Glossary (§4) is substantive and its nine terms are used consistently in case and meaning across the document (Site A/B/C, Piranha CMS, Sản phẩm cần thi công, etc.). FR/UJ/SM IDs are contiguous and unique (FR-1 through FR-14, UJ-1 through UJ-4, SM-1/2/3 plus SM-C1/C2), and most cross-references resolve correctly (e.g., FR-5's pointer to `addendum.md` matches real content there).

### Findings
- **medium** Two stale section cross-references point to the wrong section number — line 89 (UJ-4 edge case: "No on-page cart recovery mechanism exists in v1 (see §5)") and line 216 (FR-13: "no cart or online payment attached (§5 Non-Goals, §6)") both label §5 as "Non-Goals," but Non-Goals is §6 (§5 is "Features"). This reads like a leftover from an earlier section-numbering pass. *Fix:* change both to point to §6 only (e.g., "(see §6 Non-Goals)").
- **low** UJ-2 (§3.3) is not explicitly "realized by" any FR the way UJ-1 (→FR-8), UJ-3 (→FR-10/FR-11), and UJ-4 (→FR-12/FR-13) are — it borrows Cô Lan's persona type by reference ("Same buyer type as UJ-1") rather than naming its own protagonist or an owning FR, leaving it the one journey without a traceable FR anchor. *Fix:* either give UJ-2 its own named protagonist and tag which FR (likely FR-6) realizes it, or fold it into UJ-1 as a documented variant rather than a separate numbered UJ.

## Shape fit — strong
This is a consumer/institutional-buyer product with meaningful UX (per-page SEO, phone/Zalo-first funnels, paid-ads landing pages), and UJs with named protagonists are correctly load-bearing here — four UJs, not over- or under-formalized. The document is chain-top (feeds Google Stitch UX prompts, then Architecture, then Sprint Planning — explicit in OQ6 and OQ8), which is why the Glossary/ID/Assumptions-roundtrip rigor shown above matters and is appropriately present. It's also effectively brownfield: the addendum's legacy-site audit (real live URLs, SKU codes, breadcrumb structure, current nav) is concrete and specific rather than hand-waved, and the redirect strategy correctly distinguishes same-domain vs. cross-domain moves. The mid-discovery pivot from single-site to three-site architecture is handled by grouping FRs by site cluster (§5.1/5.2/5.3) instead of forcing a single flat FR list — a structural choice that fits the actual shape of what happened during discovery, explained plainly in §0.

No findings.

## Mechanical notes
- **Cross-reference errors:** see Downstream usability findings above (lines 89 and 216 mislabel §5 as "Non-Goals"; should be §6).
- **ID continuity:** clean. FR-1…FR-14 contiguous, no gaps or duplicates. UJ-1…UJ-4 contiguous. SM-1…SM-3 plus SM-C1/SM-C2, no gaps.
- **Assumptions Index roundtrip:** clean. All five inline `[ASSUMPTION: …]` tags (FR-6, FR-9, FR-10, FR-14, SM-1) are indexed in §10, and every §10 entry has a matching inline tag — no orphans in either direction.
- **UJ protagonist naming:** three of four UJs (UJ-1 Cô Lan, UJ-3 Bác Thành, UJ-4 Chị Hương) carry a named protagonist with inline context. UJ-2 does not name its own protagonist (see Downstream usability finding).
- **Glossary drift:** minimal. The one soft spot is "Nghệ nhân (master craftsman)" (§4) — defined but the body consistently uses the English "craftsman"/"craftsman story" instead of the Vietnamese term elsewhere (FR-10, UJ-3). Given §0's stated policy of keeping only product/category names and SEO keywords in Vietnamese, this is consistent with that policy, not drift — noted only for completeness, not as a finding.
- **Required sections:** all present for a chain-top PRD at this stage — Document Purpose, Vision, Site Architecture, Target User (JTBD/Non-Users/UJs), Glossary, Features, Non-Goals, MVP Scope, Success Metrics, Open Questions, Assumptions Index.
