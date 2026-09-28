#nullable enable

using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// Story 3.1: idempotent startup seed for Site B's first paid-ads landing
/// page - a top-level draft <see cref="LandingPage"/> (slug
/// <c>thung-ruou-go-qua-tet</c>) with only the title set. It is hidden from
/// the nav (the Pages save hook forces that anyway) and noindex, so the ad
/// page never competes with the organic Thùng rượu gỗ category page; the
/// editor can change indexing in the SEO tab. Prices, variants and prose are
/// the client's content, so none are seeded.
///
/// Works per slug, following <see cref="CraftsmanStorySeed"/>: the page is
/// created only when Site B has no page with that slug, and an existing page
/// is never modified.
/// </summary>
public static class LandingPageSeed
{
    internal const string Slug = "thung-ruou-go-qua-tet";
    internal const string Title = "Thùng rượu gỗ – Quà Tết từ làng nghề Đọi Tam";

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

        var sitemap = await api.Sites.GetSitemapAsync(siteId, onlyPublished: false);

        var page = await api.Pages.CreateAsync<LandingPage>();
        page.SiteId = siteId;
        page.ParentId = null;
        page.SortOrder = sitemap.Count;
        page.Title = Title;
        page.Slug = Slug;
        page.Published = null;
        page.IsHidden = true;
        page.MetaIndex = false;
        await api.Pages.SaveAsync(page);
    }
}
