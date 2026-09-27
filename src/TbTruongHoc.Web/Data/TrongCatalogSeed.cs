#nullable enable

using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// Story 2.2: idempotent startup seed for Site B's Trống catalog - the hub
/// "Trống" (<see cref="ProductHubPage"/>, slug <c>trong</c>) plus its 5
/// <see cref="ProductArchive"/> subcategories, published, title + slug only
/// (no prose - the client authors that in the CMS).
///
/// Runs only when Site B has none of the 6 seed slugs (hub + children).
/// Once any of them exists the whole seed is skipped, so editor edits (titles, slugs of the
/// children, deleted/hidden children) always stick; existing pages are never
/// modified. Deleting the hub itself re-seeds it on the next start.
/// </summary>
public static class TrongCatalogSeed
{
    internal const string HubSlug = "trong";
    internal const string HubTitle = "Trống";

    internal static readonly IReadOnlyList<(string Title, string Slug)> Subcategories = new[]
    {
        ("Trường học", "trong/truong-hoc"),
        ("Lân", "trong/lan"),
        ("Đội", "trong/doi"),
        ("Lễ hội", "trong/le-hoi"),
        ("Chùa", "trong/chua"),
    };

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
    /// Seeds the given site. Skipped when any of the 6 seed slugs already
    /// exists there, so a renamed hub never re-seeds into colliding child
    /// slugs.
    /// </summary>
    internal static async Task EnsureSeededAsync(IApi api, Guid siteId)
    {
        foreach (var slug in new[] { HubSlug }.Concat(Subcategories.Select(c => c.Slug)))
        {
            if (await api.Pages.GetBySlugAsync<PageInfo>(slug, siteId) != null)
            {
                return;
            }
        }

        // Append after any existing top-level pages so the hub never
        // displaces an existing start page.
        var sitemap = await api.Sites.GetSitemapAsync(siteId, onlyPublished: false);
        var published = DateTime.Now;

        var hub = await api.Pages.CreateAsync<ProductHubPage>();
        hub.SiteId = siteId;
        hub.ParentId = null;
        hub.SortOrder = sitemap.Count;
        hub.Title = HubTitle;
        hub.Slug = HubSlug;
        hub.Published = published;
        await api.Pages.SaveAsync(hub);

        for (var i = 0; i < Subcategories.Count; i++)
        {
            var (title, slug) = Subcategories[i];

            var archive = await api.Pages.CreateAsync<ProductArchive>();
            archive.SiteId = siteId;
            archive.ParentId = hub.Id;
            archive.SortOrder = i;
            archive.Title = title;
            archive.Slug = slug;
            archive.Published = published;
            await api.Pages.SaveAsync(archive);
        }
    }
}
