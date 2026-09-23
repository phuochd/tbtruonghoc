---
name: 'Adversarial Review — Ngoc Anh Multi-Site Rebuild Architecture Spine'
type: review
reviews: architecture-tbtruonghoc-2026-09-18/ARCHITECTURE-SPINE.md
verdict-only-summary: see top
created: '2026-09-18'
---

# Adversarial Review: ARCHITECTURE-SPINE.md (Ngoc Anh Multi-Site Rebuild)

**Verdict:** The spine correctly rules out the loud divergences (separate deployments, Comment-table abuse, hand-rolled redirect middleware, stray price fields on products) but leaves quiet, load-bearing details unspecified — canonical schemas, ownership/sequencing of shared artifacts, and "which type governs which content" boundaries — so two literal-minded builders can each honor every AD and still produce incompatible systems.

Below, each finding gives: the concrete scenario, which AD/convention should have caught it and how it falls short, and a suggested tightened rule.

---

## AD-1 — Single Piranha instance, multi-site

### Finding 1.1 — No enforcement hook, only rule text; the tooling's own default path violates it
**Scenario:** A builder is handed "build Site B" as a ticket, without re-reading the whole spine (or working from a different machine/contractor setup than whoever built Site A). Piranha's own official quickstart is `dotnet new piranha.mvc -d MySql`, which scaffolds a *fresh* project with its own new database by convention. Following the tool's own onboarding path — not re-deriving AD-1 from first principles — naturally produces a second instance/second DB. AD-1's rule ("do not split into per-site deployments") is stated, but nothing in the spine *operationalizes* it: there's no "there is exactly one `.sln`, one `appsettings.Production.json`, one connection string, and Site B's ConnectionString must equal Site A's" statement, and no acceptance check a second builder can run to self-verify before writing code.
**AD gap:** AD-1 states the invariant but provides no artifact-level tripwire (single repo, single connection string, single `Startup`/`Program.cs`) that a builder unfamiliar with the rest of the spine would trip over early.
**Suggested tightening:** Add to AD-1's Rule: "There is exactly one solution/repository and one `appsettings` connection string for both sites; Site B's `ConnectionString` and `Program.cs` are additions to Site A's existing project, never a fresh `dotnet new piranha.mvc` scaffold. Before writing any Site B code, confirm the repo/DB you're pointed at already contains Site A's migrations."

### Finding 1.2 — Site-record ownership and "default site" flag are unassigned
**Scenario:** Piranha requires exactly one `Site` marked `IsDefault` (the fallback when no hostname matches, e.g. bare IP/localhost hits). Site A's builder, working first, reasonably marks Site A default. Site B's builder, doing local dev, seeds their own dev DB with Site B marked default (convenient for their own `localhost` testing) and/or binds Site B's Hostnames to `localhost` too. Both builders followed AD-1 to the letter (one instance, one DB, no separate deployment) — the collision only surfaces when their seed scripts/migrations are merged into the single shared DB AD-1 mandates, at which point either the default-site flag flips unexpectedly or two `Site` rows claim the same hostname string, and Piranha's routing becomes ambiguous.
**AD gap:** AD-1 says "one instance, one DB" but never says who creates `Site` records, in what order, or who owns the `IsDefault`/`Hostnames` values — an omission the user's brief called out directly.
**Suggested tightening:** New clause under AD-1: "Site A's builder creates and owns the `Site` record for tbtruonghoc (marked `IsDefault`) as part of initial scaffold; Site B's builder adds a second `Site` record only, with explicit, non-overlapping `Hostnames` (never `localhost` in a shared/merged DB) and `IsDefault=false`. No other builder edits an existing `Site` record's `IsDefault` or `Hostnames` field."

