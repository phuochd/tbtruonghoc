---
review-of: architecture-tbtruonghoc-2026-09-18/ARCHITECTURE-SPINE.md
reviewer: rubric-walker (good-spine checklist)
date: 2026-09-18
---

# Review: Architecture Spine — Ngọc Anh Multi-Site Rebuild Phase 1

**Verdict: PASS WITH FINDINGS — the AD set correctly fixes the real divergence points and covers all 14 FRs with no internal contradictions, but two High-severity gaps (the operational/deployment envelope is silent, and the .NET 8 stack pin is not verified against today's LTS lifecycle) should be closed before this spine is treated as final.**

---

## Findings

### High

**H-1. .NET 8 stack pin is stale relative to the spine's own "today" (2026-09-18), and the memlog shows no verification against the current LTS.**
- The Stack table pins `.NET 8 (LTS)`, and the memlog's justification is: "chosen for LTS stability over .NET 9 STS." That comparison is the *right* comparison as of the PRD/brief's original research context, but it omits .NET 10.
- .NET 8 was released November 2023 with a 3-year LTS window — support ends ~November 2026, i.e. **about two months after this document's own `created`/`updated` date**. .NET 9 (STS, released Nov 2024) already went EOL ~May 2026. **.NET 10 (LTS) shipped ~November 2025** and is the runtime that would actually carry a build starting now through its multi-year life, especially given Site B's Tết 2027 deadline and Site A's open-ended timeline.
- Nothing in the spine or memlog cites checking Piranha CMS 12.2.0 / Piranha.Data.EF.MySql 12.0.0 against .NET 10 compatibility, nor re-confirms .NET 8 is still the right pin given today's date. The memlog's version-verification bullet is dated "Sept 2026" and cites GitHub releases for Piranha CMS itself, but the .NET version reasoning reads as carried over from an earlier, now-stale comparison (8 vs. 9) rather than freshly checked against 10.
- **Why this matters at this altitude:** the Stack table is exactly the kind of fixed invariant meant to stop Site A and Site B builders from diverging on runtime — but if the pin itself is wrong, both builders converge correctly on a soon-to-be-unsupported target. This is a "verified-current" failure (checklist item 4), not a divergence-prevention failure, but it's consequential enough to flag before build starts.
- **Suggested fix:** re-run the version check explicitly against .NET 9 *and* .NET 10, confirm Piranha 12.2.0's supported target frameworks, and either re-justify .NET 8 (e.g., if Piranha 12.2.0 doesn't yet support .NET 10) or move the pin.

**H-2. The operational/environmental envelope is silent, not deferred — for a shared-single-database topology this is a real risk, not just an omission.**
- Checklist item 5 specifically flags deployment & environments topology, infra/provider strategy, and operations/monitoring as a dimension domain-focused drafts tend to skip. This spine partially covers infra (Structural Seed diagram: one CentOS 9 host, one App, one DB, one Media store) but is completely silent on:
  - **Backup/disaster-recovery** for the shared MySQL database. AD-1 explicitly consolidates both sites' data into "one shared database" — a single point of failure that didn't exist when the two businesses were unrelated — yet no backup cadence, retention, or restore plan is stated, or even named as an open question.
  - **Environments topology** (dev/staging vs. production) — no mention of whether builders test against a staging Piranha instance or edit content/schema directly against production.
  - **Monitoring/alerting/logging** — nothing on how a failure (app down, DB down, disk full on the local media store) would be observed.
  - **CI/CD or deployment process** — how code/migrations reach the CentOS host is unaddressed.
