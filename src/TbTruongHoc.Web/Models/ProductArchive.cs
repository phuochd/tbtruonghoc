using Piranha.AttributeBuilder;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 2.2 (AD-2): a product category (e.g. one Trống subcategory) - an
/// archive page whose items are <see cref="ProductPost"/>s, rendered as a
/// paginated product-card grid.
/// Story 4.2 (FR-11): an editor can add a <see cref="ConfigBlock"/> among the
/// page's blocks; on an archive it brings its own quote form (4.1's
/// DrumConfig regions were replaced by that block).
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
