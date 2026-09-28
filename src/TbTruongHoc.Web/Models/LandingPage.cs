using System.Collections.Generic;
using System.Linq;
using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 3.1 (AD-2, AD-5, FR-12): the standalone paid-ads landing page - a
/// chrome-less, single-goal page (hero, prose blocks, inline quote form and
/// one fixed CTA). Story 3.2 (FR-13): its own repeatable variant+price region
/// (<see cref="Variants"/>) renders as a card grid - photo, chip, real price
/// and a per-card CTA to the inline form. Every save is forced
/// hidden from the nav (see the Pages hook in <c>Program.cs</c>). Every
/// field is optional; a blank one renders nothing.
/// </summary>
[PageType(Title = "Landing page (quảng cáo)")]
[ContentTypeRoute(Title = "Default", Route = "/landingpage")]
public class LandingPage : Page<LandingPage>
{
    /// <summary>The <c>formType</c> this page's quote form posts to <c>/api/leads</c>.</summary>
    public const string FormType = "landing";

    /// <summary>The fixed CTA's text when <see cref="CtaLabel"/> is blank.</summary>
    public const string DefaultCtaLabel = "Nhận báo giá";

    /// <summary>Small label above the title.</summary>
    [Region(Title = "Dòng nhãn nhỏ (eyebrow)")]
    public StringField Eyebrow { get; set; }

    /// <summary>The hero image under the title.</summary>
    [Region(Title = "Ảnh chính (hero)", Description = "Ảnh lớn dưới tiêu đề. Nhớ điền Alt text cho ảnh trong thư viện Media; nếu để trống, trang dùng tiêu đề làm mô tả ảnh.")]
    public ImageField HeroImage { get; set; }

    /// <summary>Short intro paragraph under the hero.</summary>
    [Region(Title = "Đoạn giới thiệu", Description = "1–3 câu ngắn dưới ảnh chính. Không bắt buộc.")]
    public TextField Intro { get; set; }

    /// <summary>The fixed bottom CTA's text.</summary>
    [Region(Title = "Nút cố định cuối màn hình", Description = "Chữ trên nút luôn hiện ở cuối màn hình, bấm vào sẽ cuộn tới form đặt hàng. Để trống thì hiện \"Nhận báo giá\". Chỉ dùng \"Đặt mua ngay\" khi trang đã hiện giá thật.")]
    public StringField CtaLabel { get; set; }

    /// <summary>Optional h2 above the variant cards (Story 3.2). Blank = no heading.</summary>
    [Region(Title = "Tiêu đề phần biến thể", Description = "Tiêu đề phía trên các thẻ biến thể, ví dụ \"Chọn mẫu thùng rượu\". Để trống thì không hiện tiêu đề.")]
    public StringField VariantsTitle { get; set; }

    /// <summary>Variant cards with their own prices (Story 3.2), shown after the intro.</summary>
    [Region(Title = "Biến thể & giá", ListTitle = "Name", Description = "Mỗi mục là một thẻ biến thể (ảnh, tên, nhãn, giá) hiện sau đoạn giới thiệu, kèm nút \"Đặt mua ngay\" dẫn tới form đặt hàng. Mục không có cả tên lẫn nhãn sẽ bị ẩn. Để trống giá thì thẻ hiện \"Liên hệ báo giá\" và nút \"Nhận báo giá\". Giá phải là giá thật do khách hàng cung cấp - không tự đặt giá.")]
    public IList<LandingVariant> Variants { get; set; } = new List<LandingVariant>();

    /// <summary>The variant section's h2, trimmed, or null when blank.</summary>
    public string VariantsTitleText => Trimmed(VariantsTitle?.Value);

    /// <summary>The variants to render, in order: those with a name or a label.</summary>
    public IReadOnlyList<LandingVariant> VisibleVariants =>
        (Variants ?? Enumerable.Empty<LandingVariant>())
            .Where(v => v != null && v.HeadingText != null)
            .ToList();

    /// <summary>The eyebrow, trimmed, or null when blank.</summary>
    public string EyebrowText => Trimmed(Eyebrow?.Value);

    /// <summary>The intro, trimmed, or null when blank.</summary>
    public string IntroText => Trimmed(Intro?.Value);

    /// <summary>The fixed CTA's text: <see cref="CtaLabel"/> trimmed, or <see cref="DefaultCtaLabel"/>.</summary>
    public string CtaText => Trimmed(CtaLabel?.Value) ?? DefaultCtaLabel;

    /// <summary>True when a hero image is set and its media exists.</summary>
    public bool HasHeroImage => HeroImage != null && HeroImage.HasValue && HeroImage.Media != null;

    /// <summary>The hero image's alt: the media's AltText, then the page title.</summary>
    public string HeroAltText =>
        HasHeroImage && !string.IsNullOrWhiteSpace(HeroImage.Media.AltText)
            ? HeroImage.Media.AltText.Trim()
            : Title;

    internal static string Trimmed(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// Story 3.1/3.2: one variant+price card on a <see cref="LandingPage"/>.
/// Every helper trims its value and returns null when blank.
/// </summary>
public class LandingVariant
{
    /// <summary>The card CTA's text when the variant has a price.</summary>
    public const string OrderCtaText = "Đặt mua ngay";

    /// <summary>The card CTA's text when the price is blank.</summary>
    public const string QuoteCtaText = LandingPage.DefaultCtaLabel;

    /// <summary>The muted line shown instead of a blank price.</summary>
    public const string ContactPriceText = "Liên hệ báo giá";

    [Field(Title = "Ảnh")]
    public ImageField Image { get; set; }

    [Field(Title = "Tên biến thể")]
    public StringField Name { get; set; }

    [Field(Title = "Nhãn (chip)")]
    public StringField Label { get; set; }

    [Field(Title = "Giá", Description = "Giá thật do khách hàng cung cấp, ví dụ \"2.500.000đ\".")]
    public StringField Price { get; set; }

    /// <summary>The name, trimmed, or null when blank.</summary>
    public string NameText => LandingPage.Trimmed(Name?.Value);

    /// <summary>The chip label, trimmed, or null when blank.</summary>
    public string LabelText => LandingPage.Trimmed(Label?.Value);

    /// <summary>The price, trimmed, or null when blank.</summary>
    public string PriceText => LandingPage.Trimmed(Price?.Value);

    /// <summary>The card title: the name, then the label; null when both are blank.</summary>
    public string HeadingText => NameText ?? LabelText;

    /// <summary>True when an image is set and its media exists.</summary>
    public bool HasImage => Image != null && Image.HasValue && Image.Media != null;

    /// <summary>The image's alt: the media's AltText, then the name, the label, the page title.</summary>
    public string AltText(string pageTitle) =>
        (HasImage ? LandingPage.Trimmed(Image.Media.AltText) : null) ?? HeadingText ?? pageTitle;

    /// <summary>The form prefill: "heading – price", or just the heading when the price is blank.</summary>
    public string PrefillText => PriceText != null ? $"{HeadingText} – {PriceText}" : HeadingText;

    /// <summary>The card CTA's text: "Đặt mua ngay" with a price, "Nhận báo giá" without.</summary>
    public string CtaText => PriceText != null ? OrderCtaText : QuoteCtaText;
}
