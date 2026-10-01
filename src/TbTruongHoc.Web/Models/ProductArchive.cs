using System.Text;
using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 2.2 (AD-2): a product category (e.g. one Trống subcategory) - an
/// archive page whose items are <see cref="ProductPost"/>s, rendered as a
/// paginated product-card grid.
/// Story 4.2 (FR-11): an editor can add a <see cref="ConfigBlock"/> among the
/// page's blocks; on an archive it brings its own quote form (4.1's
/// DrumConfig regions were replaced by that block).
/// Story 6.2: two optional, Site A-only fields feed the aggregate
/// "Danh mục sản phẩm" page - filter-chip groups and certification pills.
/// </summary>
[PageType(Title = "Danh mục sản phẩm", IsArchive = true)]
[ContentTypeRoute(Title = "Default", Route = "/productarchive")]
[PageTypeArchiveItem(typeof(ProductPost))]
public class ProductArchive : Page<ProductArchive>
{
    /// <summary>Cards per archive page.</summary>
    public const int PageSize = 12;

    /// <summary>Certification pills shown per aggregate tile, at most.</summary>
    public const int MaxCertifications = 2;

    [Region(Title = "Nhóm lọc (Site A)", Description = "Chỉ dùng cho Site A: các nhóm lọc trên trang tổng hợp Danh mục sản phẩm, cách nhau bằng dấu phẩy, ví dụ \"Mầm non, Ngoài trời\". Một danh mục có thể thuộc nhiều nhóm. Site B không dùng trường này.")]
    public StringField FilterGroups { get; set; }

    [Region(Title = "Chứng nhận (Site A)", Description = "Chỉ dùng cho Site A: chứng nhận THẬT đang có hồ sơ, cách nhau bằng dấu phẩy, ví dụ \"ASTM, CARB P2\" (hiện tối đa 2). Để trống nếu chưa có. Site B không dùng trường này.")]
    public StringField Certifications { get; set; }

    /// <summary>
    /// Story 6.4 (Q4): default trust claims for this category's Site A
    /// product pages; a product's own non-blank value replaces them.
    /// </summary>
    [Region(Title = "Cam kết (Site A)", Description = "Chỉ dùng cho Site A: các cam kết THẬT áp dụng cho mọi sản phẩm trong danh mục, cách nhau bằng dấu phẩy, ví dụ \"Bảo hành 12 tháng, Hàng chính hãng\" (hiện tối đa 3 trên trang sản phẩm; \"Giao hàng toàn quốc\" tự ẩn trên sản phẩm cần thi công). Sản phẩm có cam kết riêng sẽ dùng cam kết riêng. Site B không dùng trường này.")]
    public StringField TrustClaims { get; set; }

    /// <summary>
    /// The currently loaded page of products.
    /// </summary>
    public PostArchive<ProductPost> Archive { get; set; }

    /// <summary>
    /// Story 6.3 (Site A only): the breadcrumb's hub step - the published
    /// parent <see cref="ProductHubPage"/>, or null for a top-level category.
    /// Set by the controller; not a region.
    /// </summary>
    public PageInfo ParentHub { get; set; }

    /// <summary>The filter groups, parsed with <see cref="SplitList"/>.</summary>
    public IReadOnlyList<string> FilterGroupList => SplitList(FilterGroups?.Value);

    /// <summary>The first <see cref="MaxCertifications"/> certifications.</summary>
    public IReadOnlyList<string> CertificationList =>
        SplitList(Certifications?.Value).Take(MaxCertifications).ToList();

    /// <summary>
    /// A comma-separated editor list: NFC-normalized, entries trimmed, blanks
    /// dropped, duplicates by <see cref="ProductHubPage.GroupKey"/> dropped
    /// (first spelling wins), in order.
    /// </summary>
    public static IReadOnlyList<string> SplitList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        // NFC first: the same Vietnamese word typed/pasted in NFD (macOS,
        // some IMEs) must not become a second, different entry.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return value.Normalize(NormalizationForm.FormC).Split(',')
            .Select(v => v.Trim())
            .Where(v => v.Length > 0 && seen.Add(ProductHubPage.GroupKey(v)))
            .ToList();
    }
}
