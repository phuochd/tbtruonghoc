#nullable enable

using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// Story 6.1 (Q3): idempotent startup seed for Site A's homepage - a
/// published, visible top-level <see cref="SiteAHomePage"/> titled
/// "Trang chủ" with every hero field blank (the heading falls back to the
/// title, so the hero is never empty). Created only when Site A has no
/// pages at all; once any page exists - seeded or editor-made - nothing is
/// ever created or modified.
/// </summary>
public static class SiteAHomeSeed
{
    internal const string Slug = "trang-chu";
    internal const string Title = "Trang chủ";

    /// <summary>The site the startup seed targets: Site A.</summary>
    internal const string TargetInternalId = SiteSeed.TbTruongHocInternalId;

    public static Task EnsureSeededAsync(IApi api) =>
        EnsureSeededForInternalIdAsync(api, TargetInternalId);

    /// <summary>
    /// Resolves the site by internal id and seeds it; an unknown internal
    /// id is a no-op.
    /// </summary>
    internal static async Task EnsureSeededForInternalIdAsync(IApi api, string internalId)
    {
        var site = await api.Sites.GetByInternalIdAsync(internalId);
        if (site == null)
        {
            return;
        }

        await EnsureSeededAsync(api, site.Id);
    }

    /// <summary>Seeds the given site when it has no pages at all.</summary>
    internal static async Task EnsureSeededAsync(IApi api, Guid siteId)
    {
        var pages = await api.Pages.GetAllAsync<PageInfo>(siteId);
        if (pages.Any())
        {
            return;
        }

        var page = await api.Pages.CreateAsync<SiteAHomePage>();
        page.SiteId = siteId;
        page.ParentId = null;
        page.SortOrder = 0;
        page.Title = Title;
        page.Slug = Slug;
        page.Published = DateTime.Now;
        page.IsHidden = false;
        await api.Pages.SaveAsync(page);
    }
}
