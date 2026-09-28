using System.Collections.Generic;
using System.Linq;
using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 2.2 (AD-2): a product category (e.g. one Trống subcategory) - an
/// archive page whose items are <see cref="ProductPost"/>s, rendered as a
/// paginated product-card grid.
/// Story 4.1 (FR-11): an optional, read-only drum configuration reference
/// (label/value rows + optional heading) rendered after the grid. No row
/// valid = nothing rendered.
/// </summary>
[PageType(Title = "Danh mục sản phẩm", IsArchive = true)]
[ContentTypeRoute(Title = "Default", Route = "/productarchive")]
[PageTypeArchiveItem(typeof(ProductPost))]
public class ProductArchive : Page<ProductArchive>
{
    /// <summary>Cards per archive page.</summary>
    public const int PageSize = 12;

    /// <summary>The config reference's h2 when the editor leaves it blank.</summary>
    public const string DefaultConfigTitle = "Bảng tham khảo cấu hình trống";

    /// <summary>
    /// The currently loaded page of products.
    /// </summary>
    public PostArchive<ProductPost> Archive { get; set; }

    /// <summary>
    /// Story 4.1: the config reference's heading. A single-field region -
    /// Piranha collapses a one-field region class into the bare field, so it
    /// is its own region, separate from <see cref="DrumConfig"/>.
    /// </summary>
    [Region(Title = "Tiêu đề bảng cấu hình trống", Description = "Chỉ dùng cho các trang Trống. Để trống thì hiện \"Bảng tham khảo cấu hình trống\". Tiêu đề chỉ hiện khi bảng bên dưới có ít nhất một dòng hợp lệ.")]
    public StringField DrumConfigTitle { get; set; }

    /// <summary>Story 4.1: one row per option dimension (size, loại, bánh xe, …).</summary>
    [Region(Title = "Bảng tham khảo cấu hình trống", ListTitle = "Label", Description = "Chỉ dùng cho các trang Trống. Mỗi dòng là một hạng mục (ví dụ \"Kích thước\", \"Loại\", \"Bánh xe\", \"Sơn\", \"Vẽ mặt trống\") và các lựa chọn xưởng đang nhận làm. Cần có cả Hạng mục và Lựa chọn mới được hiển thị; dòng thiếu một trong hai sẽ bị ẩn. Đây là bảng tham khảo, không ghi giá - dưới bảng luôn có dòng \"Bảng tham khảo, không tính giá tự động\" và form nhận tư vấn. Để trống cả bảng thì trang không hiện phần này.")]
    public IList<DrumConfigRow> DrumConfig { get; set; } = new List<DrumConfigRow>();

    /// <summary>The config heading, trimmed, or <see cref="DefaultConfigTitle"/> when blank.</summary>
    public string ConfigTitleText =>
        string.IsNullOrWhiteSpace(DrumConfigTitle?.Value) ? DefaultConfigTitle : DrumConfigTitle.Value.Trim();

    /// <summary>Config rows whose label and value are both non-blank, trimmed, in editor order.</summary>
    public IReadOnlyList<(string Label, string Value)> ConfigRows =>
        (DrumConfig ?? Enumerable.Empty<DrumConfigRow>())
            .Select(r => (Label: Trimmed(r?.Label), Value: Trimmed(r?.Value)))
            .Where(r => r.Label != null && r.Value != null)
            .ToList();

    private static string Trimmed(StringField field) =>
        string.IsNullOrWhiteSpace(field?.Value) ? null : field.Value.Trim();
}

/// <summary>Story 4.1: one drum config dimension (e.g. "Bánh xe" / "Có, không").</summary>
public class DrumConfigRow
{
    [Field(Title = "Hạng mục")]
    public StringField Label { get; set; }

    [Field(Title = "Lựa chọn")]
    public StringField Value { get; set; }
}