- This is meaningfully different from the one ops item the spine *does* address: "Deferred" explicitly and reasonably defers "exact reverse-proxy/TLS/process-supervisor setup" as "a standard, well-documented ops pattern... left to deployment setup." That's a deliberate, justified scope cut. Backup/DR, environments, and monitoring get no equivalent treatment — they're simply absent, which reads as an oversight rather than an intentional deferral.
- **Why this matters:** an AI coding agent or independent builder handed this spine has no signal that backup/staging is even a question to raise before going live, and a DB-level incident would now take down both site's data and lead-capture history (FormSubmission table, AD-3) at once.
- **Suggested fix:** add these to Deferred at minimum (so it's a flagged gap rather than silence), or better, fix a minimal invariant (e.g., "nightly MySQL dump to X, retained N days" / "staging site record under a third `Site` before promoting content").

### Medium

**M-1. FR-2 (persistent contact channels) has no Consistency Convention analogous to FR-4's, despite the identical divergence risk.**
- The Consistency Conventions table fixes FR-4 (GA4/Search Console) as "a field on each Piranha `Site`'s settings... never hardcoded per-view, so a tag is swappable without a deploy." FR-2 (click-to-call, Zalo, Maps, "using the correct contact details/location for that site's business line") is the same shape of problem — per-site business data that must not be hardcoded into a view/template — but the Capability Map only says it "Lives in: Shared layout, per-site settings" without a Rule or Convention row enforcing that. A builder could legitimately hardcode Site A's phone number directly into a shared partial view and nothing in the spine catches it.
- **Suggested fix:** either fold FR-2's contact fields into the same per-`Site`-settings convention explicitly, or add a dedicated row.

**M-2. The "per-`Site` settings" content type that both FR-4 and (per M-1) FR-2 depend on is never itself named as a single shared invariant.**
- Nothing states there is exactly one custom Site-content-type/settings schema (holding GA4 ID, Search Console token, phone, Zalo link, address, maps embed) shared by both sites — it's implied by the FR-4 convention wording but not fixed as its own AD-level decision the way Product Post Type, FormSubmission, and Alias are. Low risk of a builder inventing a second one, but it is the same class of "shared shape" invariant the rest of the spine is careful to name explicitly.

**M-3. MySQL server version is unpinned.**
- Every other Stack row pins an exact version (.NET 8, Piranha CMS 12.2.0, Piranha.Templates 12.0.0, Piranha.Data.EF.MySql 12.0.0) — only "Database: MySQL, one shared instance/database" has no version. Given Piranha.Data.EF.MySql 12.0.0 rides on Pomelo.EntityFrameworkCore.MySql, whose supported MySQL-server range is a real compatibility constraint, this is worth pinning too (or explicitly noting MySQL version is intentionally left to deployment, the way reverse-proxy/TLS was).

### Low

**L-1. No shared testing-strategy or CI baseline is named.**
- Plausibly out of scope at "initiative" altitude (left to epic/story level), but two independent builders with zero shared testing convention is exactly the kind of thing that can diverge quietly. Flagging as low rather than medium since the PRD/brief never asked for one and it doesn't block either site's build.

**L-2. Piranha.Templates 12.0.0's version pin has no dedicated verification citation.**
- The memlog cites GitHub releases specifically for Piranha CMS 12.2.0, and source-code citations for Alias and Comment models, but Piranha.Templates 12.0.0 is asserted in the same breath without its own citation. Low risk (it's a scaffold tool, not a runtime dependency), but inconsistent with the rigor shown elsewhere.

---

## Checklist walk-through (summary)

1. **Divergence points fixed / missed:** Core divergence points (instance topology, content shape, lead-capture storage, redirects, pricing-field placement) are all correctly identified and fixed. Missed: the FR-2 contact-info storage pattern (M-1) and the operational envelope (H-2).
2. **AD Rules enforceable and Prevents-accurate:** All five ADs have concrete, checkable Rules (content-type shape, table vs. built-in model, Alias vs. custom middleware, Site record vs. deployment) that genuinely block the stated divergence. No weak/unenforceable Rules found.
3. **Wrongly deferred items:** None found — every Deferred entry is either genuinely low-risk (per-site user restriction, given solo admin) or explicitly out of PRD scope (CRM, Site C). The gap is not a wrongly-deferred item but an *unmentioned* dimension (see H-2), which is a different failure mode than #3 is asking about.
4. **Verified-current tech claims:** Piranha CMS 12.2.0, the Alias model (`RedirectUrl` unconstrained string), the Comment/PostComment fixed schema, and Piranha's single-tenant multi-site behavior are all backed by specific citations (GitHub releases, source file paths, docs URLs, a GitHub issue number) in the memlog — good practice. The .NET 8 pin is the exception: reasoned from a since-superseded comparison (8 vs. 9) with no evidence it was re-checked against .NET 10, which is the actually-current LTS as of this document's own date (H-1). MySQL server version is simply unpinned (M-3).
5. **Silent structural dimensions:** The operational/environmental envelope (backup/DR, staging vs. prod, monitoring, CI/CD) is left completely silent — not decided, not deferred, not flagged as an open question (H-2). Reverse-proxy/TLS/process-supervisor, by contrast, *is* explicitly and reasonably deferred, which makes the silence on backup/monitoring look like an oversight rather than a considered cut.
6. **Capability → Architecture Map vs. FR-1…FR-14:** Full coverage confirmed — every FR from FR-1 through FR-14 has a row, plus an extra row for the addendum's legacy-redirect concern. FR-11 is correctly mapped to "Deferred — exact shape" rather than silently dropped.
7. **Internal consistency:** No contradictions found between the ADs' Prevents/Rules and the Consistency Conventions table. AD-2 and AD-5 agree on landing pages being standalone Pages with their own price region; AD-1's link-scheme rationale is consistent with FR-5's convention row; the Media-storage convention's "no stated redundancy requirement" is consistent with (if quietly exposed by) the AD-1 single-shared-database choice, which is where H-2 originates.
