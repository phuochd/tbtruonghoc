#nullable enable

using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// Story 2.5: idempotent startup seed for Site B's craftsman story page -
/// a top-level draft <see cref="CraftsmanStoryPage"/> (slug
/// <c>cau-chuyen-nghe-nhan</c>) with only the title and quote attribution
/// set, hidden from the nav (it is reached from the product-line pages and
/// the footer). The prose, quote and photos are the client's content, so
/// none are seeded; the editor publishes the page once they are in.
///
/// Works per slug, following <see cref="ProductLineSeed"/>: the page is
/// created only when Site B has no page with that slug, and an existing page
/// is never modified. Deleting it re-seeds it on the next start.
/// </summary>
public static class CraftsmanStorySeed
{
    internal const string Slug = "cau-chuyen-nghe-nhan";
    internal const string Title = "Câu chuyện nghệ nhân";
    internal const string Attribution = "Nghệ nhân Phạm Trí Trong";

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

        var page = await api.Pages.CreateAsync<CraftsmanStoryPage>();
        page.SiteId = siteId;
        page.ParentId = null;
        page.SortOrder = sitemap.Count;
        page.Title = Title;
        page.Slug = Slug;
        page.QuoteAttribution = Attribution;
        page.Published = null;
        // Reached from the product-line pages and the footer, not the nav.
        page.IsHidden = true;
        await api.Pages.SaveAsync(page);
    }
}
