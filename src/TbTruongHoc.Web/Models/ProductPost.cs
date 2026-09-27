using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 2.2 (AD-2, AD-5): one product model/variant, a child of a
/// <see cref="ProductArchive"/>. Carries an optional free-form price; a blank
/// price renders as "Liên hệ báo giá" wherever the product is shown.
/// </summary>
[PostType(Title = "Sản phẩm")]
[ContentTypeRoute(Title = "Default", Route = "/productpost")]
public class ProductPost : Post<ProductPost>
{
    /// <summary>
    /// AD-5 price: free text (exact, range or "từ X"), no format validation,
    /// rendered HTML-encoded. A single-field region - Piranha collapses a
    /// one-field region class into the bare field, so it is declared as the
    /// field itself.
    /// </summary>
    [Region(Title = "Giá (để trống = Liên hệ báo giá)")]
    public StringField Price { get; set; }

    /// <summary>
    /// The editor-typed price, trimmed, or null when blank.
    /// </summary>
    public string PriceText => string.IsNullOrWhiteSpace(Price?.Value) ? null : Price.Value.Trim();
}
