---
name: 'Web-Verification Review — Architecture Spine (Ngoc Anh Multi-Site Rebuild Phase 1)'
type: review
reviewed: _bmad-output/planning-artifacts/architecture/architecture-tbtruonghoc-2026-09-18/ARCHITECTURE-SPINE.md
reviewed_memlog: _bmad-output/planning-artifacts/architecture/architecture-tbtruonghoc-2026-09-18/.memlog.md
reviewer: Claude (subagent, web re-check pass)
date: 2026-09-18
---

# Web-Verification Review

**Overall verdict:** All 8 committed technical claims independently re-checked against live sources (NuGet, GitHub source, official Piranha docs, Microsoft/CentOS lifecycle pages) come back CONFIRMED as literally stated, with one important flag — claim 8's "LTS stability" reasoning for picking .NET 8 is technically true today but is about to expire (.NET 8 and .NET 9 both reach End of Support on the same date, November 10, 2026 — under two months after this spine's creation date — and .NET 10, the actual current LTS, was never considered), so this should be revisited before the build starts rather than treated as settled.

---

## Claim-by-claim findings

### 1. Piranha CMS latest stable is 12.2.0, released ~July 16, 2026, cross-compiled for .NET 8 and .NET 9

**CONFIRMED.**
- NuGet package page confirms version **12.2.0**, published **7/16/2026**, explicitly targeting **net8.0** and **net9.0** (net10.0 shows only as "computed"/inferred compatibility, not an explicit, tested target). — https://www.nuget.org/packages/Piranha/12.2.0
- Independently corroborated by libraries.io's version history (12.0.0 → Mar 22, 2025; 12.1.0 → Jul 15, 2026; 12.2.0 → Jul 16, 2026 — chronologically consistent). — https://libraries.io/nuget/Piranha
- Cross-compilation for .NET 8 and .NET 9 originates in the 12.0 release notes ("Cross-compile for both .NET 8 and .NET 9 [#2116]"), carried forward into 12.2. — https://github.com/PiranhaCMS/piranha.core/releases

