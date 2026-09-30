#nullable enable

using System.Collections.Generic;
using System.Linq;
using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 6.1 (Q2): Site A's homepage ("Trang chủ (Site A)"). Carries the
/// hero - eyebrow, heading, subtext, a primary CTA and a photo list - which
/// renders through <c>Views/Shared/_SiteAHero.cshtml</c> as a photo carousel
/// (photos present, >= 768px) or the solid-teal fallback. Story 6.2 adds
/// the featured-category grid (editor picks, else the hub's first
/// <see cref="MaxFeatured"/> tiles) and the trust-stat band. Every field is
/// optional: a blank heading falls back to the page title, a blank/unsafe
/// CTA link drops the primary button.
/// </summary>
[PageType(Title = "Trang chủ (Site A)")]
[ContentTypeRoute(Title = "Default", Route = "/siteahomepage")]
public class SiteAHomePage : Page<SiteAHomePage>
{
    /// <summary>The primary CTA's text when <see cref="HeroCtaLabel"/> is blank.</summary>
    public const string DefaultCtaLabel = "Nhận báo giá";

    [Region(Title = "Hero: dòng nhãn nhỏ (eyebrow)", Description = "Nhãn nhỏ phía trên tiêu đề hero. Không bắt buộc.")]
    public StringField HeroEyebrow { get; set; } = null!;

    [Region(Title = "Hero: tiêu đề", Description = "Tiêu đề lớn của hero. Để trống thì dùng tiêu đề trang.")]
    public StringField HeroHeading { get; set; } = null!;

    [Region(Title = "Hero: đoạn mô tả", Description = "1–2 câu ngắn dưới tiêu đề. Không bắt buộc.")]
    public TextField HeroSubtext { get; set; } = null!;

    [Region(Title = "Hero: chữ trên nút chính", Description = "Để trống thì hiện \"Nhận báo giá\".")]
    public StringField HeroCtaLabel { get; set; } = null!;

    [Region(Title = "Hero: liên kết nút chính", Description = "Trang nút chính dẫn tới, ví dụ /lien-he hoặc https://... Để trống thì không hiện nút chính (nút Gọi ngay · Zalo vẫn hiện).")]
    public StringField HeroCtaLink { get; set; } = null!;

    [Region(Title = "Hero: ảnh (carousel)", Description = "Ảnh thật về sản phẩm/công trình, khổ ngang rộng (nên 1920×800 trở lên). Nhớ điền Alt text trong thư viện Media; để trống thì dùng tiêu đề hero. Không có ảnh thì hero hiện nền xanh. Trên điện thoại luôn hiện nền xanh, không tải ảnh.")]
    public IList<ImageField> HeroPhotos { get; set; } = new List<ImageField>();

    [Region(Title = "Nhóm sản phẩm nổi bật", Description = "Chọn tối đa 6 trang danh mục để hiện trên trang chủ, theo thứ tự trong danh sách. Khi đã chọn thì CHỈ hiện các danh mục được chọn: danh mục bị ẩn hoặc chưa có sản phẩm sẽ tự bỏ qua (không tự bù), chọn quá 6 thì phần dư bị bỏ. Để trống (hoặc không còn danh mục hợp lệ nào) thì hiện 6 danh mục đầu tiên của trang Sản phẩm.")]
    public IList<PageField> FeaturedCategories { get; set; } = new List<PageField>();

    [Region(Title = "Dải số liệu uy tín", Description = "Các con số THẬT, ví dụ \"500+\" / \"Trường đã lắp đặt\". Hiện dưới lưới danh mục. Để trống thì không hiện dải này.")]
    public IList<TrustStat> TrustStats { get; set; } = new List<TrustStat>();

    /// <summary>Story 6.2: featured tiles shown on the homepage, at most.</summary>
    public const int MaxFeatured = 6;

    /// <summary>Story 6.2: the site's product hub, set by the controller; null when there is none.</summary>
    public SitemapItem? Hub { get; set; }

    /// <summary>Story 6.2: every visible tile of <see cref="Hub"/>, set by the controller.</summary>
    public IReadOnlyList<CategoryTileModel> HubTiles { get; set; } = Array.Empty<CategoryTileModel>();

