---
title: 'Story 6.2: Site A homepage category grid + trust band, and the aggregate "Danh mục sản phẩm" page'
type: 'feature'
created: '2026-09-30'
status: 'done'
baseline_commit: 'c48372071b239474b72a51c5645e9db8fb7fc5f4'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-tbtruonghoc-2026-09-18/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Site A's homepage stops after the hero, and its "Sản phẩm" hub (a `ProductHubPage`, target of the dropdown's "Xem tất cả") still renders Site B's `sb-` hub view. Visitors can't browse featured categories, see the trust stats, or search/filter the full category list.

**Approach:** Extend `SiteAHomePage` with a 6-tile featured-category grid, a "Xem tất cả {N} nhóm sản phẩm →" link and a CMS-authored `trust-stat` band. Give Site A's `ProductHubPage` its own view: breadcrumb, header, `category-search` (live text search + multi-select filter chips), the full tile grid and a distinct placeholder tile. All lists come from `ProductCatalog.GetHubTilesAsync`, the same hide rule as the nav.

## Boundaries & Constraints

**Always:**
- Tiles come only from `GetHubTilesAsync`, so the hidden, draft and empty categories stay hidden. The hub is the first top-level `ProductHubPage` in the Site A sitemap (the same one the nav uses). {N} is the tile count. No hub or no tiles → the homepage section is omitted.
- `category-tile`: the whole tile is one link, with a `bg` fill, a `border-neutral` border, `rounded.xl`, no shadow and a `primary` icon block (`rounded.md`). The block shows the category's primary image when one is set, else a plain teal square. Below it: title, then excerpt. Homepage grid: 3/2/1 columns (≥1024/≥768/<768). Aggregate grid: 4/2/1 columns.
- Certification pills (`trust-badge` look, at most 2) render **only** on aggregate tiles and **only** when the category's new optional "Chứng nhận" field is filled. Never a default value.
- Filter chips come from the distinct values of a new optional per-category "Nhóm lọc" field (comma-separated, trimmed, case-insensitive dedupe, first-seen order). A category may sit in several groups. No values anywhere → no chip row.
- Search and filter are client-side (`site-a.js`). The search is diacritic- and case-insensitive over title and excerpt. Chips are ORed with each other and ANDed with the search. Active chips get `aria-pressed="true"` and the trust colors. When nothing matches, show a polite live-region message "Không tìm thấy nhóm sản phẩm phù hợp." Without JS: every tile shows and the search/chips are hidden.
- `trust-stat` band: a CMS list of (number, label) on `SiteAHomePage`, number over label, on `surface-tint`/`border-tint`, placed below the category grid. Empty list → no band. Nothing is seeded into production content, because the claims must be true.
- Breadcrumb on the aggregate page: "Trang chủ / {hub title}". Links in primary, current step as plain text, `aria-label="Breadcrumb"`.
- The aggregate header is the hub's title (H1) and excerpt. The hub's own content blocks still render (except `ConfigBlock`).
- **Decision (Q1):** `SiteAHomePage` gets a "Nhóm nổi bật" page-picker list. The homepage shows the picks, in list order, that are among the hub's tiles (so hidden or empty picks are skipped), at most 6. When nothing valid is picked → the first 6 tiles in hub order.
- **Decision (Q2):** the placeholder is a visitor-facing dashed tile (`border-neutral` dashed, no icon fill) reading "Chưa thấy thiết bị bạn cần?" / "Liên hệ để được tư vấn". It links to `tel:` (SiteSettings phone via `ContactLinks`), else to the Zalo URL, else it renders without a link. It is always last and ignored by search/filter, so it stays visible.
- **Decision (Q3):** the spec is kept whole (about 2.4k tokens), accepted by the owner.

**Never:**
- Change Site B's hub output (`ProductHub.cshtml` stays byte-identical for Site B), `_CategoryTile.cshtml` or Site B CSS.
- Hardcode the category list, the count or the stats. No discount/urgency copy. No shadows or gradients on tiles or the band.
- Build the category pages or PDPs (6.3–6.5).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Home, 10 visible categories | 10 tiles | 6 tiles + "Xem tất cả 10 nhóm sản phẩm →" to hub | N/A |
| Home, 3 visible | 3 tiles | 3 tiles + "Xem tất cả 3 …" | N/A |
| Home, no hub/tiles | none | No category section; the band still shows if stats exist | N/A |
| Aggregate, 10 tiles | 10 tiles, 1 empty category | 10 tiles + placeholder; empty category absent | N/A |
| Groups | A:"Mầm non, Ngoài trời", B:"mầm non" | Chips "Mầm non", "Ngoài trời"; "Mầm non" shows A+B | Blank/whitespace entries dropped |
| Certification | "ASTM, CARB P2, TT 38" | First 2 pills on aggregate only | Blank → none |
| Search | "du che" | Only "Dù che nắng sân trường" visible | 0 hits → message |
| Site B hub | any | Unchanged `sb-` hub markup | N/A |

</frozen-after-approval>

