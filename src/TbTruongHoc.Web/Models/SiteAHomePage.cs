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
/// (photos present, >= 768px) or the solid-teal fallback. Story 6.2 extends
/// this same type with the tile grid and trust band. Every field is
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