    /// <summary>The tiles for the homepage grid (<see cref="SelectFeatured"/>).</summary>
    public IReadOnlyList<CategoryTileModel> FeaturedTiles =>
        SelectFeatured(HubTiles, (FeaturedCategories ?? Enumerable.Empty<PageField>())
            .Where(f => f != null && f.HasValue)
            .Select(f => f.Id!.Value));

    /// <summary>The stats with both a number and a label, in order.</summary>
    public IReadOnlyList<TrustStat> TrustStatItems =>
        (TrustStats ?? Enumerable.Empty<TrustStat>())
            .Where(s => s != null && s.NumberText != null && s.LabelText != null)
            .ToList();

    /// <summary>
    /// The picks, in pick order and without repeats, that are among
    /// <paramref name="tiles"/> (so hidden, draft or empty categories drop
    /// out), at most <see cref="MaxFeatured"/>. No valid pick -> the first
    /// <see cref="MaxFeatured"/> tiles.
    /// </summary>
    public static IReadOnlyList<CategoryTileModel> SelectFeatured(
        IReadOnlyList<CategoryTileModel> tiles, IEnumerable<Guid> picks)
    {
        var byId = tiles.GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First());
        var picked = picks
            .Distinct()
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .Take(MaxFeatured)
            .ToList();

        return picked.Count > 0 ? picked : tiles.Take(MaxFeatured).ToList();
    }

    /// <summary>The eyebrow, trimmed, or null when blank.</summary>
    public string? HeroEyebrowText => Trimmed(HeroEyebrow?.Value);

    /// <summary>The heading, trimmed, or the page title when blank.</summary>
    public string HeroHeadingText => Trimmed(HeroHeading?.Value) ?? Title;

    /// <summary>The subtext, trimmed, or null when blank.</summary>
    public string? HeroSubtextText => Trimmed(HeroSubtext?.Value);

    /// <summary>The primary CTA's text: trimmed label, or <see cref="DefaultCtaLabel"/>.</summary>
    public string HeroCtaText => Trimmed(HeroCtaLabel?.Value) ?? DefaultCtaLabel;

    /// <summary>
    /// The primary CTA's href, or null (button omitted) when blank or
    /// unsafe. Accepts an absolute http(s) URL, a site-relative path
    /// ("/lien-he", never protocol-relative "//host") or an in-page "#anchor".
    /// </summary>
    public string? HeroCtaHref => SafeLink(HeroCtaLink?.Value);

    /// <summary>Photos whose media exists, in order.</summary>
    public IReadOnlyList<ImageField> HeroPhotoItems =>
        (HeroPhotos ?? Enumerable.Empty<ImageField>())
            .Where(p => p != null && p.HasValue && p.Media != null)
            .ToList();

    /// <summary>A photo's alt: the media's AltText, else the hero heading.</summary>
    public string HeroAltText(ImageField photo) =>
        Trimmed(photo?.Media?.AltText) ?? HeroHeadingText;

    internal static string? SafeLink(string? value)
    {
        var link = Trimmed(value);
        if (link == null)
        {
            return null;
        }

        // Browsers strip TAB/CR/LF from URLs, so "/\t/evil" would become
        // protocol-relative "//evil": reject any control or whitespace char.
        if (link.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
        {
            return null;
        }

        if (SiteSettingsValidation.IsSafeAbsoluteUrl(link))
        {
            return link;
        }

        var isRelativePath = link.StartsWith('/') && !link.StartsWith("//") && !link.StartsWith("/\\");
        var isAnchor = link.StartsWith('#') && link.Length > 1;
        if ((isRelativePath || isAnchor) && link.IndexOfAny(new[] { ' ', '"', '<', '>', '\\' }) < 0)
        {
            return link;
        }

        return null;
    }

    internal static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Story 6.2: one number-over-label stat in the homepage trust band.</summary>
public class TrustStat
{
    [Field(Title = "Con số", Description = "Ví dụ \"500+\", \"20 năm\".")]
    public StringField Number { get; set; } = null!;

    [Field(Title = "Nhãn", Description = "Ví dụ \"Trường đã lắp đặt\".")]
    public StringField Label { get; set; } = null!;

    /// <summary>The number, trimmed, or null when blank.</summary>
    public string? NumberText => SiteAHomePage.Trimmed(Number?.Value);

    /// <summary>The label, trimmed, or null when blank.</summary>
    public string? LabelText => SiteAHomePage.Trimmed(Label?.Value);
}
