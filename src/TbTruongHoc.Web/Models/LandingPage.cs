using System.Collections.Generic;
using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 3.1 (AD-2, AD-5, FR-12): the standalone paid-ads landing page - a
/// chrome-less, single-goal page (hero, prose blocks, inline quote form and
/// one fixed CTA). It has its own repeatable variant+price region
/// (<see cref="Variants"/>), rendered by Story 3.2. Every save is forced
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

    /// <summary>Variant cards with their own prices (rendered by Story 3.2).</summary>
    [Region(Title = "Biến thể & giá", ListTitle = "Name", Description = "Mỗi mục là một thẻ biến thể (ảnh, tên, nhãn, giá). Hiện các thẻ này chưa hiển thị trên trang. Giá phải là giá thật do khách hàng cung cấp - không tự đặt giá.")]
    public IList<LandingVariant> Variants { get; set; } = new List<LandingVariant>();

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

    private static string Trimmed(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Story 3.1: one variant+price card on a <see cref="LandingPage"/> (rendered by Story 3.2).</summary>
public class LandingVariant
{
    [Field(Title = "Ảnh")]
    public ImageField Image { get; set; }

    [Field(Title = "Tên biến thể")]
    public StringField Name { get; set; }

    [Field(Title = "Nhãn (chip)")]
    public StringField Label { get; set; }

    [Field(Title = "Giá", Description = "Giá thật do khách hàng cung cấp, ví dụ \"2.500.000đ\".")]
    public StringField Price { get; set; }
}
