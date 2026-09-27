#nullable enable

using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// Story 2.3: idempotent startup seed for Site B's two other product lines -
/// "Thùng rượu gỗ" (<c>thung-ruou-go</c>) and "Bồn tắm gỗ" (<c>bon-tam-go</c>),
/// both top-level <see cref="ProductArchive"/> pages, title + slug only.
///
/// Both pages are seeded as drafts (<c>Published = null</c>): the editor
/// publishes each one once it has real products, which is how the "hide an
/// empty category" rule is carried out (Bồn tắm stays unpublished until the
/// client has content).
///
/// Works per slug: a page is created only when Site B has no page with that
/// slug, and existing pages are never modified. Deleting a seeded page
/// re-seeds it on the next start. The 3 draft Thùng rượu variant posts are
/// created only in the run that creates the Thùng rượu page, so an editor's
/// edited or deleted variants stay that way while the page exists; they come
/// back (all 3, as fresh drafts) only together with a re-created Thùng rượu
/// page after the whole page is deleted.
/// </summary>
public static class ProductLineSeed
{
    internal const string ThungRuouSlug = "thung-ruou-go";
    internal const string ThungRuouTitle = "Thùng rượu gỗ";
    internal const string BonTamSlug = "bon-tam-go";
    internal const string BonTamTitle = "Bồn tắm gỗ";

    /// <summary>
    /// The confirmed gỗ sồi variants, created in this order.
    /// </summary>
    internal static readonly IReadOnlyList<string> ThungRuouVariants = new[]
    {
        "Thùng rượu gỗ sồi – ngựa kéo",
        "Thùng rượu gỗ sồi – 1 ngựa",
        "Thùng rượu gỗ sồi – 2 ngựa",
    };

    /// <summary>Piranha requires a category on every post.</summary>
    internal const string VariantCategory = "General";

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
    /// Seeds the given site, per slug. New pages are appended after the
    /// existing top-level pages.
    /// </summary>
    internal static async Task EnsureSeededAsync(IApi api, Guid siteId)
    {
        var needsThungRuou = await api.Pages.GetBySlugAsync<PageInfo>(ThungRuouSlug, siteId) == null;
        var needsBonTam = await api.Pages.GetBySlugAsync<PageInfo>(BonTamSlug, siteId) == null;
        if (!needsThungRuou && !needsBonTam)
        {
            return;
        }

        var sitemap = await api.Sites.GetSitemapAsync(siteId, onlyPublished: false);
        var sortOrder = sitemap.Count;

        if (needsThungRuou)
        {
            var archive = await CreateDraftArchiveAsync(api, siteId, sortOrder++, ThungRuouTitle, ThungRuouSlug);

            foreach (var title in ThungRuouVariants)
            {
                var post = await api.Posts.CreateAsync<ProductPost>();
                post.BlogId = archive.Id;
                post.Category = VariantCategory;
                post.Title = title;
                post.Published = null;
                await api.Posts.SaveAsync(post);
            }
        }

        if (needsBonTam)
        {
            await CreateDraftArchiveAsync(api, siteId, sortOrder, BonTamTitle, BonTamSlug);
        }
    }

    private static async Task<ProductArchive> CreateDraftArchiveAsync(IApi api, Guid siteId, int sortOrder, string title, string slug)
    {
        var archive = await api.Pages.CreateAsync<ProductArchive>();
        archive.SiteId = siteId;
        archive.ParentId = null;
        archive.SortOrder = sortOrder;
        archive.Title = title;
        archive.Slug = slug;
        archive.Published = null;
        await api.Pages.SaveAsync(archive);
        return archive;
    }
}