**Note on source noise:** a raw scrape of the GitHub releases HTML page (via a fetch-summarizer) returned an inconsistent, clearly wrong chronology (12.0 dated "March 22, 2024" with 11.1 dated *after* it, "May 23, 2024"; 12.2 dated "July 16, 2025"). This is very likely a small-model misread of relative/ambiguous date strings on that page, not a real discrepancy — the two independent package-registry sources (NuGet's own metadata and libraries.io) agree with each other and are internally chronologically consistent, so they were treated as authoritative over the scraped HTML summary. Flagging this only so the date isn't second-guessed later without re-checking — the spine's July 16, 2026 date is correct.

### 2. Piranha.Templates 12.0.0 provides `piranha.mvc` dotnet-new template with `-d MySql`

**CONFIRMED.**
- Piranha.Templates 12.0.0 exists on NuGet. — https://www.nuget.org/packages/Piranha.Templates/12.0.0
- Official docs for the `piranha.mvc` template list the `-d|--database` option with exactly four values: `SQLite` (default), `SQLServer`, `MySql`, `PostgreSql`. — https://piranhacms.org/docs/master/basics/project-templates

### 3. Piranha.Data.EF.MySql 12.0.0 is Piranha's officially integrated MySQL EF Core provider, built on Pomelo

**CONFIRMED.**
- Piranha.Data.EF.MySql 12.0.0 on NuGet declares dependencies on Piranha ≥12.0.0, Piranha.Data.EF ≥12.0.0, and Pomelo.EntityFrameworkCore.MySql (≥8.0.3, with a 9.0 preview alternative). — https://www.nuget.org/packages/Piranha.Data.EF.MySql/

### 4. Native Alias feature: IApi.Aliases / AliasRouter, 301/302, RedirectUrl unconstrained across domains

**CONFIRMED**, with one minor nuance not called out in the spine.
- `IApi` exposes `IAliasService Aliases { get; }` directly (confirmed reading IApi.cs on GitHub). — https://github.com/PiranhaCMS/piranha.core/blob/master/core/Piranha/IApi.cs
- `Alias.RedirectUrl` is `[Required][StringLength(256)] string RedirectUrl` — no domain/path/format validation, so it does accept absolute external URLs, matching the "unconstrained... can point to a different domain" claim. — https://github.com/PiranhaCMS/piranha.core/blob/master/core/Piranha/Models/Alias.cs
- `RedirectType` enum has exactly `Permanent` and `Temporary` members, mapping to 301/302 respectively (standard convention; not explicitly commented in source but consistent with implementation elsewhere in the codebase).
- **Nuance not mentioned in the spine/memlog:** `RedirectUrl` has a **256-character length cap** (`[StringLength(256)]`). "Unconstrained" is accurate re: domain/format, but not re: length. Unlikely to matter for this project's redirect URLs, but worth a one-line note if a very long legacy URL ever needs redirecting.

### 5. Built-in Comment/PostComment model — fields, Post-scoped, not on IApi, public-once-approved

**CONFIRMED.**
- `Comment` base class fields: `Id`, `ContentId`, `UserId`, `Author` (required, max 128), `Email` (required, validated), `Url` (max 256), `IpAddress`, `UserAgent`, `IsApproved` (bool, **defaults to true**), `Body` (required), `Created` (required) — matches the claimed field list. — core/Piranha/Models/Comment.cs (raw GitHub)
- Confirmed **not** exposed on `IApi` (no `Comments`/`PostComments` property in IApi.cs).
- Confirmed **Post-scoped**: comment methods live on `IPostService` — `GetAllCommentsAsync(Guid? postId, ...)`, `GetAllPendingCommentsAsync(Guid? postId, ...)`, `SaveCommentAsync(Guid postId, Comment model)`, `SaveCommentAndVerifyAsync`, `DeleteCommentAsync` — all keyed by `postId`, no equivalent on pages.
- "Publicly displayed once approved" is a reasonable inference from `IsApproved` defaulting to `true` and this being a standard blog-comment feature (consistent with Piranha's example templates), rather than a sentence lifted verbatim from docs — flagging only so it isn't mistaken for a literal doc quote, not because it's wrong.

### 6. Manager UI extensibility (custom modules/menu items/Vue components) is a documented, supported pattern

**CONFIRMED.**
- Official docs describe exactly this: modules via `IModule`, menu items added with the `MenuItem` class (InternalId/Name/Route/Css/Policy) typically registered in `Startup.cs` after `Piranha.App.Init()`, and custom Vue.js components registered as global components via custom "Resources" (injected CSS/JS). — https://piranhacms.org/docs/master/manager-extensions/menu, https://piranhacms.org/docs/master/manager-extensions/resources, https://piranhacms.org/docs/master/extensions/modules

### 7. Multi-site is single-tenant: shared media library, all users see all sites, no per-site content-type/user restriction

**CONFIRMED — verbatim match.**
- Official "How to use multitenancy" doc lists exactly these three restrictions: "You cannot restrict content types per site", "You cannot restrict users to a single site", "You cannot restrict sites to a single media library." — https://piranhacms.org/docs/master/tutorials/how-to-use-multitenancy
- This is a precise match to the spine's Deferred-section claim and the memlog's cited GitHub issue #982 discussion.

### 8. .NET 8 LTS still within support window as of Sept 2026; CentOS Stream 9 supported/current

**PARTIALLY CONFIRMED — literally true, but the underlying rationale is stale and should be revisited.**
- **.NET 8 (LTS):** confirmed still in support on Sept 18, 2026 — but Microsoft's own .NET blog states **.NET 8 and .NET 9 both reach End of Support on the same date: November 10, 2026** (https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/). That's **under two months** after this spine's creation date.
- **.NET 9 (STS):** Microsoft extended STS support from 18→24 months, which is *why* .NET 9's end-of-support date moved to also land on November 10, 2026 — i.e., .NET 8 and .NET 9 now expire on the exact same day. The memlog's rationale ("`.NET 8 chosen for LTS stability over .NET 9 STS`") is no longer a meaningful distinction: neither gives materially more runway than the other from today.
- **What wasn't considered: .NET 10.** Released November 2025, it is the *current* LTS, supported through **November 2028** (https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). Piranha CMS 12.2.0 does **not** yet explicitly target net10.0 (only net8.0/net9.0 are declared TFMs; net10.0 shows only as inferred/"computed" compatibility on NuGet) — so there may be a real reason to stay on .NET 8/9 for now, but that reason (Piranha's own TFM support lag) isn't the one written down, and the current framing overstates .NET 8's remaining runway.
- **CentOS Stream 9:** confirmed still an actively supported, current .NET 8/9 host per Microsoft's official supported-OS matrix (https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md), with its own EOL not until **May 31, 2027** (tied to RHEL 9's full-support phase). This half of the claim holds without caveat.
- **Recommendation:** before build starts, explicitly re-decide .NET 8 vs 9 vs 10 with the Nov 10, 2026 EOL and Piranha's actual net10.0 support status in view, rather than carrying forward "LTS beats STS" as the deciding rationale — that reasoning has an expiration date of its own now.

---

## Summary table

| # | Claim | Verdict |
|---|---|---|
| 1 | Piranha 12.2.0, ~July 16 2026, cross-compiled .NET 8/9 | CONFIRMED |
| 2 | Piranha.Templates 12.0.0, `piranha.mvc`, `-d MySql` | CONFIRMED |
| 3 | Piranha.Data.EF.MySql 12.0.0, Pomelo-based | CONFIRMED |
| 4 | Native Alias, IApi.Aliases/AliasRouter, 301/302, cross-domain RedirectUrl | CONFIRMED (note: 256-char length cap not mentioned in spine) |
| 5 | Comment/PostComment model, fields, Post-scoped, not on IApi | CONFIRMED |
| 6 | Manager UI extensibility is documented/supported | CONFIRMED |
| 7 | Multi-site single-tenant: shared media, all-users-all-sites | CONFIRMED (verbatim doc match) |
| 8 | .NET 8 LTS in-window Sept 2026; CentOS Stream 9 current | PARTIALLY CONFIRMED — literally true but rationale is stale (.NET 8 & 9 both EOL Nov 10 2026; .NET 10 LTS not considered) |
