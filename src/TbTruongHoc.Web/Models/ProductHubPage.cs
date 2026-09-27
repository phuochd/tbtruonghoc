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
}

/// <summary>One category-tile on a <see cref="ProductHubPage"/>.</summary>
public sealed record CategoryTileModel(string Title, string Excerpt, string Permalink);

/// <summary>
/// Story 2.2: input for <c>Views/Shared/_CategoryTile.cshtml</c> - the tile
/// plus its eyebrow (the hub's title).
/// </summary>
public sealed record CategoryTileViewModel(CategoryTileModel Tile, string Eyebrow);