### Finding 1.3 — No shared-repo/migration-coordination convention, so two builders create two divergent EF Core migration histories
**Scenario:** Site A and Site B builders work in parallel (per the brief's own framing: "two different developers... without talking to each other"). Each, needing to add their own Post/Page types (Product, LandingPage) and the FormSubmission table, adds their own EF Core migrations against their own local copy of the project. When merged, migration IDs/order conflict, or worse, both add a similarly-named-but-differently-shaped `FormSubmission` migration (see Finding 3 below) that EF Core can't reconcile without manual surgery.
**AD gap:** AD-1 governs deployment topology, not the single-codebase/single-migration-history discipline that topology depends on. There's no stated branching/merge convention or "who owns Migrations/" rule.
**Suggested tightening:** Add a Consistency Convention row: "EF Core migrations are added serially against one shared branch; before adding a migration, pull and rebase on the latest Migrations/ history — never generate a migration against a stale local snapshot of the schema."

---

## AD-2 — Archive+Post vs. standalone Page

### Finding 2.1 — The "Danh mục sản phẩm" hub's *internal* data-sourcing pattern is unconstrained
**Scenario:** The hub is correctly classified as a Standalone Page (not paginated, not an Archive) — that part is unambiguous. But the spine only classifies the page *type*, not how the hub obtains its list of categories to display. Builder 1 (Site A) implements the hub as a manually curated set of links (an editor-picked list of Archive pages), matching the spirit of the FR-5 "plain hyperlink, no automation" convention. Builder 2 (Site B), building the analogous hub for FR-9's product lines, instead writes a custom query in the hub's view/controller that walks the page tree at render time and auto-lists every child Archive with live product counts/thumbnails pulled directly from Posts — i.e., hand-rolled aggregation logic that duplicates exactly what Archive+Post's native pagination/listing already provides, which is the precise failure AD-2 exists to prevent ("duplicating pagination logic by hand"). Both builders technically satisfy "the hub is a Standalone Page, excluded from Archive listing" — the AD is silent on *how* the standalone page may source its list content, so one of the two implementations quietly reintroduces the duplicated-listing-logic problem inside a page nominally exempt from it.
**AD gap:** AD-2 constrains page *type* selection, not implementation pattern for pages that reference/aggregate Archive+Post content.
**Suggested tightening:** Add to AD-2: "A Standalone Page (hub, story page, landing page) may reference Archive/Post content only via manually curated, editor-placed references (page/post picker fields) — never by querying/aggregating the page tree or Post set programmatically. Any page that needs live, auto-updating listing behavior is by definition an Archive page, not a standalone one."

### Finding 2.2 — Page→Post "featured/related content" relation has two legitimate-looking patterns and the spine picks neither
**Scenario:** The craftsman/heritage story page (FR-10, Standalone Page) plausibly needs to reference specific products ("products made using this joinery technique"). The only relevant Consistency Convention is "Cross-site linking (FR-5): a plain hyperlink... no dedicated link-block component" — but that convention is scoped to *cross-site* (FR-5) links, not same-site Page→Post references. Builder A (Site B craftsman page) reasonably generalizes FR-5's philosophy and uses a plain manual hyperlink in rich text. Builder B (working the Site A category/landing pages, or a later contributor to the same page) instead builds a structured, repeatable "Related Products" region using Piranha's native Post-picker field (a legitimate, arguably *better* CMS practice, and not itself an "automated/sitewide link list"). Neither violates AD-2 or the FR-5 convention's literal text, yet the two patterns produce incompatible editor workflows and incompatible markup/partial-view expectations for what should be one conceptual relation ("this content references that product").
**AD gap:** No AD or convention row governs same-site structured content references (only cross-site links and Archive/Post shape are covered); the gap is invisible until two builders pick different patterns for the same relation.
**Suggested tightening:** New Consistency Convention row: "Same-site Page→Post/Archive references (e.g., craftsman page → related products, hub → categories): always a repeatable Post/Page-picker region field, never inline rich-text links. Rich-text manual hyperlinks are reserved for FR-5 cross-site editorial backlinks only."

---

## AD-3 — Lead capture: FormSubmission table

### Finding 3.1 — "All submitted fields, tagged by form type" under-specifies the schema; two builders produce two incompatible tables
**Scenario:** FR-3 (general contact: name, phone, email, message) and FR-8 (survey/request: possibly product interest, quantity, delivery address, preferred contact time) have different field sets. The rule says submissions write to "a custom EF Core table... all submitted fields, tagged by form type" — this sentence is compatible with at least three different, mutually incompatible implementations: (a) one wide table with a nullable column per possible field across all forms, (b) one table with fixed common columns (Name/Phone/Email/CreatedAt/FormType) plus a JSON/`Fields` blob column for form-specific data, or (c) a shared base table plus per-form-type subtables (TPT/TPH inheritance). Builder A (FR-3) migrates a table shaped like (a). Builder B (FR-8), unaware of Builder A's migration or reading the same sentence differently, adds their own migration assuming (c) — a separate `SurveySubmission` table — reasoning that "tagged by form type" means each form type gets its own tagged table, not that they share one. Both cite AD-3 as their justification. The result is exactly what AD-3 was written to prevent ("each site/form inventing its own storage") — produced by two people who both read AD-3 and both believe they're complying with it.
**AD gap:** AD-3 names the mechanism (custom EF Core table, not Comment) and the presentation layer (Manager extension) but not the schema shape or a single migration owner — the exact ambiguity the task brief predicted.
**Suggested tightening:** Add to AD-3's Rule: "FormSubmission is ONE table: fixed columns `Id, FormType (enum: Contact|Survey), Name, Phone, Email, CreatedAt, ProcessedAt` plus one `FieldsJson` column holding any form-specific extra fields as JSON. Only one migration ever creates/alters this table; a builder adding a new form type extends `FormType`'s enum and writes into `FieldsJson`, never adds a new table or a new fixed column without updating this spine first."

### Finding 3.2 — Email notification path is unowned, inviting two separate notification implementations
**Scenario:** "An email notification fires on every new submission" — no shared service/abstraction is named. Builder A wires SMTP directly into the FR-3 controller using ASP.NET Core's basic `SmtpClient`; Builder B, working FR-8 later, pulls in a transactional-email SDK (e.g., a different NuGet package) for the survey form, with its own sender address, template, and retry/failure semantics. Two independent, differently-configured notification paths now exist for what the spine implies is one behavior ("an email notification fires").
**AD gap:** AD-3 mandates the behavior but not a shared implementation point (one `INotificationService`, one set of SMTP/API credentials, one template).
**Suggested tightening:** Add: "Both forms call one shared `IFormNotificationService` (single implementation, single outbound email configuration) — never per-form ad hoc SMTP/API calls."

---

## AD-4 — Alias-based redirects

### Finding 4.1 — No owner/sequencing rule for legacy-URL → Site-B-URL mapping data, only for the mechanism
**Scenario:** AD-4 correctly establishes that `Alias.RedirectUrl` can hold an absolute cross-domain URL, so Site B not yet existing as a `Site` record is *not* a technical blocker (the target is just a string). But the spine says nothing about who authors the actual mapping (which of ~N legacy trống URLs on tbtruonghoc.com maps to which new trongdoitam.net URL) or when. Concrete failure: the Site A migration builder, working ahead of Site B's build, creates Alias rows pointing at a *draft* Site B URL scheme (e.g., guessed slugs). The Site B builder, independently applying the Consistency Convention that slugs are "human-readable, Vietnamese... editable post-publish," finalizes different actual slugs once real content/SEO input arrives. Nothing re-validates the now-published Site A aliases against Site B's real, final URLs — both builders followed AD-4 and the slug convention to the letter, and the redirects 301 to dead links.
**AD gap:** AD-4 governs the mechanism (Alias, SiteId scoping, string-target support) but not the data-ownership/sequencing of the mapping itself, nor a validation step tied to Site B's launch.
**Suggested tightening:** Add: "The legacy-URL→new-URL mapping is a single manifest (owned by whoever runs the migration), and Site A's cross-domain aliases are only *published* (not merely drafted) after Site B's URL/slug scheme for the target pages is frozen. A link-check pass against every Site-A-scoped cross-domain alias runs once Site B goes live, before Site A's legacy pages are decommissioned."

---

## AD-5 — Pricing separation

### Finding 5.1 — The isolation is enforced in only one direction: nothing stops a product from being modeled as a Landing Page (not a Product Post) to get a visible price
**Scenario:** AD-5 explicitly forbids adding a price field to the Product Post Type. It says nothing about the reverse: a Site B builder, asked by the paid-ads/marketing side to give one flagship product a visible "starting from" price for a promo, satisfies that request by modeling the content as a **Landing Page Type** instance (the only type AD-5 permits to carry price) instead of a Product Post under its danh mục Archive. This is fully compliant with both AD-2 (it's legitimately standalone, non-paginated) and AD-5 (only the Landing Page Type carries price) — yet it quietly pulls one product out of its category's Archive+Post taxonomy (no pagination, tags, or category listing membership) and breaks the sitewide "products are always contact-for-quote" invariant the AD was written to protect, without ever touching the forbidden path (adding price to Product Post).
**AD gap:** AD-5 protects the Product Post Type from acquiring pricing, but doesn't constrain what content is allowed to *be* a Landing Page Type instance — so "which type must a product use" is unenforced in the direction that actually matters for catalog integrity.
**Suggested tightening:** Add to AD-5: "A Landing Page Type instance is only ever a paid-ad campaign page (FR-12), never the canonical page for a catalog product — every catalog product (Site A or Site B) has exactly one canonical Product Post under its danh mục Archive; a Landing Page may link to it, but must not substitute for it."

### Finding 5.2 — Landing Page Type could absorb non-pricing landing-page use cases, diluting "pricing region" as a signal
**Scenario:** FR-8's survey-request form might want its own paid-ad landing page with no pricing content at all (a lead-gen page, not a product-pricing page). Because "Landing Page Type" is the only standalone page type the spine names for FR-12-shaped needs, a builder reuses it for this unrelated non-pricing campaign, leaving the repeatable variant+price region empty. Nothing in AD-5 prohibits this, and it isn't harmful today, but it means "has a Landing Page Type" no longer reliably signals "has visible pricing," weakening the isolation's legibility for the next builder auditing the codebase for price fields.
**AD gap:** AD-5 defines what Landing Page Type is *for* (FR-12/13, pricing) but not what other campaign needs are allowed to reuse it.
**Suggested tightening:** Clarify AD-5's Rule: "Landing Page Type is reserved for pricing-bearing paid-ad pages (FR-12/13). A non-pricing paid-ad/lead-gen page uses a separate, simpler standalone Page — never Landing Page Type merely for convenience."

---

## Consistency Conventions table

### Finding C.1 — Cross-site linking convention doesn't say *where* the hyperlink lives, so rendered output diverges
**Scenario:** "A plain hyperlink placed manually by the content editor inside a Post/Page rich-text block" is satisfied both by (a) a raw `<a>` typed into the free-text HtmlBlock, and (b) a single structured `Link`/`Url` field added to a Post's regions (still "manual," still not "a dedicated link-block component or automated list" under a strict reading). Site A's builder does (a); Site B's builder, wanting consistent styling, does (b). The two sites now render cross-site backlinks with different markup/styling and different editor UX, and any shared partial view built assuming one shape breaks for the other site.
**AD gap:** The convention constrains automation/farming risk but not authoring-surface consistency.
**Suggested tightening:** "...placed as inline text within the rich-text HtmlBlock body itself — not as a separate structured Link field/region," closing the (a)/(b) ambiguity explicitly.

### Finding C.2 — Media storage: no folder/namespace convention despite two sites and (eventually) two editors sharing one library
**Scenario:** Site A and Site B builders both upload directly into the shared media library's root with no per-site folder convention. Today (Phuoc as sole admin) this is low-risk, but it's a small, cheap gap: nothing stops a future second editor (explicitly flagged as a "Deferred" risk already) from picking the wrong site's photo in the Manager's media picker, since nothing visually/structurally separates Site A's and Site B's assets.
**AD gap:** Convention states storage backend (local filesystem, shared) but not organizational structure within it.
**Suggested tightening (low priority, can ride with the existing Deferred item):** "Media is organized under `/SiteA/` and `/SiteB/` root folders in the library from the start, even though both are technically browsable by any user" — costs nothing now, pays off if/when a second editor is added.

---

## Summary Table

| # | AD / Convention | Gap type |
| --- | --- | --- |
| 1.1 | AD-1 | No enforcement hook; tooling's default path violates the rule |
| 1.2 | AD-1 | Site-record/default-site/hostname ownership unassigned |
| 1.3 | AD-1 | No shared-repo/migration-coordination convention |
| 2.1 | AD-2 | Hub page's internal data-sourcing pattern unconstrained (can reintroduce hand-rolled pagination) |
| 2.2 | AD-2 | Page→Post reference pattern (structured region vs. inline link) unspecified |
| 3.1 | AD-3 | FormSubmission schema shape (wide table / JSON blob / subtables) unspecified, no single migration owner |
| 3.2 | AD-3 | Email notification path unowned, invites duplicate implementations |
| 4.1 | AD-4 | Legacy-URL→Site-B-URL mapping ownership/sequencing vs. Site B's slug-freeze unspecified |
| 5.1 | AD-5 | Isolation enforced one-directionally; nothing stops a product being modeled as Landing Page Type |
| 5.2 | AD-5 | Landing Page Type could be diluted by non-pricing reuse |
| C.1 | Consistency Conventions | Cross-site link authoring surface (inline text vs. structured field) unspecified |
| C.2 | Consistency Conventions | No media folder/namespace convention |
