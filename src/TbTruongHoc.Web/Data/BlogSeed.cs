#nullable enable

using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// Story 5.1: idempotent startup seed for Site B's "Blog" page - a top-level
/// <see cref="BlogArchive"/> (slug <c>blog</c>) with only the title set. It is
/// created published and visible, so the sitemap-driven nav shows "Blog"
/// straight away, with the empty-state message until posts are published.
/// No posts are seeded: articles are the client's content.
///
/// Works per slug, following <see cref="LandingPageSeed"/>: the page is
/// created only when Site B has no page with that slug, and an existing page
/// is never modified.
/// </summary>
public static class BlogSeed
{
    internal const string Slug = "blog";
    internal const string Title = "Blog";

    public static async Task EnsureSeededAsync(IApi api)
    {
        var site = await api.Sites.GetByInternalIdAsync(SiteSeed.TrongDoiTamInternalId);
        if (site == null)
        {
            return;
        }

        await EnsureSeededAsync(api, site.Id);
    }

    /// <summary>
    /// Seeds the given site. The new page is appended after the existing
    /// top-level pages.
    /// </summary>
    internal static async Task EnsureSeededAsync(IApi api, Guid siteId)
    {
        if (await api.Pages.GetBySlugAsync<PageInfo>(Slug, siteId) != null)
        {
            return;
        }

        // An editor may have renamed the seeded page's slug: any existing
        // BlogArchive on the site counts as "already seeded".
        var pages = await api.Pages.GetAllAsync<PageInfo>(siteId);
        if (pages.Any(p => p.TypeId == nameof(BlogArchive)))
        {
            return;
        }

        var sitemap = await api.Sites.GetSitemapAsync(siteId, onlyPublished: false);

        var page = await api.Pages.CreateAsync<BlogArchive>();
        page.SiteId = siteId;
        page.ParentId = null;
        page.SortOrder = sitemap.Count;
        page.Title = Title;
        page.Slug = Slug;
        page.Published = DateTime.Now;
        page.IsHidden = false;
        await api.Pages.SaveAsync(page);
    }
}
