using System.Reflection;
using Piranha.AttributeBuilder;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 2.2 (AD-2): a standalone hub page (e.g. "Trống") whose category-tile
/// grid is built at render time from its <see cref="ProductArchive"/>
/// children in the sitemap - never a hand-curated link list.
/// </summary>
[PageType(Title = "Trang tổng danh mục")]
[ContentTypeRoute(Title = "Default", Route = "/producthub")]
public class ProductHubPage : Page<ProductHubPage>
{
    /// <summary>
    /// The non-empty, visible subcategories, in sitemap order.
    /// </summary>
    public IReadOnlyList<CategoryTileModel> Tiles { get; set; } = Array.Empty<CategoryTileModel>();

    /// <summary>
    /// Story 6.1: this type's [PageType] title. Piranha's
    /// <see cref="SitemapItem.PageTypeName"/> carries the page type's
    /// <em>title</em> (not its id), so the nav can spot a hub from the
    /// sitemap alone, without loading each page.
    /// </summary>
    public static readonly string PageTypeTitle =
        typeof(ProductHubPage).GetCustomAttribute<PageTypeAttribute>()!.Title;

    /// <summary>True when the sitemap item is a <see cref="ProductHubPage"/>.</summary>
    public static bool IsHub(SitemapItem item) => item?.PageTypeName == PageTypeTitle;
}

/// <summary>One category-tile on a <see cref="ProductHubPage"/>.</summary>
public sealed record CategoryTileModel(string Title, string Excerpt, string Permalink);

/// <summary>
/// Story 2.2: input for <c>Views/Shared/_CategoryTile.cshtml</c> - the tile
/// plus its eyebrow (the hub's title).
/// </summary>
public sealed record CategoryTileViewModel(CategoryTileModel Tile, string Eyebrow);
