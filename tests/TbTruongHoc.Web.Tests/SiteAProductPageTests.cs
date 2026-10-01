#nullable enable

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
using Piranha.Extend.Fields;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using Xunit;
using static TbTruongHoc.Web.Tests.ProductCatalogTests;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 6.4: Site A's product detail page (<c>Views/Cms/SiteAProductPost.cshtml</c>),
/// one test per I/O &amp; Edge-Case Matrix row plus the acceptance criteria.
/// Real HTTP renders on Site A (shared dev DB: asserts are scoped to the
/// test's own pages); each test builds and deletes its own hub/category/
/// product/media, and restores Site A's phone/Zalo settings.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class SiteAProductPageTests
{
    // 220x90 solid PNG (resizable to every PDP size).
    private static readonly byte[] WidePng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAANwAAABaCAIAAABc0a8TAAAAwElEQVR42u3SQQ0AAAjEsPODIyxhmuCCR5MqWJbpglciAaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMaUKmBJMiSnBlJgSTIkpwZRgSkwJpsSUYEpMCabElGBKMCWmBFNiSjAlpgRTgikxJZgSU4IpMSWYElOCKcGUmBJMiSnBlJgSTAmmxJRgSkwJpsSUYEpMCaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMSWYEkyJKcGUmBJMiSnBlHBTLjqcMk/EWR93AAAAAElFTkSuQmCC");

    private const string Shipping = "Giao hàng toàn quốc — 3–7 ngày làm việc tùy khu vực";

    private readonly PiranhaWebApplicationFactory _factory;

    public SiteAProductPageTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Matrix: Full shippable product. AC: own URL with its SEO <title>/meta.
    [Fact]
    public async Task Full_Shippable_Product_Renders_Scaffold_In_Order()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            await SaveContactAsync(api, siteA.Id, "0909 123 456", "https://zalo.me/0909123456");

            var hub = await c.HubAsync("Pdp A Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp A Cat", 0);
            var post = await c.PostAsync(archive, "Giuong", price: "185.000đ / chiếc", excerpt: "Mô tả ngắn sản phẩm");
            var primary = await media.UploadAsync();
            var second = await media.UploadAsync(altText: "Chi tiết mặt lưới");
            await UpdatePostAsync(post.Id, p =>
            {
                p.Sku = "GL-01";
                p.PrimaryImage = primary;
                p.Photos.Add(new ImageField { Id = second });
                p.Specs.Add(new ProductSpecRow { Label = "Kích thước", Value = "130 × 60 cm" });
                p.Specs.Add(new ProductSpecRow { Label = "Chất liệu", Value = "Nhựa PE" });
                p.Specs.Add(new ProductSpecRow { Label = "Tải trọng", Value = "60kg" });
                p.MetaTitle = $"SEO PDP {c.Suffix}";
                p.MetaDescription = $"SEO PDP description {c.Suffix}";
            });

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteA));
            var decoded = Decode(html);
            Assert.Contains($"<title>SEO PDP {c.Suffix}</title>", decoded);
            Assert.Contains($"<meta name=\"description\" content=\"SEO PDP description {c.Suffix}\">", decoded);

            var main = Decode(Section(html, "<main class=\"sa-pdp\">", "</main>"));

            // Breadcrumb: Trang chủ / hub / category (links) / product (plain).
            var crumb = Section(main, "<nav class=\"sa-breadcrumb", "</nav>");
            Assert.Matches(new Regex(
                "<a href=\"/\">Trang chủ</a>\\s*<span class=\"sa-breadcrumb__sep\" aria-hidden=\"true\">/</span>\\s*" +
                $"<a href=\"{Regex.Escape(hub.Permalink)}\">{Regex.Escape(hub.Title)}</a>\\s*<span class=\"sa-breadcrumb__sep\" aria-hidden=\"true\">/</span>\\s*" +
                $"<a href=\"{Regex.Escape(archive.Permalink)}\">{Regex.Escape(archive.Title)}</a>\\s*<span class=\"sa-breadcrumb__sep\" aria-hidden=\"true\">/</span>\\s*" +
                $"<span aria-current=\"page\">{Regex.Escape(post.Title)}</span>"), crumb);

            // Gallery: main image + 2 thumbs on sb-gallery.js hooks.
            Assert.Contains("sa-pdp__layout sa-pdp__layout--gallery", main);
            var gallery = Section(main, "<div class=\"sa-pdp__gallery\"", "</ul>");
            Assert.Matches(@"\sdata-sb-gallery[\s>=]", gallery);
            Assert.Contains("data-sa-pdp-gallery=\"false\"", gallery);
            var thumbs = Regex.Matches(gallery, "<a class=\"sa-pdp__thumb\"[^>]*>").Select(m => m.Value).ToList();
            Assert.Equal(2, thumbs.Count);
            Assert.All(thumbs, t => Assert.Matches(@"\sdata-sb-gallery-thumb[\s>=]", t));
            Assert.Equal("Chi tiết mặt lưới", AttrOf(thumbs[1], "data-alt"));
            Assert.Equal($"{post.Title} – ảnh 1", AttrOf(Regex.Match(gallery, "<img class=\"sa-pdp__image\"[^>]*>").Value, "alt"));
            Assert.DoesNotContain("sa-photo-badge", gallery);
            Assert.Contains("sb-gallery.js", html);

            // Info column order.
            var info = Section(main, "<div class=\"sa-pdp__info\">", "<section class=\"sa-pdp__specs\"");
            var order = new[]
            {
                $"<h1 class=\"sa-pdp__title\">{post.Title}</h1>",
                "<p class=\"sa-pdp__sku\">Mã: GL-01</p>",
                "<p class=\"sa-pdp__excerpt\">Mô tả ngắn sản phẩm</p>",
                "<div class=\"sa-price-block\">",
                "<p class=\"sa-shipping-note\">",
                "<div class=\"sa-pdp__ctas\">",
            }.Select(m => info.IndexOf(m, StringComparison.Ordinal)).ToList();
            Assert.All(order, i => Assert.True(i >= 0));
            Assert.Equal(order.OrderBy(i => i), order);
            Assert.True(main.IndexOf("sa-pdp__gallery", StringComparison.Ordinal) < main.IndexOf("sa-pdp__info", StringComparison.Ordinal));

            // Price present + bulk note.
            var price = Section(info, "<div class=\"sa-price-block\">", "</div>");
            Assert.Contains("<p class=\"sa-price-block__label\">Giá bán</p>", price);
            Assert.Contains("<p class=\"sa-price-block__price\">185.000đ / chiếc</p>", price);
            Assert.Contains("Giá tham khảo — liên hệ để nhận báo giá ưu đãi khi đặt số lượng lớn.", price);
            Assert.DoesNotContain("Liên hệ để nhận báo giá<", price);

            Assert.Contains(Shipping, info);

            // CTAs: amber quote button, then call and Zalo.
            var ctas = Section(info, "<div class=\"sa-pdp__ctas\">", "</div>");
            var buttons = Regex.Matches(ctas, "<(a|button) [^>]*>[^<]*</(a|button)>").Select(m => m.Value).ToList();
            Assert.Equal(3, buttons.Count);
            Assert.StartsWith("<button type=\"button\" class=\"sa-btn sa-btn--primary\" data-sa-modal-open=\"sa-quote-modal\"", buttons[0]);
            Assert.EndsWith(">Yêu cầu báo giá</button>", buttons[0]);
            Assert.Equal("<a class=\"sa-btn sa-btn--secondary\" href=\"tel:0909123456\">Gọi 0909 123 456</a>", buttons[1]);
            Assert.Contains("href=\"https://zalo.me/0909123456\"", buttons[2]);
            Assert.Contains("aria-label=\"Zalo tư vấn – nhắn tin qua Zalo (mở tab mới)\"", buttons[2]);
            Assert.EndsWith(">Zalo tư vấn</a>", buttons[2]);

            // No trust claims anywhere -> no chip row.
            Assert.DoesNotContain("sa-pdp__trust", main);

            // Specs: full-width table after the two columns, then nothing Site B.
            var specs = Section(main, "<section class=\"sa-pdp__specs\"", "</section>");
            Assert.Contains("<h2 id=\"sa-pdp-specs-title\">Thông số kỹ thuật</h2>", specs);
            var rows = Regex.Matches(specs, "<tr>\\s*<th scope=\"row\">([^<]*)</th>\\s*<td>([^<]*)</td>").Select(m => (m.Groups[1].Value, m.Groups[2].Value)).ToList();
            Assert.Equal(new[] { ("Kích thước", "130 × 60 cm"), ("Chất liệu", "Nhựa PE"), ("Tải trọng", "60kg") }, rows);
            Assert.DoesNotContain("class=\"sb-", main);
            Assert.DoesNotContain("sb-trust", html);

            // Quote modal: native dialog with title, labelled close button, general form prefilled.
            var dialog = Decode(Section(html, "<dialog class=\"sa-modal\" id=\"sa-quote-modal\"", "</dialog>"));
            Assert.Contains("aria-labelledby=\"sa-quote-modal-title\"", dialog);
            Assert.Contains("<h2 class=\"sa-modal__title\" id=\"sa-quote-modal-title\">Yêu cầu báo giá</h2>", dialog);
            Assert.Matches(new Regex("<button type=\"button\" class=\"sa-modal__close\" data-sa-modal-close aria-label=\"Đóng hộp thoại yêu cầu báo giá\">[\\s\\S]*Đóng\\s*</button>"), dialog);
            Assert.Contains("data-quote-request-form", dialog);
            Assert.Contains("<input type=\"hidden\" name=\"formType\" value=\"general\">", dialog);
            Assert.Contains($"name=\"productOfInterest\" value=\"{post.Title}\"", dialog);
            Assert.Contains("lead-form.js", html);
        });
    }

    // Matrix: Blank price; encoding of price/sku/spec.
    [Fact]
    public async Task Blank_Price_Uses_Fallback_In_Same_Block_And_Values_Are_Encoded()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Blank Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp Blank Cat", 0);
            var blank = await c.PostAsync(archive, "Blank", price: "  ");
            var injected = await c.PostAsync(archive, "Injected", price: "<script>alert(1)</script>");
            await UpdatePostAsync(injected.Id, p =>
            {
                p.Sku = "<script>sku()</script>";
                p.Specs.Add(new ProductSpecRow { Label = "<script>l()</script>", Value = "<script>v()</script>" });
            });

            var blankHtml = Decode(Section(await GetHtmlAsync(blank.Permalink, HostnameOf(siteA)), "<main class=\"sa-pdp\">", "</main>"));
            var block = Section(blankHtml, "<div class=\"sa-price-block\">", "</div>");
            Assert.Contains("<p class=\"sa-price-block__label\">Giá bán</p>", block);
            Assert.Contains("<p class=\"sa-price-block__price sa-price-block__price--fallback\">Liên hệ để nhận báo giá</p>", block);
            Assert.Contains("Giá phụ thuộc số lượng và yêu cầu cụ thể — gửi yêu cầu để nhận báo giá.", block);
            Assert.DoesNotContain("Giá tham khảo", block);
            // Same position: right before the shipping note.
            Assert.True(blankHtml.IndexOf("sa-price-block", StringComparison.Ordinal) < blankHtml.IndexOf("sa-shipping-note", StringComparison.Ordinal));
            // No specs -> no table.
            Assert.DoesNotContain("sa-pdp__specs", blankHtml);

            var raw = Section(await GetHtmlAsync(injected.Permalink, HostnameOf(siteA)), "<main class=\"sa-pdp\">", "</main>");
            Assert.DoesNotContain("<script>", raw);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", raw);
            Assert.Contains("&lt;script&gt;sku()&lt;/script&gt;", raw);
            Assert.Contains("&lt;script&gt;l()&lt;/script&gt;", raw);
            Assert.Contains("&lt;script&gt;v()&lt;/script&gt;", raw);
        });
    }

    // Matrix: No photos.
    [Fact]
    public async Task No_Photos_Renders_No_Gallery_And_Full_Width_Info()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            var hub = await c.HubAsync("Pdp NoPhoto Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp NoPhoto Cat", 0);
            var post = await c.PostAsync(archive, "NoPhoto");

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteA));
            var main = Section(html, "<main class=\"sa-pdp\">", "</main>");
            Assert.Contains("<div class=\"sa-pdp__layout\">", main);
            Assert.DoesNotContain("sa-pdp__layout--gallery", main);
            Assert.DoesNotContain("sa-pdp__gallery", main);
            Assert.DoesNotContain("<img", main);
            Assert.DoesNotContain("sb-gallery.js", html);
        });
    }

    // Matrix: No phone / Zalo.
    [Fact]
    public async Task No_Phone_Or_Zalo_Leaves_Only_The_Quote_Button()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            await SaveContactAsync(api, siteA.Id, null, null);
            var hub = await c.HubAsync("Pdp NoContact Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp NoContact Cat", 0);
            var post = await c.PostAsync(archive, "NoContact");

            var main = Decode(Section(await GetHtmlAsync(post.Permalink, HostnameOf(siteA)), "<main class=\"sa-pdp\">", "</main>"));
            var ctas = Section(main, "<div class=\"sa-pdp__ctas\">", "</div>");
            Assert.Contains(">Yêu cầu báo giá</button>", ctas);
            Assert.DoesNotContain("<a ", ctas);
            Assert.DoesNotContain("tel:", ctas);
            Assert.DoesNotContain("Zalo", ctas);
        });
    }

    // Matrix: Category not under a hub.
    [Fact]
    public async Task Category_Under_Plain_Page_Gives_Three_Step_Breadcrumb()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            var parent = await api.Pages.CreateAsync<StandardPage>();
            parent.SiteId = siteA.Id;
            parent.SortOrder = 52;
            parent.Title = $"Pdp Plain Parent {c.Suffix}";
            parent.Slug = $"sa-pdp-plain-{c.Suffix}";
            parent.IsHidden = true;
            parent.Published = DateTime.Now.AddMinutes(-5);
            await api.Pages.SaveAsync(parent);
            var parentInfo = (await api.Pages.GetByIdAsync<PageInfo>(parent.Id))!;
            c.DeleteLast(parent.Id);
            var archive = await c.ArchiveAsync(parentInfo, "Pdp Plain Cat", 0);
            var post = await c.PostAsync(archive, "Plain");

            var crumb = Decode(Section(await GetHtmlAsync(post.Permalink, HostnameOf(siteA)), "<nav class=\"sa-breadcrumb", "</nav>"));
            Assert.Equal(2, Regex.Matches(crumb, "<a ").Count);
            Assert.DoesNotContain(parentInfo.Permalink + "\"", crumb);
            Assert.Matches(new Regex(
                "<a href=\"/\">Trang chủ</a>\\s*<span class=\"sa-breadcrumb__sep\" aria-hidden=\"true\">/</span>\\s*" +
                $"<a href=\"{Regex.Escape(archive.Permalink)}\">{Regex.Escape(archive.Title)}</a>\\s*<span class=\"sa-breadcrumb__sep\" aria-hidden=\"true\">/</span>\\s*" +
                $"<span aria-current=\"page\">{Regex.Escape(post.Title)}</span>"), crumb);
        });
    }

    // Matrix: Installation-required product.
    [Fact]
    public async Task Installation_Required_Product_Never_Shows_Shipping()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Install Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp Install Cat", 0);
            await UpdateArchiveAsync(archive.Id, a => a.TrustClaims = "Giao hàng toàn quốc, Bảo hành 12 tháng");
            var installed = await c.PostAsync(archive, "Install", price: "từ 5.000.000đ");
            await UpdatePostAsync(installed.Id, p => p.RequiresInstallation = true);
            var shipped = await c.PostAsync(archive, "Ship");

            var html = Decode(await GetHtmlAsync(installed.Permalink, HostnameOf(siteA)));
            var main = Section(html, "<main class=\"sa-pdp\">", "</main>");
            Assert.DoesNotContain("sa-shipping-note", main);
            Assert.DoesNotContain("Giao hàng toàn quốc", main);
            // Rest of the scaffold unchanged (6.5 swaps the CTA for the survey one).
            Assert.Contains("<p class=\"sa-price-block__price\">từ 5.000.000đ</p>", main);
            Assert.Contains(">Đăng ký khảo sát miễn phí</button>", main);
            Assert.Equal(new[] { "Bảo hành 12 tháng" }, Chips(main));

            // The shippable sibling keeps both the note and the category's claim.
            var shipMain = Decode(Section(await GetHtmlAsync(shipped.Permalink, HostnameOf(siteA)), "<main class=\"sa-pdp\">", "</main>"));
            Assert.Contains(Shipping, shipMain);
            Assert.Equal(new[] { "Giao hàng toàn quốc", "Bảo hành 12 tháng" }, Chips(shipMain));
        });
    }

    // Story 6.5 matrix: Installation PDP (blank price) + Shippable PDP.
    [Fact]
    public async Task Installation_Product_Renders_Survey_Variant_And_Shippable_Does_Not()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            await SaveContactAsync(api, siteA.Id, "0909 123 456", "https://zalo.me/0909123456");

            var hub = await c.HubAsync("Pdp Survey Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp Survey Cat", 0);
            var installed = await c.PostAsync(archive, "Du che <b>");
            await UpdatePostAsync(installed.Id, p => p.RequiresInstallation = true);
            var shipped = await c.PostAsync(archive, "Ship");

            var html = await GetHtmlAsync(installed.Permalink, HostnameOf(siteA));
            var main = Decode(Section(html, "<main class=\"sa-pdp\">", "</main>"));

            // CTA row: survey first, then call/Zalo; no quote button.
            var ctas = Section(main, "<div class=\"sa-pdp__ctas\">", "</div>");
            Assert.Contains("data-sa-modal-open=\"sa-survey-modal\" aria-haspopup=\"dialog\" aria-controls=\"sa-survey-modal\">Đăng ký khảo sát miễn phí</button>", ctas);
            Assert.True(ctas.IndexOf("Đăng ký khảo sát", StringComparison.Ordinal) < ctas.IndexOf("Gọi 0909 123 456", StringComparison.Ordinal));
            Assert.Contains("Zalo tư vấn", ctas);
            Assert.DoesNotContain("Yêu cầu báo giá", main);
            Assert.DoesNotContain("sa-quote-modal", html);
            Assert.DoesNotContain("Giao hàng toàn quốc", main);

            // Blank price: fallback + the installation note.
            Assert.Contains("Liên hệ để nhận báo giá", main);
            Assert.Contains("Giá phụ thuộc điều kiện thi công thực tế — báo giá chính xác sau khi khảo sát hiện trường.", main);
            Assert.DoesNotContain("Giá phụ thuộc số lượng và yêu cầu cụ thể", main);

            // Service-area then process-strip, after the info column.
            var info = main.IndexOf("sa-pdp__ctas", StringComparison.Ordinal);
            var area = main.IndexOf("<section class=\"sa-service-area\"", StringComparison.Ordinal);
            var strip = main.IndexOf("<section class=\"sa-process-strip\"", StringComparison.Ordinal);
            Assert.True(info < area && area < strip, $"order: ctas {info}, area {area}, strip {strip}");
            Assert.Contains("Khu vực phục vụ: Miền Bắc – Thanh Hóa", main);
            Assert.Contains("Ngoài khu vực này, quý khách vẫn có thể gửi đăng ký khảo sát", main);
            var steps = Regex.Matches(Section(main, "<ol class=\"sa-process-strip__steps\">", "</ol>"), "<h3>([^<]*)</h3>").Select(m => m.Groups[1].Value);
            Assert.Equal(new[] { "Khảo sát", "Hợp đồng", "Thi công" }, steps);

            // Survey dialog: product prefilled read-only, 3 required fields, warning hidden.
            var dialog = Section(html, "<dialog class=\"sa-modal\" id=\"sa-survey-modal\"", "</dialog>");
            var decodedDialog = Decode(dialog);
            Assert.Contains("aria-label=\"Đóng hộp thoại đăng ký khảo sát\"", decodedDialog);
            Assert.Equal("survey", Regex.Match(dialog, "name=\"formType\" value=\"([^\"]*)\"").Groups[1].Value);
            var product = Regex.Match(dialog, "<input[^>]*name=\"productOfInterest\"[^>]*>").Value;
            Assert.Equal(installed.Title, AttrOf(product, "value"));
            Assert.Contains(" readonly", product);
            Assert.DoesNotContain("<b>", dialog);
            foreach (var field in new[] { "locationAddress", "name", "phone" })
            {
                var input = Regex.Match(dialog, $"<input[^>]*name=\"{field}\"[^>]*>").Value;
                Assert.Equal("true", AttrOf(input, "aria-required"));
                var id = AttrOf(input, "id");
                Assert.Matches($"<label for=\"{id}\">[^<]+<span aria-hidden=\"true\">\\*</span></label>", decodedDialog);
            }
            Assert.Equal("500", AttrOf(Regex.Match(dialog, "<input[^>]*name=\"locationAddress\"[^>]*>").Value, "maxlength"));
            var location = decodedDialog.IndexOf("name=\"locationAddress\"", StringComparison.Ordinal);
            var warning = decodedDialog.IndexOf("data-sa-survey-warning", StringComparison.Ordinal);
            var nameField = decodedDialog.IndexOf("name=\"name\"", StringComparison.Ordinal);
            Assert.True(location < warning && warning < nameField);
            Assert.Contains("class=\"sa-warning-banner\" id=\"surveyLocationWarning\" data-sa-survey-warning hidden", dialog);
            Assert.Contains("Ngoài khu vực phục vụ trực tiếp", decodedDialog);
            Assert.Contains(">Gửi đăng ký</button>", decodedDialog);
            var areas = AttrOf(Regex.Match(dialog, "<div[^>]*data-sa-survey-areas=\"[^\"]*\"[^>]*>").Value, "data-sa-survey-areas");
            Assert.Equal(ServiceArea.OutsideTerms, JsonSerializer.Deserialize<string[]>(areas!));

            // The shippable sibling has none of it.
            var shipHtml = await GetHtmlAsync(shipped.Permalink, HostnameOf(siteA));
            Assert.DoesNotContain("sa-service-area", shipHtml);
            Assert.DoesNotContain("sa-process-strip", shipHtml);
            Assert.DoesNotContain("sa-survey-modal", shipHtml);
            Assert.DoesNotContain("data-sa-survey-areas", shipHtml);
            Assert.Contains(">Yêu cầu báo giá</button>", Decode(shipHtml));
            Assert.Contains("Giá phụ thuộc số lượng và yêu cầu cụ thể — gửi yêu cầu để nhận báo giá.", Decode(shipHtml));
        });
    }

    // Story 6.5 matrix: Installation + price. AC: the rendered survey form stores a survey lead.
    [Fact]
    public async Task Installation_Product_With_Price_And_Rendered_Survey_Stores_Survey_Lead()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Survey Lead Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp Survey Lead Cat", 0);
            var post = await c.PostAsync(archive, "Survey Lead", price: "12.000.000đ <i>");
            await UpdatePostAsync(post.Id, p => p.RequiresInstallation = true);

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteA));
            var rawMain = Section(html, "<main class=\"sa-pdp\">", "</main>");
            Assert.DoesNotContain("<i>", rawMain);
            var main = Decode(rawMain);
            Assert.Contains("<p class=\"sa-price-block__price\">12.000.000đ <i></p>", main);
            Assert.Contains("Giá tham khảo — liên hệ để nhận báo giá ưu đãi khi đặt số lượng lớn.", main);

            var dialog = Decode(Section(html, "<dialog class=\"sa-modal\" id=\"sa-survey-modal\"", "</dialog>"));
            var formType = Regex.Match(dialog, "name=\"formType\" value=\"([^\"]*)\"").Groups[1].Value;
            var product = Regex.Match(dialog, "name=\"productOfInterest\" value=\"([^\"]*)\"").Groups[1].Value;

            var name = $"Pdp Survey {c.Suffix}";
            const string address = "Trường Tiểu học Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh";
            var client = _factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/leads")
            {
                Content = new StringContent(JsonSerializer.Serialize(new { name, phone = "0987654321", productOfInterest = product, message = "", formType, locationAddress = address }), Encoding.UTF8)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Host = HostnameOf(siteA);
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var leadDb = scope.ServiceProvider.GetRequiredService<LeadDbContext>();
            var saved = await leadDb.FormSubmissions.AsNoTracking().SingleAsync(s => s.Name == name);
            Assert.Equal("survey", saved.FormType);
            Assert.Equal(post.Title, saved.ProductOfInterest);
            Assert.Equal(address, saved.LocationAddress);
            Assert.True(saved.IsOutsideServiceArea);
            Assert.Equal(siteA.Id, saved.SiteId);
        });
    }

    // Matrix: Trust chips.
    [Fact]
    public async Task Trust_Chips_Product_Replaces_Category_Max_Three_Encoded()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Trust Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp Trust Cat", 0);
            await UpdateArchiveAsync(archive.Id, a => a.TrustClaims = "A, B");
            var inherits = await c.PostAsync(archive, "Inherit");
            var own = await c.PostAsync(archive, "Own");
            await UpdatePostAsync(own.Id, p => p.TrustClaims = "C");
            var many = await c.PostAsync(archive, "Many");
            await UpdatePostAsync(many.Id, p => p.TrustClaims = "1, 2, <b>3</b>, 4, 5");

            async Task<string> MainOf(ProductPost post) =>
                Section(await GetHtmlAsync(post.Permalink, HostnameOf(siteA)), "<main class=\"sa-pdp\">", "</main>");

            var inheritsMain = Decode(await MainOf(inherits));
            Assert.Equal(new[] { "A", "B" }, Chips(inheritsMain));
            Assert.Contains("<ul class=\"sa-pdp__trust\" aria-label=\"Cam kết\">", inheritsMain);
            // Trust row after the CTAs; chips are plain spans.
            Assert.True(inheritsMain.IndexOf("sa-pdp__ctas", StringComparison.Ordinal) < inheritsMain.IndexOf("sa-pdp__trust", StringComparison.Ordinal));
            Assert.Equal(new[] { "C" }, Chips(Decode(await MainOf(own))));

            var manyRaw = await MainOf(many);
            Assert.DoesNotContain("<b>3</b>", manyRaw);
            Assert.Equal(new[] { "1", "2", "<b>3</b>" }, Chips(Decode(manyRaw)));
        });
    }

    // Matrix: Genuine photos.
    [Fact]
    public async Task Genuine_Photos_Join_Gallery_After_Regular_With_Badge_Only_On_Them()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Genuine Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp Genuine Cat", 0);
            var post = await c.PostAsync(archive, "Genuine");
            var regular = await media.UploadAsync();
            var genuine = await media.UploadAsync();
            await UpdatePostAsync(post.Id, p =>
            {
                p.PrimaryImage = regular;
                p.GenuinePhotos.Add(new ImageField { Id = genuine });
                p.GenuinePhotos.Add(new ImageField { Id = regular }); // already shown as a regular photo
                p.GenuinePhotos.Add(new ImageField());
            });

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteA));
            var gallery = Decode(Section(Section(html, "<main class=\"sa-pdp\">", "</main>"), "<div class=\"sa-pdp__gallery\"", "</ul>"));
            Assert.Contains("data-sa-pdp-gallery=\"true\"", gallery);

            var thumbs = Regex.Matches(gallery, "<a class=\"sa-pdp__thumb\"[\\s\\S]*?</a>").Select(m => m.Value).ToList();
            Assert.Equal(2, thumbs.Count);
            Assert.Contains(regular.ToString(), AttrOf(thumbs[0], "href")!);
            Assert.Contains(genuine.ToString(), AttrOf(thumbs[1], "href")!);
            Assert.DoesNotContain("sa-photo-badge", thumbs[0]);
            Assert.Equal("false", AttrOf(thumbs[0], "data-genuine"));
            Assert.Contains("<span class=\"sa-photo-badge sa-photo-badge--thumb\" title=\"Hình ảnh thi công thực tế\">Hình ảnh thi công thực tế</span>", thumbs[1]);
            Assert.Equal("true", AttrOf(thumbs[1], "data-genuine"));
            Assert.Equal("Xem ảnh 2 – Hình ảnh thi công thực tế", AttrOf(thumbs[1], "aria-label"));

            // Main image's badge exists but is hidden while the regular photo is selected.
            var figure = Section(gallery, "<div class=\"sa-pdp__figure\">", "</div>");
            Assert.Contains("<span class=\"sa-photo-badge\" data-sa-photo-badge hidden=\"hidden\">Hình ảnh thi công thực tế</span>", figure);
            Assert.Equal(2, Regex.Matches(gallery, "Hình ảnh thi công thực tế</span>").Count);
        });
    }

    [Fact]
    public async Task Only_Genuine_Photo_Shows_Main_Badge_Without_Gallery_Script()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            var hub = await c.HubAsync("Pdp OnlyGenuine Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp OnlyGenuine Cat", 0);
            var post = await c.PostAsync(archive, "OnlyGenuine");
            var genuine = await media.UploadAsync();
            await UpdatePostAsync(post.Id, p => p.GenuinePhotos.Add(new ImageField { Id = genuine }));

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteA));
            var main = Decode(Section(html, "<main class=\"sa-pdp\">", "</main>"));
            Assert.Contains("<span class=\"sa-photo-badge\" data-sa-photo-badge>Hình ảnh thi công thực tế</span>", main);
            Assert.DoesNotContain("sa-pdp__thumbs", main);
            Assert.DoesNotContain("data-sb-gallery", main);
            Assert.DoesNotContain("sb-gallery.js", html);
        });
    }

    // Matrix: Quote modal. AC: the general form stores formType=general + the product.
    [Fact]
    public async Task Quote_Form_In_Modal_Stores_General_Lead_With_Product()
    {
        await WithSiteAAsync(async (api, siteA, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Lead Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp Lead Cat", 0);
            var post = await c.PostAsync(archive, "Lead");

            // What lead-form.js sends: the form's own hidden formType and
            // prefilled productOfInterest.
            var dialog = Decode(Section(await GetHtmlAsync(post.Permalink, HostnameOf(siteA)), "<dialog class=\"sa-modal\"", "</dialog>"));
            var formType = Regex.Match(dialog, "name=\"formType\" value=\"([^\"]*)\"").Groups[1].Value;
            var product = Regex.Match(dialog, "name=\"productOfInterest\" value=\"([^\"]*)\"").Groups[1].Value;
            Assert.Equal("general", formType);
            Assert.Equal(post.Title, product);

            var name = $"Pdp Lead {c.Suffix}";
            var client = _factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/leads")
            {
                Content = new StringContent(JsonSerializer.Serialize(new { name, phone = "0901234567", productOfInterest = product, message = "", formType }), Encoding.UTF8)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Host = HostnameOf(siteA);
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var leadDb = scope.ServiceProvider.GetRequiredService<LeadDbContext>();
            var saved = await leadDb.FormSubmissions.AsNoTracking().SingleAsync(s => s.Name == name);
            Assert.Equal("general", saved.FormType);
            Assert.Equal(post.Title, saved.ProductOfInterest);
            Assert.Equal(siteA.Id, saved.SiteId);
        });
    }

    // Matrix: Site B product.
    [Fact]
    public async Task Site_B_Product_Keeps_Its_Own_View()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var c = new CatalogBuilder(api, siteB);
        try
        {
            var hub = await c.HubAsync("Pdp B Hub");
            var archive = await c.ArchiveAsync(hub, "Pdp B Cat", 0);
            var post = await c.PostAsync(archive, "B");

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteB));
            Assert.Contains("<main class=\"sb-pdp\">", html);
            Assert.Contains("Liên hệ báo giá", Decode(html));
            Assert.Contains("sb-trust", html);
            Assert.DoesNotContain("sa-pdp", html);
            Assert.DoesNotContain("sa-price-block", html);
            Assert.DoesNotContain("<dialog", html);
            Assert.DoesNotContain("Giao hàng toàn quốc", Decode(Section(html, "<main class=\"sb-pdp\">", "</main>")));
        }
        finally
        {
            await c.CleanupAsync();
        }
    }

    // --- model (no DB) ---

    [Fact]
    public void Trust_Chip_List_Falls_Back_To_Category_And_Caps_At_Three()
    {
        var category = new ProductArchive { TrustClaims = "A, B" };
        Assert.Equal(new[] { "A", "B" }, new ProductPost { ParentCategory = category }.TrustChipList);
        Assert.Equal(new[] { "A", "B" }, new ProductPost { ParentCategory = category, TrustClaims = " , " }.TrustChipList);
        Assert.Equal(new[] { "C" }, new ProductPost { ParentCategory = category, TrustClaims = "C" }.TrustChipList);
        Assert.Equal(new[] { "1", "2", "3" }, new ProductPost { TrustClaims = "1,2,3,4,5" }.TrustChipList);
        Assert.Empty(new ProductPost().TrustChipList);
        Assert.Equal(new[] { "B", "C", "D" }, new ProductPost
        {
            RequiresInstallation = true,
            TrustClaims = "giao hàng toàn quốc 48h, B, C, D",
        }.TrustChipList);
    }

    // --- helpers ---

    private static List<string> Chips(string html) =>
        Regex.Matches(html, "<span class=\"sa-trust-chip\">([^<]*(?:<b>[^<]*</b>)?)</span>").Select(m => m.Groups[1].Value).ToList();

    private static string? AttrOf(string tag, string name)
    {
        var m = Regex.Match(tag, $"\\s{Regex.Escape(name)}=\"([^\"]*)\"");
        return m.Success ? Decode(m.Groups[1].Value) : null;
    }

    private async Task UpdatePostAsync(Guid id, Action<ProductPost> change)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var post = (await api.Posts.GetByIdAsync<ProductPost>(id))!;
        change(post);
        await api.Posts.SaveAsync(post);
    }

    private async Task UpdateArchiveAsync(Guid id, Action<ProductArchive> change)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var page = (await api.Pages.GetByIdAsync<ProductArchive>(id))!;
        change(page);
        await api.Pages.SaveAsync(page);
    }

    private static async Task SaveContactAsync(IApi api, Guid siteId, string? phone, string? zalo)
    {
        var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(siteId)
            ?? await api.Sites.CreateContentAsync<SiteSettings>();
        settings.Phone = phone;
        settings.ZaloUrl = zalo;
        await api.Sites.SaveContentAsync(siteId, settings);
    }

    private sealed class Builder
    {
        private readonly CatalogBuilder _inner;
        private readonly List<Guid> _last = new();

        public Builder(IApi api, Site site)
        {
            _inner = new CatalogBuilder(api, site);
        }

        public string Suffix => _inner.Suffix;

        public Task<ProductHubPage> HubAsync(string title) => _inner.HubAsync(title);

        public Task<ProductArchive> ArchiveAsync(PageBase hub, string title, int sortOrder) =>
            _inner.ArchiveAsync(hub, title, sortOrder);

        public Task<ProductPost> PostAsync(PageBase archive, string title, string? price = null, string? excerpt = null) =>
            _inner.PostAsync(archive, title, price: price, excerpt: excerpt);

        /// <summary>A parent page created outside the builder; deleted after the builder's pages.</summary>
        public void DeleteLast(Guid pageId) => _last.Add(pageId);

        public async Task CleanupAsync(IServiceProvider services)
        {
            await _inner.CleanupAsync();
            using var scope = services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            foreach (var id in _last)
            {
                await api.Pages.DeleteAsync(id);
            }
        }
    }

    private sealed class MediaBuilder
    {
        private readonly IApi _api;
        private readonly IServiceProvider _services;
        private readonly List<Guid> _ids = new();

        public MediaBuilder(IApi api, IServiceProvider services)
        {
            _api = api;
            _services = services;
        }

        public async Task<Guid> UploadAsync(string? altText = null)
        {
            using var stream = new MemoryStream(WidePng);
            var content = new StreamMediaContent { Filename = $"sa-pdp-test-{Guid.NewGuid():N}.png", Data = stream };
            await _api.Media.SaveAsync(content);
            var id = content.Id!.Value;
            _ids.Add(id);
            if (altText != null)
            {
                var item = (await _api.Media.GetByIdAsync(id))!;
                item.AltText = altText;
                await _api.Media.SaveAsync(item);
            }
            return id;
        }

        /// <summary>Fresh scope: the render may have added resized versions through another DbContext.</summary>
        public async Task CleanupAsync()
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            foreach (var id in _ids)
            {
                await api.Media.DeleteAsync(id);
            }
        }
    }

    private async Task WithSiteAAsync(Func<IApi, Site, Builder, MediaBuilder, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await api.Sites.GetContentByIdAsync<SiteSettings>(siteA.Id);
        var (phone, zalo) = (original?.Phone?.Value, original?.ZaloUrl?.Value);
        var builder = new Builder(api, siteA);
        var media = new MediaBuilder(api, _factory.Services);

        try
        {
            await body(api, siteA, builder, media);
        }
        finally
        {
            try
            {
                await builder.CleanupAsync(_factory.Services);
            }
            finally
            {
                await media.CleanupAsync();
                using var settingsScope = _factory.Services.CreateScope();
                await SaveContactAsync(settingsScope.ServiceProvider.GetRequiredService<IApi>(), siteA.Id, phone, zalo);
            }
        }
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
}