## Code Map

- `src/TbTruongHoc.Web/Models/SiteAHomePage.cs` -- add the trust-stat list region (`TrustStat` region class: `Number`, `Label` StringFields), plus the Q1 `FeaturedCategories` `IList<PageField>` region. Reuse `Trimmed`.
- `src/TbTruongHoc.Web/Models/ProductArchive.cs` -- add optional `FilterGroups` ("Nhóm lọc (Site A)") and `Certifications` ("Chứng nhận (Site A)") StringField regions plus parse helpers. These also appear, unused, in Site B's Manager; the descriptions say so.
- `src/TbTruongHoc.Web/Models/ProductHubPage.cs` -- `CategoryTileModel` is a record(Title, Excerpt, Permalink). Extend it with optional `Id`, `ImageUrl`, `Groups`, `Certifications` defaults so Site B callers compile unchanged.
- `src/TbTruongHoc.Web/Services/ProductCatalog.cs` -- `GetHubTilesAsync` is the single hide rule. Load the child as `ProductArchive` (instead of `PageInfo`) only to fill the new tile fields. Add `FindSiteHubAsync(siteId)`: the first top-level hub, via `ProductHubPage.IsHub`.
- `src/TbTruongHoc.Web/Controllers/CmsController.cs` l.106 `ProductHub`, l.309 `SiteAHomePage` -- the hub picks the view `SiteAProductHub` when the site's `InternalId == SiteSeed.TbTruongHocInternalId` (via `_api.Sites.GetByIdAsync`, Piranha-cached). The home action fills the tiles/hub on the model.
- `src/TbTruongHoc.Web/Views/Cms/SiteAHomePage.cshtml` -- hero → blocks → featured grid → band.
- `src/TbTruongHoc.Web/Views/Cms/ProductHub.cshtml`, `Views/Shared/_CategoryTile.cshtml` -- Site B. Don't modify; copy structure only.
- `src/TbTruongHoc.Web/Views/Shared/_SiteANav.cshtml` -- hub detection pattern.
- `src/TbTruongHoc.Web/wwwroot/assets/css/site-a.css` -- tokens, `.sa-grid` (l.1017), breakpoints 768/1024. `wwwroot/assets/js/site-a.js` -- progressive-enhancement pattern.
- `src/TbTruongHoc.Web/Data/SiteASampleSeed.cs` -- dev-only. Add sample groups/certs/stats only on a fresh seed (the hub-slug guard stays).
- `tests/TbTruongHoc.Web.Tests/SiteAShellTests.cs`, `ProductCatalogTests.cs`, `SiteAScriptTests.cs` -- render/Jint test patterns. Shared dev DB: scope asserts to the test's own ids.

## Tasks & Acceptance

**Execution:**
- [x] `Models/ProductArchive.cs`, `Models/ProductHubPage.cs`, `Services/ProductCatalog.cs` -- new category fields, tile enrichment, hub lookup, group/cert parsing.
- [x] `Models/SiteAHomePage.cs` -- trust stats + featured picks (Q1) and view-model props.
- [x] `Controllers/CmsController.cs` -- Site A hub view switch; home tiles.
- [x] `Views/Cms/SiteAHomePage.cshtml`, `Views/Shared/_SiteACategoryTile.cshtml`, `Views/Cms/SiteAProductHub.cshtml` -- markup (tile, grids, band, breadcrumb, search, chips, placeholder, empty message).
- [x] `wwwroot/assets/css/site-a.css`, `wwwroot/assets/js/site-a.js` -- styles; search/filter behavior.
- [x] `Data/SiteASampleSeed.cs` -- dev samples.
- [x] `tests/.../SiteAHomeCatalogTests.cs` (new), `SiteAScriptTests.cs`, `ProductCatalogTests.cs` -- every I/O row, Site B hub unchanged, Jint search/chip/empty-state.

**Acceptance Criteria:**
- Given the aggregate page with JS, when a visitor types in search and toggles two chips, then only tiles matching (chip1 OR chip2) AND the search stay visible, and the chips report `aria-pressed`.
- Given a 12th published, non-empty category added in the Manager, when the homepage and aggregate page reload, then counts and grids include it with no code change.
- Given the full suite, when run, then all pre-existing tests pass.

## Implementation Notes

- Implemented inline (no subagent). `GetHubTilesAsync(siteId, hubId, details: false)` keeps the nav and Site B hub on the cheap `PageInfo` path; `details: true` (Site A home/aggregate) also loads each `ProductArchive` for id, primary image, groups and certs.
- The view switch is `CmsController.IsSiteAAsync` via `SiteLayout.ForInternalId`, so Site B's `ProductHub` action path is unchanged.
- The filter chip key is `ProductHubPage.GroupKey` (trimmed, lowercased invariant). Tile groups go out as a JSON array in `data-sa-groups`; the search text (title + excerpt) goes in `data-sa-search` and is folded in JS (NFD, combining marks stripped, `đ` → `d`, whitespace collapsed).
- The empty-state live region stays in the DOM; JS sets or clears its text (`:empty` hides it).
- Placeholder: `tel:` link, else a Zalo link (new tab), else a plain `div`. A visually hidden suffix names the destination.
- Homepage tiles use `h3` (under the "Nhóm sản phẩm nổi bật" `h2`); aggregate tiles use `h2`.
- **Deviation from Code Map:** `SiteASampleSeed` fills blanks on every run (like its contacts) rather than only on a fresh seed, so an existing dev DB also shows the excerpts, chips, sample certs and stats. Every sample cert and stat is marked "(mẫu)". Dev-only and opt-in, as before.
- Homepage render tests use a throwaway site (own start page + hub), because the shared dev DB's Site A sample hub would otherwise decide which hub is "first".

