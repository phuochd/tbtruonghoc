using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using Piranha.Extend.Blocks;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using TbTruongHoc.Web.Notifications;
using Xunit;
using static TbTruongHoc.Web.Tests.ProductCatalogTests;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Stories 3.1/3.2: the paid-ads landing page type (<c>Models/LandingPage.cs</c>,
/// <c>Views/Cms/LandingPage.cshtml</c>) - one test per I/O &amp; Edge-Case
/// Matrix row (3.2: variant cards, prices and card CTAs), the nav-hiding save hook, the seed, the <c>landing</c> lead
/// type, and the page-wide acceptance checks on every render. Real HTTP
/// render against the MariaDB-backed app. Each test builds its own
/// throwaway pages/media and deletes them afterwards; Site B's contact
/// settings are restored when a test changes them.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class LandingPageTests
{
    private const int NonStartPageSortOrder = 1;

    // 1x1 transparent PNG.
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private readonly PiranhaWebApplicationFactory _factory;

    public LandingPageTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Matrix: Published landing page.
    [Fact]
    public async Task Published_Landing_Page_Is_Chrome_Less_With_Form_And_Cta_And_Absent_From_Nav()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var suffix = Guid.NewGuid().ToString("N");
            var digits = Random.Shared.Next(1000, 9999).ToString();
            await f.SetContactAsync($"090 777 0000 {digits}", $"https://zalo.me/lp-{suffix}");
            var landing = await f.LandingAsync("Published Landing", isHidden: false, eyebrow: "Quà Tết 2027",
                block: "<p>Landing block marker</p>");
            var other = await f.StandardPageAsync("Nav Probe");

            // Hook: saved with IsHidden = false, reads back hidden.
            Assert.True(landing.IsHidden);

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");

            Assert.DoesNotContain("sb-nav", html);
            Assert.DoesNotContain("sb-footer", html);
            Assert.DoesNotContain("sb-contact-bar", html);
            Assert.DoesNotContain("sb-has-bar", BodyTag(html));
            Assert.Contains("<meta name=\"robots\"", html);

            // Brand mark is plain text, never a link.
            Assert.Contains("<p class=\"sb-lp__brand\">Trống Đọi Tam</p>", Decode(main));

            var cta = Regex.Match(html, "<a class=\"sb-lp__cta\"[^>]*>").Value;
            Assert.Equal("#dat-hang", AttrOf(cta, "href"));
            Assert.Single(Regex.Matches(html, "class=\"sb-lp__cta\""));

            var form = Section(main, "<section class=\"sb-lp__form\" id=\"dat-hang\"", "</section>");
            Assert.Contains("data-quote-request-form", form);
            Assert.Contains("<input type=\"hidden\" name=\"formType\" value=\"landing\">", form);
            Assert.Equal(landing.Title, Decode(AttrOf(Regex.Match(form, "<input type=\"text\" id=\"quoteRequestProduct\"[^>]*>").Value, "value")!));
            Assert.Contains("lead-form.js", html);

            // Order: brand, eyebrow, h1, blocks, form; the CTA after main.
            var markers = new[] { "sb-lp__brand", "sb-lp__eyebrow", "sb-lp__title", "Landing block marker", "id=\"dat-hang\"" };
            var positions = markers.Select(m => main.IndexOf(m, StringComparison.Ordinal)).ToList();
            Assert.All(positions, p => Assert.True(p >= 0));
            Assert.Equal(positions.OrderBy(p => p), positions);
            Assert.True(html.IndexOf("</main>", StringComparison.Ordinal) < html.IndexOf("class=\"sb-lp__cta\"", StringComparison.Ordinal));

            // Alternatives line: tel + Zalo, each with an accessible name.
            var alt = Section(form, "<p class=\"sb-lp__alt\">", "</p>");
            var links = Regex.Matches(alt, "<a [^>]*>").Select(m => m.Value).ToList();
            Assert.Equal(2, links.Count);
            Assert.Equal($"tel:0907770000{digits}", AttrOf(links[0], "href"));
            Assert.False(string.IsNullOrWhiteSpace(AttrOf(links[0], "aria-label")));
            Assert.Equal($"https://zalo.me/lp-{suffix}", AttrOf(links[1], "href"));
            Assert.Equal("_blank", AttrOf(links[1], "target"));
            Assert.Equal("noopener noreferrer", AttrOf(links[1], "rel"));
            Assert.False(string.IsNullOrWhiteSpace(AttrOf(links[1], "aria-label")));
            AssertPageWideRules(html);

            // Absent from the nav on other Site B pages.
            var otherHtml = await GetHtmlAsync(other.Permalink, HostnameOf(siteB));
            Assert.Contains("<header class=\"sb-nav\"", otherHtml);
            Assert.DoesNotContain($"href=\"{landing.Permalink}\"", otherHtml);
            Assert.DoesNotContain(landing.Title, Decode(otherHtml));
        });
    }

    // Hook: Manager-style re-save with IsHidden = false.
    [Fact]
    public async Task Save_Hook_Forces_Landing_Pages_Hidden_But_Leaves_Other_Types_Alone()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var landing = await f.LandingAsync("Hook Landing", isHidden: false);
            Assert.True(landing.IsHidden);

            await f.UpdateLandingAsync(landing.Id, p => p.IsHidden = false);
            using (var scope = _factory.Services.CreateScope())
            {
                var fresh = scope.ServiceProvider.GetRequiredService<IApi>();
                Assert.True((await fresh.Pages.GetByIdAsync<PageInfo>(landing.Id))!.IsHidden);
                var item = (await fresh.Sites.GetSitemapAsync(siteB.Id, onlyPublished: false)).Single(i => i.Id == landing.Id);
                Assert.True(item.IsHidden);
            }

            // Still resolves at its permalink.
            await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));

            var standard = await f.StandardPageAsync("Hook Standard");
            Assert.False(standard.IsHidden);
        });
    }

    // Matrix: Bare page.
    [Fact]
    public async Task Bare_Landing_Page_Renders_Brand_Title_Form_And_Default_Cta_Only()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var landing = await f.LandingAsync("Bare Landing");

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");

            Assert.Contains($"<title>{landing.Title}</title>", Decode(html));
            Assert.Contains("sb-lp__brand", main);
            Assert.Contains($"<h1 class=\"sb-lp__title\">{landing.Title}</h1>", Decode(main));
            Assert.Contains("data-quote-request-form", main);
            Assert.DoesNotContain("sb-lp__eyebrow", main);
            Assert.DoesNotContain("<img", main);
            Assert.DoesNotContain("sb-lp__intro", main);
            Assert.Contains(">Nhận báo giá</a>", Decode(html));
            AssertPageWideRules(html);
        });
    }

    // Matrix: Hero + intro.
    [Fact]
    public async Task Hero_Image_Falls_Back_To_Title_Alt_And_Intro_Is_Encoded()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var image = await f.UploadAsync();
            var landing = await f.LandingAsync("Hero Landing");
            await f.UpdateLandingAsync(landing.Id, p =>
            {
                p.HeroImage = image;
                p.Intro = "Thùng gỗ sồi <b>thủ công</b> & quà Tết";
            });

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");

            var imgs = Imgs(main);
            Assert.Single(imgs);
            Assert.Contains(image.ToString(), AttrOf(imgs[0], "src")!);
            Assert.Equal(landing.Title, Decode(AttrOf(imgs[0], "alt")!));

            var intro = Section(main, "<p class=\"sb-lp__intro\">", "</p>");
            Assert.Contains("&lt;b&gt;", intro);
            Assert.DoesNotContain("<b>", intro);
            Assert.Contains("Thùng gỗ sồi <b>thủ công</b> & quà Tết", Decode(intro));

            // Title, hero, intro in order.
            Assert.True(main.IndexOf("sb-lp__title", StringComparison.Ordinal) < main.IndexOf("sb-lp__hero", StringComparison.Ordinal));
            Assert.True(main.IndexOf("sb-lp__hero", StringComparison.Ordinal) < main.IndexOf("sb-lp__intro", StringComparison.Ordinal));
            AssertPageWideRules(html);

            // Media AltText wins over the title.
            await f.SetAltTextAsync(image, "Thùng rượu gỗ sồi 2 ngựa");
            main = Section(await GetHtmlAsync(landing.Permalink, HostnameOf(siteB)), "<main", "</main>");
            Assert.Equal("Thùng rượu gỗ sồi 2 ngựa", Decode(AttrOf(Imgs(main)[0], "alt")!));
        });
    }

    // Story 3.2 matrix: Variants filled.
    [Fact]
    public async Task Filled_Variants_Render_Cards_In_Order_With_Chip_Price_And_Cta()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var image = await f.UploadAsync();
            var landing = await f.LandingAsync("Variant Landing", block: "<p>Variant block marker</p>");
            await f.UpdateLandingAsync(landing.Id, p =>
            {
                p.Intro = "Variant intro marker";
                p.Variants.Add(new LandingVariant { Image = image, Name = "  VariantNameOne ", Label = "ChipOne", Price = " 1.234.567đ " });
                p.Variants.Add(new LandingVariant { Name = "VariantNameTwo", Label = "ChipTwo", Price = "7.654.321đ" });
            });

            // Stored as structure.
            using (var scope = _factory.Services.CreateScope())
            {
                var fresh = scope.ServiceProvider.GetRequiredService<IApi>();
                var saved = (await fresh.Pages.GetByIdAsync<LandingPage>(landing.Id))!;
                Assert.Equal(2, saved.Variants.Count);
                Assert.Equal("1.234.567đ", saved.Variants[0].PriceText);
            }

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            var cards = VariantCards(main);
            Assert.Equal(2, cards.Count);

            var expected = new[]
            {
                (Name: "VariantNameOne", Chip: "ChipOne", Price: "1.234.567đ"),
                (Name: "VariantNameTwo", Chip: "ChipTwo", Price: "7.654.321đ")
            };
            for (var i = 0; i < cards.Count; i++)
            {
                var card = Decode(cards[i]);
                Assert.Contains($"<p class=\"sb-lp__chip\">{expected[i].Chip}</p>", card);
                Assert.Contains($"<h2 class=\"sb-card__title\">{expected[i].Name}</h2>", card);
                Assert.Contains($"<p class=\"sb-card__price\">{expected[i].Price}</p>", card);
                Assert.DoesNotContain("sb-card__price--contact", card);
                var cta = VariantCta(cards[i]);
                Assert.Equal("#dat-hang", AttrOf(cta.Tag, "href"));
                Assert.Equal("Đặt mua ngay", cta.Text);
                Assert.Equal($"{expected[i].Name} – {expected[i].Price}", cta.Prefill);
                Assert.Equal($"Đặt mua ngay {expected[i].Name} – đến form đặt hàng", cta.AriaLabel);
                // The card itself is not a link: the CTA is its only anchor.
                Assert.Single(Regex.Matches(cards[i], "<a\\b"));
                // Chip, title, price, CTA in that order.
                var order = new[] { "sb-lp__chip", "sb-card__title", "sb-card__price", "sb-lp__variant-cta" }
                    .Select(m => card.IndexOf(m, StringComparison.Ordinal)).ToList();
                Assert.All(order, p => Assert.True(p >= 0));
                Assert.Equal(order.OrderBy(p => p), order);
            }

            // Image: lazy, srcset, alt falls back to the variant name.
            var img = Assert.Single(Imgs(cards[0]));
            Assert.Contains(image.ToString(), AttrOf(img, "src")!);
            Assert.Contains("480w", AttrOf(img, "srcset")!);
            Assert.Contains("768w", AttrOf(img, "srcset")!);
            Assert.Equal("lazy", AttrOf(img, "loading"));
            Assert.Equal("VariantNameOne", Decode(AttrOf(img, "alt")!));
            Assert.Empty(Imgs(cards[1]));

            // Product grid, after the intro and before the content blocks.
            Assert.Contains("<ul class=\"sb-grid sb-grid--products\">", main);
            var intro = main.IndexOf("Variant intro marker", StringComparison.Ordinal);
            var section = main.IndexOf("class=\"sb-lp__variants\"", StringComparison.Ordinal);
            var block = main.IndexOf("Variant block marker", StringComparison.Ordinal);
            Assert.True(intro >= 0 && intro < section && section < block);

            // The fixed CTA, form type and form prefill stay as in 3.1.
            Assert.Single(Regex.Matches(html, "class=\"sb-lp__cta\""));
            Assert.EndsWith(">Nhận báo giá</a>", Decode(Regex.Match(html, "<a class=\"sb-lp__cta\"[^>]*>[^<]*</a>").Value));
            Assert.Contains("<input type=\"hidden\" name=\"formType\" value=\"landing\">", main);
            Assert.Equal(landing.Title, Decode(AttrOf(Regex.Match(main, "<input type=\"text\" id=\"quoteRequestProduct\"[^>]*>").Value, "value")!));
            AssertPageWideRules(html, allowDatMua: true);

            // Media AltText wins over the name.
            await f.SetAltTextAsync(image, "Thùng rượu gỗ sồi 1 ngựa");
            main = Section(await GetHtmlAsync(landing.Permalink, HostnameOf(siteB)), "<main", "</main>");
            Assert.Equal("Thùng rượu gỗ sồi 1 ngựa", Decode(AttrOf(Imgs(VariantCards(main)[0])[0], "alt")!));
        });
    }

    // Story 3.2 matrix: No variants.
    [Fact]
    public async Task No_Variants_Render_No_Section_Or_Grid()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var landing = await f.LandingAsync("Novariant Landing");
            await f.UpdateLandingAsync(landing.Id, p => p.VariantsTitle = "Chọn mẫu");

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            Assert.DoesNotContain("sb-lp__variants", html);
            Assert.DoesNotContain("sb-grid", html);
            Assert.DoesNotContain("data-lp-variant", html);
            Assert.DoesNotContain("Chọn mẫu", Decode(html));
            AssertPageWideRules(html);
        });
    }

    // Story 3.2 matrix: Blank variant.
    [Fact]
    public async Task Blank_Variants_Are_Skipped_And_All_Blank_Renders_No_Section()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var image = await f.UploadAsync();
            var landing = await f.LandingAsync("Blankvariant Landing");
            await f.UpdateLandingAsync(landing.Id, p =>
            {
                p.Variants.Add(new LandingVariant { Image = image, Name = "  ", Label = null, Price = "9.999.999đ" });
                p.Variants.Add(new LandingVariant { Name = "KeptVariant", Price = "1.000.000đ" });
                p.Variants.Add(new LandingVariant { Name = null, Label = " ", Price = null });
            });

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var cards = VariantCards(Section(html, "<main", "</main>"));
            Assert.Contains("KeptVariant", Decode(Assert.Single(cards)));
            Assert.DoesNotContain("9.999.999", html);
            Assert.DoesNotContain(image.ToString(), html);

            // All blank: no section at all.
            await f.UpdateLandingAsync(landing.Id, p => p.Variants.RemoveAt(1));
            html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            Assert.DoesNotContain("sb-lp__variants", html);
            Assert.DoesNotContain("data-lp-variant", html);
            AssertPageWideRules(html);
        });
    }

    // Story 3.2 matrix: Label only.
    [Fact]
    public async Task Label_Only_Variant_Uses_Label_As_Title_Without_Chip()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var landing = await f.LandingAsync("Labelonly Landing");
            await f.UpdateLandingAsync(landing.Id, p =>
                p.Variants.Add(new LandingVariant { Label = " 2 ngựa ", Price = "3.000.000đ" }));

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var card = Assert.Single(VariantCards(Section(html, "<main", "</main>")));
            Assert.Contains("<h2 class=\"sb-card__title\">2 ngựa</h2>", Decode(card));
            Assert.DoesNotContain("sb-lp__chip", card);
            var cta = VariantCta(card);
            Assert.Equal("2 ngựa – 3.000.000đ", cta.Prefill);
            Assert.Equal("Đặt mua ngay 2 ngựa – đến form đặt hàng", cta.AriaLabel);
            AssertPageWideRules(html, allowDatMua: true);
        });
    }

    // Story 3.2 matrix: No price (decision a).
    [Fact]
    public async Task Priceless_Variant_Shows_Contact_Line_And_Quote_Cta()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var landing = await f.LandingAsync("Noprice Landing");
            await f.UpdateLandingAsync(landing.Id, p =>
                p.Variants.Add(new LandingVariant { Name = "Ngựa kéo", Label = "Gỗ sồi", Price = "   " }));

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var card = Assert.Single(VariantCards(Section(html, "<main", "</main>")));
            Assert.Contains("<p class=\"sb-card__price sb-card__price--contact\">Liên hệ báo giá</p>", Decode(card));
            var cta = VariantCta(card);
            Assert.Equal("Nhận báo giá", cta.Text);
            Assert.Equal("Ngựa kéo", cta.Prefill);
            Assert.Equal("Nhận báo giá Ngựa kéo – đến form đặt hàng", cta.AriaLabel);
            // No "Đặt mua" anywhere without a real price.
            AssertPageWideRules(html);
        });
    }

    // Story 3.2 matrix: Section title (decision a).
    [Fact]
    public async Task Section_Title_Renders_H2_With_H3_Cards_And_Blank_Title_Gives_H2_Cards()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var landing = await f.LandingAsync("Sectiontitle Landing");
            await f.UpdateLandingAsync(landing.Id, p =>
            {
                p.VariantsTitle = "  Chọn mẫu <thùng> ";
                p.Variants.Add(new LandingVariant { Name = "TitledVariant", Price = "1.500.000đ" });
            });

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var section = Section(Section(html, "<main", "</main>"), "<section class=\"sb-lp__variants\"", "</section>");
            var h2 = Regex.Match(section, "<h2 class=\"sb-lp__variants-title\" id=\"([^\"]+)\">([^<]*)</h2>");
            Assert.True(h2.Success);
            Assert.Equal("Chọn mẫu <thùng>", Decode(h2.Groups[2].Value));
            Assert.Contains($"aria-labelledby=\"{h2.Groups[1].Value}\"", section);
            Assert.DoesNotContain("<thùng>", html);
            Assert.Contains("<h3 class=\"sb-card__title\">TitledVariant</h3>", section);
            Assert.DoesNotContain("<h2 class=\"sb-card__title\"", section);

            await f.UpdateLandingAsync(landing.Id, p => p.VariantsTitle = "   ");
            html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            section = Section(Section(html, "<main", "</main>"), "<section class=\"sb-lp__variants\"", "</section>");
            Assert.DoesNotContain("sb-lp__variants-title", section);
            Assert.DoesNotContain("aria-labelledby", section);
            Assert.Contains("<h2 class=\"sb-card__title\">TitledVariant</h2>", section);
            Assert.DoesNotContain("<h3", section);
            AssertPageWideRules(html, allowDatMua: true);
        });
    }

    // Story 3.2 matrix: No image.
    [Fact]
    public async Task Imageless_Variant_Keeps_The_Empty_Thumb_Frame()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var landing = await f.LandingAsync("Noimage Landing");
            await f.UpdateLandingAsync(landing.Id, p =>
                p.Variants.Add(new LandingVariant { Name = "NoImageVariant", Price = "2.000.000đ" }));

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var card = Assert.Single(VariantCards(Section(html, "<main", "</main>")));
            Assert.Empty(Imgs(card));
            Assert.Matches("<div class=\"sb-card__thumb\">\\s*</div>", card);
            AssertPageWideRules(html, allowDatMua: true);
        });
    }

    // Story 3.2 matrix: HTML in fields.
    [Fact]
    public async Task Variant_Fields_Are_Encoded_In_Text_And_Attributes()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var image = await f.UploadAsync();
            var landing = await f.LandingAsync("Encode Landing");
            await f.UpdateLandingAsync(landing.Id, p =>
                p.Variants.Add(new LandingVariant { Image = image, Name = "<b>x</b> \"q\"", Label = "<i>chip</i>", Price = "<s>1đ</s>" }));

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var card = Assert.Single(VariantCards(Section(html, "<main", "</main>")));
            foreach (var raw in new[] { "<b>", "<i>", "<s>" })
            {
                Assert.DoesNotContain(raw, card);
            }
            var decoded = Decode(card);
            Assert.Contains("<h2 class=\"sb-card__title\"><b>x</b> \"q\"</h2>", decoded);
            Assert.Contains("<p class=\"sb-lp__chip\"><i>chip</i></p>", decoded);
            Assert.Contains("<p class=\"sb-card__price\"><s>1đ</s></p>", decoded);
            var cta = VariantCta(card);
            Assert.Equal("<b>x</b> \"q\" – <s>1đ</s>", cta.Prefill);
            Assert.Equal("Đặt mua ngay <b>x</b> \"q\" – đến form đặt hàng", cta.AriaLabel);
            Assert.Equal("<b>x</b> \"q\"", Decode(AttrOf(Imgs(card)[0], "alt")!));
            AssertPageWideRules(html, allowDatMua: true);
        });
    }

    // Story 3.2 matrix: CTA tapped. JS is not executed here: the anchor
    // target, the prefill attribute and the script tag are what it relies on.
    [Fact]
    public async Task Card_Cta_Is_An_Anchor_To_The_Form_With_Prefill_And_Script()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var landing = await f.LandingAsync("Tap Landing");
            await f.UpdateLandingAsync(landing.Id, p =>
                p.Variants.Add(new LandingVariant { Name = "TapVariant", Label = "1 ngựa", Price = "4.000.000đ" }));

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            var cta = VariantCta(Assert.Single(VariantCards(main)));
            Assert.Equal("#dat-hang", AttrOf(cta.Tag, "href"));
            Assert.Equal("TapVariant – 4.000.000đ", cta.Prefill);
            Assert.Contains("name=\"productOfInterest\"", Section(main, "id=\"dat-hang\"", "</section>"));
            Assert.Contains("lead-form.js", html);
            Assert.Contains("landing-page.js", html);

            var js = await _factory.CreateClient().GetStringAsync("/assets/js/landing-page.js");
            Assert.Contains("data-lp-variant", js);
            Assert.Contains("#dat-hang [name=\"productOfInterest\"]", js);
            AssertPageWideRules(html, allowDatMua: true);
        });
    }

    // Story 3.2 matrix: Submit from card.
    [Fact]
    public async Task Lead_Submitted_With_Variant_Prefill_Is_Stored_As_Landing()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var prefill = new LandingVariant { Name = "2 ngựa", Price = "5.500.000đ" }.PrefillText;
        Assert.Equal("2 ngựa – 5.500.000đ", prefill);
        Guid? id = null;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/leads")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    name = $"Variant Lead {Guid.NewGuid():N}",
                    phone = "0901234567",
                    productOfInterest = prefill,
                    formType = LandingPage.FormType
                }), Encoding.UTF8)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Host = HostnameOf(siteB);
            var response = await _factory.CreateClient().SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, body);
            using (var json = JsonDocument.Parse(body))
            {
                id = json.RootElement.GetProperty("id").GetGuid();
            }

            using var dbScope = _factory.Services.CreateScope();
            var leadDb = dbScope.ServiceProvider.GetRequiredService<LeadDbContext>();
            var row = await leadDb.FormSubmissions.AsNoTracking().SingleAsync(s => s.Id == id);
            Assert.Equal("landing", row.FormType);
            Assert.Equal(prefill, row.ProductOfInterest);
            Assert.Equal(siteB.Id, row.SiteId);
        }
        finally
        {
            if (id.HasValue)
            {
                using var dbScope = _factory.Services.CreateScope();
                var leadDb = dbScope.ServiceProvider.GetRequiredService<LeadDbContext>();
                leadDb.FormSubmissions.RemoveRange(await leadDb.FormSubmissions.Where(s => s.Id == id).ToListAsync());
                await leadDb.SaveChangesAsync();
            }
        }
    }

    // Matrix: CTA label.
    [Theory]
    [InlineData(null, "Nhận báo giá")]
    [InlineData("   ", "Nhận báo giá")]
    [InlineData("  Đặt mua ngay  ", "Đặt mua ngay")]
    [InlineData("Xem <giá> & đặt", "Xem <giá> & đặt")]
    public async Task Cta_Label_Is_Editor_Set_With_Default_And_Encoded(string? label, string expected)
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var landing = await f.LandingAsync("Cta Landing", ctaLabel: label);

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var match = Regex.Match(html, "<a class=\"sb-lp__cta\"[^>]*>([^<]*)</a>");
            Assert.True(match.Success);
            Assert.Equal(expected, Decode(match.Groups[1].Value));
            Assert.Equal($"{expected} – đến form đặt hàng", Decode(AttrOf(match.Value, "aria-label")!));
            Assert.DoesNotContain("<giá>", html);
            AssertPageWideRules(html, allowDatMua: expected.Contains("Đặt mua"));
        });
    }

    // Matrix: Contact settings blank.
    [Fact]
    public async Task Blank_Contact_Settings_Render_No_Alternatives_Line()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            await f.SetContactAsync(string.Empty, string.Empty);
            var landing = await f.LandingAsync("Nocontact Landing");

            var html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            Assert.DoesNotContain("sb-lp__alt", html);
            Assert.DoesNotContain("tel:", html);
            Assert.DoesNotContain("zalo.me", html);
            AssertPageWideRules(html);

            // Phone only: no Zalo link, no separator.
            await f.SetContactAsync("0901 234 567", null);
            html = await GetHtmlAsync(landing.Permalink, HostnameOf(siteB));
            var alt = Section(html, "<p class=\"sb-lp__alt\">", "</p>");
            Assert.Single(Regex.Matches(alt, "<a "));
            Assert.Contains("href=\"tel:0901234567\"", alt);
            Assert.DoesNotContain("·", alt);
        });
    }

    // Matrix: Draft.
    [Fact]
    public async Task Draft_Landing_Page_Is_404_For_Anonymous_Visitors()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var landing = await f.LandingAsync("Draft Landing", published: false);

            var client = _factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, landing.Permalink);
            request.Headers.Host = HostnameOf(siteB);
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    // AC: a second landing page with a new slug renders with the same template.
    [Fact]
    public async Task Any_New_Landing_Page_Renders_With_The_Same_Template()
    {
        await WithFixtureAsync(async (api, siteB, f) =>
        {
            var first = await f.LandingAsync("Campaign One");
            var second = await f.LandingAsync("Campaign Two");
            Assert.NotEqual(first.Permalink, second.Permalink);

            foreach (var page in new[] { first, second })
            {
                var html = await GetHtmlAsync(page.Permalink, HostnameOf(siteB));
                Assert.Contains("<main class=\"sb-lp\">", html);
                Assert.Contains("id=\"dat-hang\"", html);
                Assert.Contains("class=\"sb-lp__cta\"", html);
                AssertPageWideRules(html);
            }
        });
    }

    // Matrix: Landing lead.
    [Theory]
    [InlineData("landing", "landing")]
    [InlineData("LANDING", "landing")]
    [InlineData("landing-x", "general")]
    public async Task Landing_Lead_Is_Stored_As_Landing_And_Email_Labels_It(string sent, string stored)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(siteB.Id)
            ?? await api.Sites.CreateContentAsync<SiteSettings>();
        var originalEmails = settings.NotificationEmails?.Value;
        Guid? id = null;

        try
        {
            settings.NotificationEmails = "landing-sales@x.vn";
            await api.Sites.SaveContentAsync(siteB.Id, settings);

            var name = $"Landing Lead {Guid.NewGuid():N}";
            var client = _factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/leads")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    name,
                    phone = "0901234567",
                    productOfInterest = LandingPageSeed.Title,
                    formType = sent
                }), Encoding.UTF8)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Host = HostnameOf(siteB);
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, body);
            using (var json = JsonDocument.Parse(body))
            {
                id = json.RootElement.GetProperty("id").GetGuid();
            }

            using (var dbScope = _factory.Services.CreateScope())
            {
                var leadDb = dbScope.ServiceProvider.GetRequiredService<LeadDbContext>();
                var row = await leadDb.FormSubmissions.AsNoTracking().SingleAsync(s => s.Id == id);
                Assert.Equal(stored, row.FormType);
                Assert.Equal(siteB.Id, row.SiteId);
            }

            var email = await _factory.EmailSender.WaitForSubjectContainingAsync(name);
            Assert.Contains(
                stored == "landing" ? "Loại form: Landing page quảng cáo" : "Loại form: Liên hệ / báo giá",
                email.Body);
        }
        finally
        {
            var restore = (await api.Sites.GetContentByIdAsync<SiteSettings>(siteB.Id))!;
            restore.NotificationEmails = originalEmails;
            await api.Sites.SaveContentAsync(siteB.Id, restore);

            if (id.HasValue)
            {
                using var dbScope = _factory.Services.CreateScope();
                var leadDb = dbScope.ServiceProvider.GetRequiredService<LeadDbContext>();
                leadDb.FormSubmissions.RemoveRange(await leadDb.FormSubmissions.Where(s => s.Id == id).ToListAsync());
                await leadDb.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public void Composer_Labels_Landing_Leads()
    {
        var email = LeadEmailComposer.Compose(new FormSubmission
        {
            FormType = LandingPage.FormType,
            Name = "A",
            Phone = "0901234567",
            CreatedAt = DateTimeOffset.UtcNow
        }, "Site", new[] { "a@x.vn" });

        Assert.Contains("Loại form: Landing page quảng cáo", email.Body);
    }

    // Matrix: Seeded page.
    [Fact]
    public async Task Seed_Creates_One_Hidden_Noindex_Draft_And_Is_Idempotent_Per_Slug()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = $"landing-seed-test-{suffix}",
            Title = $"Landing Seed Test {suffix}",
            Hostnames = $"landing-seed-test-{suffix}.local",
            IsDefault = false
        };

        try
        {
            await api.Sites.SaveAsync(site);
            var existing = await api.Pages.CreateAsync<StandardPage>();
            existing.SiteId = site.Id;
            existing.SortOrder = 0;
            existing.Title = $"Existing {suffix}";
            existing.Slug = $"existing-{suffix}";
            existing.Published = DateTime.Now.AddMinutes(-5);
            await api.Pages.SaveAsync(existing);

            await LandingPageSeed.EnsureSeededAsync(api, site.Id);

            var sitemap = await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false);
            Assert.Equal(2, sitemap.Count);
            var info = await api.Pages.GetBySlugAsync<PageInfo>(LandingPageSeed.Slug, site.Id);
            Assert.NotNull(info);
            Assert.Equal(info!.Id, sitemap[1].Id);
            var seeded = (await api.Pages.GetByIdAsync<LandingPage>(info.Id))!;
            Assert.Equal(nameof(LandingPage), seeded.TypeId);
            Assert.Equal("Thùng rượu gỗ – Quà Tết từ làng nghề Đọi Tam", seeded.Title);
            Assert.Null(seeded.Published);
            Assert.True(seeded.IsHidden);
            Assert.False(seeded.MetaIndex);
            Assert.Empty(seeded.Variants);
            Assert.Empty(seeded.Blocks);
            Assert.Null(seeded.IntroText);
            Assert.False(seeded.HasHeroImage);

            // Re-run: nothing created, editor's change kept.
            var renamed = $"Renamed {suffix}";
            seeded.Title = renamed;
            await api.Pages.SaveAsync(seeded);
            await LandingPageSeed.EnsureSeededAsync(api, site.Id);
            await LandingPageSeed.EnsureSeededAsync(api, site.Id);
            sitemap = await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false);
            Assert.Equal(2, sitemap.Count);
            Assert.Equal(renamed, (await api.Pages.GetByIdAsync<PageInfo>(info.Id))!.Title);
        }
        finally
        {
            foreach (var item in await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false))
            {
                await api.Pages.DeleteAsync(item.Id);
            }
            if (await api.Sites.GetByIdAsync(site.Id) != null)
            {
                await api.Sites.DeleteAsync(site.Id);
            }
        }
    }

    [Fact]
    public async Task Startup_Seeded_The_Landing_Page_On_Site_B()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);

        var info = await api.Pages.GetBySlugAsync<PageInfo>(LandingPageSeed.Slug, siteB.Id);
        Assert.NotNull(info);
        Assert.Equal(nameof(LandingPage), info!.TypeId);
        Assert.True(info.IsHidden);
    }

    // --- helpers ---

    /// <summary>
    /// AC: every <c>&lt;img&gt;</c> has a non-empty alt; no iframe, autoplay,
    /// cart or login UI; no "Đặt mua" unless the editor set it as the CTA.
    /// </summary>
    private static void AssertPageWideRules(string html, bool allowDatMua = false)
    {
        var decoded = Decode(html);
        if (!allowDatMua)
        {
            Assert.DoesNotContain("Đặt mua", decoded);
        }
        Assert.DoesNotContain("autoplay", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("giỏ hàng", decoded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("thanh toán", decoded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("đăng nhập", decoded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"password\"", html, StringComparison.OrdinalIgnoreCase);
        foreach (var img in Imgs(html))
        {
            Assert.False(string.IsNullOrWhiteSpace(AttrOf(img, "alt")), $"Image without alt: {img}");
        }
    }

    private static List<string> VariantCards(string html) =>
        Regex.Matches(html, "<article class=\"sb-card sb-lp__variant\">.*?</article>", RegexOptions.Singleline)
            .Select(m => m.Value).ToList();

    /// <summary>A card's CTA: its open tag, and its decoded text, prefill and aria-label.</summary>
    private static (string Tag, string Text, string Prefill, string AriaLabel) VariantCta(string card)
    {
        var m = Regex.Match(card, "(<a class=\"sb-lp__variant-cta\"[^>]*>)([^<]*)</a>");
        Assert.True(m.Success, "Expected a variant CTA.");
        var tag = m.Groups[1].Value;
        return (tag, Decode(m.Groups[2].Value), Decode(AttrOf(tag, "data-lp-variant")!), Decode(AttrOf(tag, "aria-label")!));
    }

    private static string BodyTag(string html) => Regex.Match(html, "<body[^>]*>").Value;

    private static List<string> Imgs(string html) =>
        Regex.Matches(html, "<img\\b[^>]*>").Select(m => m.Value).ToList();

    private static string? AttrOf(string tag, string name)
    {
        var m = Regex.Match(tag, $"\\s{Regex.Escape(name)}=\"([^\"]*)\"");
        return m.Success ? m.Groups[1].Value : null;
    }

    private async Task<string> GetHtmlAsync(string permalink, string hostname)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, permalink);
        request.Headers.Host = hostname;

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }

    private async Task WithFixtureAsync(Func<IApi, Site, Fixture, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var fixture = new Fixture(_factory.Services, siteB);

        try
        {
            await body(api, siteB, fixture);
        }
        finally
        {
            await fixture.CleanupAsync();
        }
    }

    /// <summary>
    /// Builds and tears down one test's pages, media and contact-settings
    /// changes. Every edit runs in its own DI scope (see the Story 2.4 notes
    /// in <see cref="ProductDetailPageTests"/>).
    /// </summary>
    private sealed class Fixture
    {
        private readonly IServiceProvider _services;
        private readonly Site _siteB;
        private readonly List<Guid> _pages = new();
        private readonly List<Guid> _media = new();
        private (string? Phone, string? Zalo)? _contact;

        public Fixture(IServiceProvider services, Site siteB)
        {
            _services = services;
            _siteB = siteB;
        }

        public string Suffix { get; } = Guid.NewGuid().ToString("N")[..8];

        public async Task<LandingPage> LandingAsync(string title, bool published = true, bool isHidden = true,
            string? eyebrow = null, string? ctaLabel = null, string? block = null)
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            var page = await api.Pages.CreateAsync<LandingPage>();
            page.SiteId = _siteB.Id;
            page.SortOrder = NonStartPageSortOrder;
            page.Title = $"{title} {Suffix}";
            page.Slug = $"landing-test-{Guid.NewGuid():N}";
            page.IsHidden = isHidden;
            page.Published = published ? DateTime.Now.AddMinutes(-5) : null;
            page.Eyebrow = eyebrow;
            page.CtaLabel = ctaLabel;
            if (block != null)
            {
                page.Blocks.Add(new HtmlBlock { Body = block });
            }
            await api.Pages.SaveAsync(page);
            _pages.Add(page.Id);
            return (await api.Pages.GetByIdAsync<LandingPage>(page.Id))!;
        }

        public async Task UpdateLandingAsync(Guid id, Action<LandingPage> change)
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            var page = (await api.Pages.GetByIdAsync<LandingPage>(id))!;
            change(page);
            await api.Pages.SaveAsync(page);
        }

        /// <summary>A published top-level StandardPage on Site B, deleted on cleanup.</summary>
        public async Task<StandardPage> StandardPageAsync(string title)
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            var page = await api.Pages.CreateAsync<StandardPage>();
            page.SiteId = _siteB.Id;
            page.SortOrder = NonStartPageSortOrder;
            page.Title = $"{title} {Suffix}";
            page.Slug = $"landing-probe-{Guid.NewGuid():N}";
            page.Published = DateTime.Now.AddMinutes(-5);
            await api.Pages.SaveAsync(page);
            _pages.Add(page.Id);
            return (await api.Pages.GetByIdAsync<StandardPage>(page.Id))!;
        }

        public async Task<Guid> UploadAsync()
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            using var stream = new MemoryStream(TinyPng);
            var content = new StreamMediaContent
            {
                Filename = $"landing-test-{Guid.NewGuid():N}.png",
                Data = stream
            };
            await api.Media.SaveAsync(content);
            var id = content.Id!.Value;
            _media.Add(id);
            return id;
        }

        public async Task SetAltTextAsync(Guid mediaId, string altText)
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            var media = (await api.Media.GetByIdAsync(mediaId))!;
            media.AltText = altText;
            await api.Media.SaveAsync(media);
        }

        /// <summary>Sets Site B's Phone/Zalo URL; the originals are restored on cleanup.</summary>
        public async Task SetContactAsync(string? phone, string? zalo)
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(_siteB.Id)
                ?? await api.Sites.CreateContentAsync<SiteSettings>();
            _contact ??= (settings.Phone?.Value, settings.ZaloUrl?.Value);
            settings.Phone = phone;
            settings.ZaloUrl = zalo;
            await api.Sites.SaveContentAsync(_siteB.Id, settings);
        }

        public async Task CleanupAsync()
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            try
            {
                foreach (var id in Enumerable.Reverse(_pages))
                {
                    await api.Pages.DeleteAsync(id);
                }
                foreach (var id in _media)
                {
                    await api.Media.DeleteAsync(id);
                }
            }
            finally
            {
                if (_contact is { } c)
                {
                    var settings = (await api.Sites.GetContentByIdAsync<SiteSettings>(_siteB.Id))!;
                    settings.Phone = c.Phone;
                    settings.ZaloUrl = c.Zalo;
                    await api.Sites.SaveContentAsync(_siteB.Id, settings);
                }
            }
        }
    }
}
