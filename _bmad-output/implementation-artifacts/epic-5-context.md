# Epic 5 Context: Site B — Blog/Nội dung

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give Site B (trongdoitam.net) a blog/content section, separate from the product catalog. Visitors read articles on craft and heritage, wood-product care, and Tết gifting guides. The section supports long-tail SEO and builds trust in a craft-heritage brand. It has two surfaces. The first is a paginated Blog listing reached from Site B navigation. The second is an `article-detail-page`: each article has its own URL and SEO fields, and the page never dead-ends. Epic 7 (Site A blog) will reuse the same pattern, so build it as a reusable mechanism, not a Site B one-off.

## Stories

- Story 5.1: Trang danh sách Blog/Nội dung (FR-14)
- Story 5.2: Trang chi tiết bài viết (article-detail-page) (FR-14)

## Requirements & Constraints

- Every blog post has its own editable Title, Meta Description and Slug. These come from the platform-level SEO capability; do not rebuild them per type. Posts are indexable separately from product pages.
- The Blog link in Site B navigation is always visible, even when no posts are published.
- Listing: show published posts with native pagination. No infinite scroll.
- Empty listing: when no posts are published, show a plain "Bài viết đang được cập nhật" message. Never a bare empty list.
- Article page:
  - Title.
  - An optional byline/date metadata line.
  - Full body copy, with in-article subheads where the article is long enough to need them.
- End of the article:
  - When related posts exist, show a related-posts module.
  - When none exist, omit the module entirely and show a plain "← Quay lại Blog" back-link. No empty shell.
- Article content is written later by the client, directly in the CMS. Never hardcode or invent article copy. Stitch mock content is illustrative only.
- Banned on blog pages:
  - account/login icons or UI
  - vanity social-proof counters (like/view counts). A Stitch draft added one; it needs an explicit client decision before it ever ships.
  - cart/checkout
  - auto-rotating carousels
  - modal-on-modal stacking
- Accessibility floor is WCAG 2.1 AA:
  - Focus order follows visual reading order.
  - Tap targets are at least about 44px.
  - Images carry descriptive alt text.
  - No hover-only affordances.
  - Phone/tablet-first, older audience.
- The UI is Vietnamese only.

## Technical Decisions

- **Archive + Post, not a new system.** The Blog is an Archive page with `BlogPost` Post children, the same mechanism as the product catalog. That gives native pagination, categories and tags. Do not hand-roll listing or pagination logic. The custom `BlogPost` Post type belongs in `Models/` next to the Product and LandingPage types.
- **Shape rules.** Blog posts are Posts under the Blog archive. Standalone Pages (homepage, landing pages, craftsman story, hub) never appear in the Blog listing.
- **Related posts.** A post counts as related when it is tagged or linked as related. Use Piranha's native categories/tags rather than a hardcoded list; the exact rule is decided at story level.
- **Cross-site links.** Links to Site A are only plain hyperlinks that an editor places inside the rich-text body. Never add an automated related-links or cross-site link block. Keeping links manual stops the blog from becoming a link farm between commonly owned domains.
- **Per-site settings.** Phone, Zalo and Maps (used by the sticky-contact-bar) and analytics come from each site's `SiteSettings`. Never hardcode them.
- **Styling uses the Mộc Trầm tokens:**
  - `background` page
  - `heading-lg` title
  - `caption` metadata in `on-surface-muted`
  - `body` running copy
  - `heading-md`/`heading-sm` in-article subheads
  - related-posts label in `label` typography, `secondary` colour
  - back-link in `primary`
- **Cards.** Listing and related-post cards reuse the `product-card` grid convention, smaller on the article page. Thumbnails use the same `secondary`→`primary` gradient placeholder frame as other cards until real CMS images exist. Tag or badge chips use `secondary` (#A97142), not a saturated orange or red.

## UX & Interaction Patterns

- Blog pages use the standard site chrome: the same 56px `nav` (hamburger on mobile, hotline on tablet/desktop) and the fixed 3-segment `sticky-contact-bar`. The Stitch blog screens used a different full horizontal nav with an account icon and no sticky bar. That is a known defect; do not copy it.
- Listing cards (from the mock) show:
  - thumbnail
  - bold title
  - small muted date
  - 1–2 line excerpt
- Listing grid: 2 columns on mobile, 3–4 on desktop. Tapping anywhere on a card opens the article.
- Article page: a breadcrumb "Trang chủ / Blog / [Tên bài viết]", an H1 title and a muted date line beneath it. On desktop the body text is width-limited for readability; it does not run full-screen. The related strip "Bài viết liên quan" shows 2–3 cards.
- Voice for any microcopy is warm, calm and unembellished, with no hype or urgency.

## Cross-Story Dependencies

- 5.2 depends on 5.1 for the Blog archive and `BlogPost` type. The back-link targets the 5.1 listing.
- Depends on:
  - Epic 1: SEO fields on every Page/Post type, `SiteSettings`, and the base layout with nav and sticky-contact-bar.
  - Epic 2: the `product-card` grid convention and the Mộc Trầm tokens.
- Epic 7 (Site A blog) reuses the listing and article pattern from 5.1/5.2. Keep the `BlogPost` type and views site-agnostic enough to serve both sites.
- Epic 8 content migration may populate posts later. The pages must work with zero posts.
