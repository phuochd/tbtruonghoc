using System.Collections.Generic;
using System.Linq;
using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 2.2 (AD-2, AD-5): one product model/variant, a child of a
/// <see cref="ProductArchive"/>. Carries an optional free-form price; a blank
/// price renders as "Liên hệ báo giá" wherever the product is shown.
/// Story 2.4: adds the product-detail-page fields - SKU, extra photos and
/// label/value spec rows. Every field is optional; a blank one renders nothing.
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

    /// <summary>Story 2.4: model code shown as "Mã: …" under the title.</summary>
    [Region(Title = "Mã sản phẩm")]
    public StringField Sku { get; set; }

    /// <summary>
    /// Story 2.4: gallery photos shown after <see cref="PostBase.PrimaryImage"/>.
    /// </summary>
    [Region(Title = "Ảnh sản phẩm", Description = "Ảnh hiển thị trên thẻ sản phẩm và khi chia sẻ là \"Ảnh chính\" (Primary image) của bài. Các ảnh ở đây hiện sau ảnh chính trong bộ ảnh của trang sản phẩm.")]
    public IList<ImageField> Photos { get; set; } = new List<ImageField>();

    /// <summary>Story 2.4: label/value spec rows.</summary>
    [Region(Title = "Thông số", ListTitle = "Label", Description = "Mỗi dòng cần có cả Nhãn và Giá trị mới được hiển thị; dòng thiếu một trong hai sẽ bị ẩn.")]
    public IList<ProductSpecRow> Specs { get; set; } = new List<ProductSpecRow>();

    /// <summary>
    /// The editor-typed price, trimmed, or null when blank.
    /// </summary>
    public string PriceText => Trimmed(Price);

    /// <summary>The SKU, trimmed, or null when blank.</summary>
    public string SkuText => Trimmed(Sku);

    /// <summary>
    /// Gallery photos in display order: the primary image (if set), then the
    /// non-empty <see cref="Photos"/>. Items whose media is missing, or whose
    /// media is already in the list, are skipped.
    /// </summary>
    public IReadOnlyList<ImageField> GalleryImages
    {
        get
        {
            var images = new List<ImageField>();
            if (HasImage(PrimaryImage))
            {
                images.Add(PrimaryImage);
            }
            // A photo already shown (same media as the primary image or an
            // earlier photo) is skipped, so no photo renders twice.
            foreach (var photo in (Photos ?? Enumerable.Empty<ImageField>()).Where(HasImage))
            {
                if (!images.Any(i => i.Id == photo.Id))
                {
                    images.Add(photo);
                }
            }
            return images;
        }
    }

    /// <summary>Spec rows whose label and value are both non-blank, trimmed.</summary>
    public IReadOnlyList<(string Label, string Value)> SpecRows =>
        (Specs ?? Enumerable.Empty<ProductSpecRow>())
            .Select(r => (Label: Trimmed(r?.Label), Value: Trimmed(r?.Value)))
            .Where(r => r.Label != null && r.Value != null)
            .ToList();

    private static bool HasImage(ImageField field) => field != null && field.HasValue && field.Media != null;

    private static string Trimmed(StringField field) =>
        string.IsNullOrWhiteSpace(field?.Value) ? null : field.Value.Trim();
}

/// <summary>Story 2.4: one product spec row (e.g. "Đường kính" / "160 cm").</summary>
public class ProductSpecRow
{
    [Field(Title = "Nhãn")]
    public StringField Label { get; set; }

    [Field(Title = "Giá trị")]
    public StringField Value { get; set; }
}