## Spec Change Log

## Review Triage Log

Pass 1 (blind = B, edge-case = E, verification-gap = V):

| # | Finding | Verdict | Evidence / route |
|---|---------|---------|------------------|
| B1 | Homepage loads details for all hub tiles, not just the 6 shown | low | Real extra loads, but Piranha's memory cache absorbs repeats and the fix adds public surface → rejected. |
| B2 | Empty live region is `display:none`, so "no results" may not be announced | medium | `:empty{display:none}` takes the role=status node out of the a11y tree → patch (`:empty{margin:0}`). |
| B3/V6 | Seed doc says "(mẫu)" on excerpts/groups and "skipped entirely once hub exists" | low | Both comments wrong after the change; direct correction → patch. |
| B4/E6/E7 | Dev seed refills blanks every run / may drop a pending draft | low | Dev-only, opt-in; same pattern as the 6.1 contact fill; guard adds branches → rejected (noted deviation in Implementation Notes). |
| B5 | Picks never top up to 6 | false | Frozen Q1: picks only, fallback only when none valid. Field description now states it (direct text fix, patched). |
| B6 | Search ignores group names | false | Frozen: search is over title and excerpt. |
| B7/V3 | Placeholder link target untested | low | Real gap; test only → patch (asserts tel/Zalo/div per Site A settings). |
| B7b | Hidden-category / ConfigBlock / draft-preview paths untested on Site A views | low | Hide rule already covered by ProductCatalogTests; ConfigBlock skip is copied code → rejected. |
| B8 | Homepage render tests run on a throwaway site, not the Site A layout | low | Layout wiring covered by 6.1 shell tests; the Site A DB's sample hub makes "first hub" nondeterministic → rejected. |
| B9/E4/E5 | NFC/NFD duplicate chips; dedupe key differs from match key | low | Real for NFD pastes; direct fix → patch (NFC in SplitList/GroupKey, dedupe by GroupKey) + unit test. |
| B10 | Placeholder target/rel built as one unquoted Razor string | low | Works but fragile; direct correction → patch (conditional attributes). |
| B11 | Draft preview mixes versions | false | Tiles come from published children only; title/excerpt and details are both published data. |
| B12 | Chip state not restored on back/forward; no URL state; no clear-all | low | Not in intent; needs new code → rejected. |
| B13 | `HasImage` doesn't check media type | false | Piranha's ImageField picker only accepts images. |
| E1/E9 | First hub with 0 tiles hides the homepage grid while a later hub feeds the nav | low | Site A has one hub; rule matches spec ("first top-level hub") → rejected. |
| E2 | Hub lookup ignores page permissions | low | No page permissions in use on Site A; needs a user parameter → rejected. |
| E3 | Homepage draft preview with an unpublished hub shows no grid | low | Editor-only preview edge; rejected. |
| E8 | `image.Media` assigned on a possibly cached instance | maybe-false | Only when Piranha left Media null; would be low → rejected. |
| E10 | Tests live in new files, not the listed ones | false | Coverage exists in SiteAHomeCatalogTests/SiteACatalogScriptTests; the task file list is guidance. |
| V1 | `data-sa-search` never checked in rendered HTML | low | Real gap → patch (render assert "title excerpt"). |
| V2 | Tile primary image render untested | low | Real gap → patch (upload PNG, assert `<img>` vs empty icon). |
| V4 | `FindSiteHubAsync` hidden/ordering untested | low | Filed defer; cheap → patch (hidden + two hubs test). |
| V5 | Seed fill pass unchecked | low | Filed defer (dev-only) → deferred. |

## Design Notes

The aggregate page stays the existing Site A `ProductHubPage` (6.1 Design Notes), so the dropdown, aggregate grid and homepage share one list and one hide rule. Switching views in the controller by site keeps Site B's view untouched, the same approach as per-site layouts.

## Verification

**Commands:**
- `docker compose up -d mariadb` then `dotnet test` (repo root) -- expected: all tests pass.
- `dotnet build` -- expected: no new warnings.

**Manual checks:**
- Run the app (dev Site A samples) at 375/820/1280px: the homepage shows 6 tiles + "Xem tất cả 10…" + the band; `/san-pham` shows breadcrumb, search, chips, 10 tiles + placeholder; trongdoitam's hub is unchanged.
