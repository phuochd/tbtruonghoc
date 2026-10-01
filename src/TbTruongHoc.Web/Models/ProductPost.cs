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
/// Story 6.4: three Site A-only fields (installation flag, genuine project
/// photos, trust claims) plus the controller-set category/hub that Site A's
/// PDP (SiteAProductPost.cshtml) needs. Site B ignores all of them.
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
    /// Story 6.4 (Q1): the installation flag lives on the product. Off (the
    /// default) = shippable: the Site A PDP shows the shipping note. On =
    /// installation-required (Story 6.5 adds that variant's blocks).
    /// </summary>
    [Region(Title = "Cần thi công (Site A)", Description = "Chỉ dùng cho Site A: đánh dấu nếu sản phẩm cần khảo sát/thi công tại chỗ. Để trống = sản phẩm giao hàng (hiện ghi chú \"Giao hàng toàn quốc\"). Site B không dùng trường này.")]
    public CheckBoxField RequiresInstallation { get; set; }

    /// <summary>
    /// Story 6.4 (Q3): genuine project photos. They join the Site A gallery
    /// after the regular photos and carry the "Hình ảnh thi công thực tế" badge.
    /// </summary>
    [Region(Title = "Ảnh thi công thực tế (Site A)", Description = "Chỉ dùng cho Site A: CHỈ ảnh chụp thật tại công trình đã thi công (không dùng ảnh mẫu, ảnh minh họa hay ảnh AI). Các ảnh này hiện sau ảnh sản phẩm và mang nhãn \"Hình ảnh thi công thực tế\". Site B không dùng trường này.")]
    public IList<ImageField> GenuinePhotos { get; set; } = new List<ImageField>();

    /// <summary>
    /// Story 6.4 (Q4): trust claims for this product, comma-separated. A
    /// non-blank value replaces the category's "Cam kết (Site A)".
    /// </summary>
    [Region(Title = "Cam kết (Site A)", Description = "Chỉ dùng cho Site A: các cam kết THẬT của sản phẩm, cách nhau bằng dấu phẩy, ví dụ \"Bảo hành 12 tháng, Hàng chính hãng\" (hiện tối đa 3). Để trống = dùng cam kết của danh mục. Site B không dùng trường này.")]
    public StringField TrustClaims { get; set; }

    /// <summary>Trust chips shown on the Site A PDP, at most.</summary>
    public const int MaxTrustChips = 3;

    /// <summary>
    /// Story 6.4 (Site A only): the product's category (its archive page).
    /// Set by the controller; not a region.
    /// </summary>
    public ProductArchive ParentCategory { get; set; }

    /// <summary>
    /// Story 6.4 (Site A only): the breadcrumb's hub step - the category's
    /// published parent <see cref="ProductHubPage"/>, or null. Set by the
    /// controller; not a region.
    /// </summary>
    public PageInfo ParentHub { get; set; }

    /// <summary>Story 6.4: true when "Cần thi công (Site A)" is ticked.</summary>
    public bool IsInstallationRequired => RequiresInstallation?.Value == true;

    /// <summary>The shipping claim that must never show on an installation-required product.</summary>
    public const string ShippingClaim = "Giao hàng toàn quốc";

    /// <summary>
    /// Story 6.4 (Q4): the product's own trust claims when it has any, else
    /// its category's, parsed with <see cref="ProductArchive.SplitList"/> and
    /// cut to <see cref="MaxTrustChips"/>. On an installation-required
    /// product a claim mentioning <see cref="ShippingClaim"/> (e.g. a
    /// category-wide one) is dropped first: that claim is only true for
    /// shippable products.
    /// </summary>
    public IReadOnlyList<string> TrustChipList
    {
        get
        {
            var own = ProductArchive.SplitList(TrustClaims?.Value);
            var list = own.Count > 0 ? own : ProductArchive.SplitList(ParentCategory?.TrustClaims?.Value);
            var installation = IsInstallationRequired;
            return list
                .Where(c => !installation || !ProductHubPage.GroupKey(c).Contains(ProductHubPage.GroupKey(ShippingClaim), StringComparison.Ordinal))
                .Take(MaxTrustChips)
                .ToList();
        }
    }

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

    /// <summary>
    /// Story 6.4 (Q3): the Site A gallery - <see cref="GalleryImages"/>, then
    /// the non-empty <see cref="GenuinePhotos"/> whose media is not already
    /// shown. Only the latter are flagged <c>Genuine</c>.
    /// </summary>
    public IReadOnlyList<(ImageField Image, bool Genuine)> SiteAGalleryImages
    {
        get
        {
            var images = GalleryImages.Select(i => (Image: i, Genuine: false)).ToList();
            foreach (var photo in (GenuinePhotos ?? Enumerable.Empty<ImageField>()).Where(HasImage))
            {
                if (!images.Any(i => i.Image.Id == photo.Id))
                {
                    images.Add((photo, true));
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
