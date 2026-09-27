using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Services;

/// <summary>
/// Story 2.2: builds a <see cref="ProductHubPage"/>'s category tiles. The one
/// place for the hide rule: a hub child becomes a tile only when it is a
/// published, non-hidden <see cref="ProductArchive"/> with at least one
/// published post.
/// </summary>
public class ProductCatalog
{
    private readonly IApi _api;

    public ProductCatalog(IApi api)
    {
        _api = api;
    }

    public async Task<IReadOnlyList<CategoryTileModel>> GetHubTilesAsync(Guid siteId, Guid hubId)
    {
        var sitemap = await _api.Sites.GetSitemapAsync(siteId, onlyPublished: false);
        // Full sitemap so a draft preview of an unpublished hub still finds
        // it; children are filtered by Published below.
        var hub = FindById(sitemap, hubId);
        if (hub == null)
        {
            return Array.Empty<CategoryTileModel>();
        }

        var now = DateTime.Now;
        var tiles = new List<CategoryTileModel>();

        foreach (var child in hub.Items.OrderBy(i => i.SortOrder))
        {
            if (child.IsHidden || !child.Published.HasValue || child.Published.Value > now)
            {
                continue;
            }

            var page = await _api.Pages.GetByIdAsync<PageInfo>(child.Id);
            if (page == null || page.TypeId != nameof(ProductArchive))
            {
                continue;
            }

            // Published posts only - the archive query filters out drafts
            // and future-dated posts (Posts.GetCountAsync would not).
            var archive = await _api.Archives.GetByIdAsync<PostInfo>(child.Id, 1, null, null, null, null, pageSize: 1);
            if (archive == null || archive.TotalPosts <= 0)
            {
                continue;
            }

            tiles.Add(new CategoryTileModel(page.Title, page.Excerpt, child.Permalink));
        }

        return tiles;
    }

    private static SitemapItem FindById(IEnumerable<SitemapItem> items, Guid id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item;
            }

            var found = FindById(item.Items, id);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
