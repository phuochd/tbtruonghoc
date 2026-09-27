using Piranha.AttributeBuilder;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 2.2 (AD-2): a product category (e.g. one Trống subcategory) - an
/// archive page whose items are <see cref="ProductPost"/>s, rendered as a
/// paginated product-card grid.
/// </summary>
[PageType(Title = "Danh mục sản phẩm", IsArchive = true)]
[ContentTypeRoute(Title = "Default", Route = "/productarchive")]
[PageTypeArchiveItem(typeof(ProductPost))]
public class ProductArchive : Page<ProductArchive>
{
    /// <summary>Cards per archive page.</summary>
    public const int PageSize = 12;

    /// <summary>
    /// The currently loaded page of products.
    /// </summary>
    public PostArchive<ProductPost> Archive { get; set; }
}
