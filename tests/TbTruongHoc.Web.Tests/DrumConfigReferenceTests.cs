using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using Xunit;
using static TbTruongHoc.Web.Tests.ProductCatalogTests;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 4.1 (FR-11): the optional drum-config-reference on a
/// <see cref="ProductArchive"/> (<c>Views/Shared/_DrumConfigReference.cshtml</c>).
/// One test per I/O &amp; Edge-Case Matrix row plus the ACs. Real HTTP render
/// on Site B against the MariaDB-backed app; each test builds its own
/// throwaway hub/archive/posts and deletes them afterwards.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class DrumConfigReferenceTests
{
    private const string Disclaimer = "Bảng tham khảo, không tính giá tự động";

    private readonly PiranhaWebApplicationFactory _factory;

    public DrumConfigReferenceTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task No_Config_Renders_Page_As_Before_Without_Config_Form_Or_LeadForm_Script()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Cfg Hub None");
            var archive = await c.ArchiveAsync(hub, "Trong chua none", 0);
            await c.PostAsync(archive, "P1");

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            AssertNoConfig(html);
            Assert.Contains("data-sb-product-grid", html);
        });
    }

    [Fact]
    public async Task Full_Config_Renders_Heading_Rows_In_Order_Disclaimer_And_One_Prefilled_Form_After_Grid()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Cfg Hub Full");
            var archive = await c.ArchiveAsync(hub, "Trong chua full", 0);
            await c.PostAsync(archive, "P1");
            await UpdateArchiveAsync(archive.Id, a =>
            {
                a.DrumConfigTitle = "  Cấu hình trống chùa  ";
                a.DrumConfig.Add(Row("Kích thước", "Mặt 40 – 120 cm"));
                a.DrumConfig.Add(Row("  Loại ", " Loại 1, loại 2, loại 3 "));
                a.DrumConfig.Add(Row("Bánh xe", "Có / không"));
            });

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            var decoded = Decode(main);
            var config = Decode(Section(main, "<section class=\"sb-config\"", "</section>"));

            Assert.Contains("<h2 class=\"sb-config__title\" id=\"sb-config-title\">Cấu hình trống chùa</h2>", config);
            Assert.Contains("<table class=\"sb-config__table\" aria-labelledby=\"sb-config-title\">", config);
            var labels = Regex.Matches(config, "<th class=\"sb-config__label\" scope=\"row\">([^<]*)</th>")
                .Select(m => m.Groups[1].Value).ToArray();
            var values = Regex.Matches(config, "<td class=\"sb-config__value\">([^<]*)</td>")
                .Select(m => m.Groups[1].Value).ToArray();
            Assert.Equal(new[] { "Kích thước", "Loại", "Bánh xe" }, labels);
            Assert.Equal(new[] { "Mặt 40 – 120 cm", "Loại 1, loại 2, loại 3", "Có / không" }, values);
            Assert.Contains($"<p class=\"sb-config__disclaimer\">{Disclaimer}</p>", config);

            // Placement: after the grid (and pager), before the trust-block.
            var gridEnd = main.IndexOf("</ul>", main.IndexOf("data-sb-product-grid", StringComparison.Ordinal), StringComparison.Ordinal);
            var configAt = main.IndexOf("data-sb-config", StringComparison.Ordinal);
            Assert.True(gridEnd >= 0 && configAt > gridEnd, "Config must follow the product grid.");
            // Config-before-trust-block is covered in CraftsmanStoryTests
            // (the trust-block needs a published story).

            // AC: one form, prefilled, formType general, script loaded.
            Assert.Single(Regex.Matches(html, "data-quote-request-form"));
            Assert.Contains("data-quote-request-form", config);
            Assert.Contains($"name=\"productOfInterest\" value=\"Tư vấn cấu hình – {archive.Title}\"", config);
            Assert.Contains("name=\"formType\" value=\"general\"", config);
            Assert.Contains("lead-form.js", html);

            AssertConfigPageRules(html);
            Assert.DoesNotContain("sb-product__price", Decode(Section(main, "<section class=\"sb-config\"", "</section>")));
            Assert.Contains("data-sb-product-grid", decoded);
        });
    }

    [Fact]
    public async Task Half_Empty_Rows_Are_Omitted_Others_Render()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Cfg Hub Half");
            var archive = await c.ArchiveAsync(hub, "Trong chua half", 0);
            await UpdateArchiveAsync(archive.Id, a =>
            {
                a.DrumConfig.Add(Row("Chỉ nhãn", "   "));
                a.DrumConfig.Add(Row("Sơn", "Sơn son, sơn mộc"));
                a.DrumConfig.Add(Row(null, "Chỉ giá trị"));
                a.DrumConfig.Add(Row("Vẽ mặt trống", "Rồng, chữ"));
            });

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            var config = Decode(Section(html, "<section class=\"sb-config\"", "</section>"));

            Assert.Equal(2, Regex.Matches(config, "<tr class=\"sb-config__row\">").Count);
            Assert.Contains(">Sơn</th>", config);
            Assert.Contains(">Vẽ mặt trống</th>", config);
            Assert.DoesNotContain("Chỉ nhãn", config);
            Assert.DoesNotContain("Chỉ giá trị", config);
            AssertConfigPageRules(html);
        });
    }

    [Fact]
    public async Task All_Rows_Invalid_Is_Treated_As_No_Config()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Cfg Hub Invalid");
            var archive = await c.ArchiveAsync(hub, "Trong chua invalid", 0);
            await c.PostAsync(archive, "P1");
            await UpdateArchiveAsync(archive.Id, a =>
            {
                a.DrumConfigTitle = "Tiêu đề không được hiện";
                a.DrumConfig.Add(Row("Chỉ nhãn", null));
                a.DrumConfig.Add(Row(" ", "Chỉ giá trị"));
                a.DrumConfig.Add(Row("", ""));
            });

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            AssertNoConfig(html);
            Assert.DoesNotContain("Tiêu đề không được hiện", Decode(html));
        });
    }

    [Fact]
    public async Task Html_In_Cells_And_Heading_Is_Encoded()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Cfg Hub Html");
            var archive = await c.ArchiveAsync(hub, "Trong chua html", 0);
            await UpdateArchiveAsync(archive.Id, a =>
            {
                a.DrumConfigTitle = "<i>tieu de</i>";
                a.DrumConfig.Add(Row("<b>x</b>", "<script>alert(1)</script>"));
            });

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            var config = Section(html, "<section class=\"sb-config\"", "</section>");

            Assert.DoesNotContain("<b>x</b>", config);
            Assert.DoesNotContain("<i>tieu de</i>", config);
            Assert.DoesNotContain("<script>alert(1)</script>", html);
            Assert.Contains("&lt;b&gt;x&lt;/b&gt;", config);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", config);
            Assert.Contains("&lt;i&gt;tieu de&lt;/i&gt;", config);
        });
    }

    [Fact]
    public async Task Non_Trong_Archive_With_Empty_Region_Is_Unchanged()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Thung ruou Hub");
            var archive = await c.ArchiveAsync(hub, "Thung ruou cat", 0);
            await c.PostAsync(archive, "Thung 1");

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            AssertNoConfig(html);
            Assert.Contains("data-sb-product-grid", html);
        });
    }

    [Fact]
    public async Task Long_Unbroken_Value_Wraps_Inside_Its_Cell()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Cfg Hub Long");
            var archive = await c.ArchiveAsync(hub, "Trong chua long", 0);
            var longValue = new string('x', 240);
            await UpdateArchiveAsync(archive.Id, a => a.DrumConfig.Add(Row("Kích thước", longValue)));

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            var config = Section(html, "<section class=\"sb-config\"", "</section>");
            Assert.Contains($"<td class=\"sb-config__value\">{longValue}</td>", config);

            // Markup/CSS-class check: the table is fixed-layout, full width,
            // and every cell wraps anywhere, so it never widens the page.
            var css = await _factory.CreateClient().GetStringAsync("/assets/css/site-b.css");
            var table = Rule(css, ".sb-config__table {");
            Assert.Contains("table-layout: fixed;", table);
            Assert.Contains("width: 100%;", table);
            var cells = Rule(css, ".sb-config__label,\n.sb-config__value {");
            Assert.Contains("overflow-wrap: anywhere;", cells);
            Assert.DoesNotContain("white-space: nowrap", Rule(css, ".sb-config__panel {"));
        });
    }

    [Fact]
    public async Task Blank_Heading_Defaults_And_Config_Renders_Without_Products()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Cfg Hub Heading");
            var archive = await c.ArchiveAsync(hub, "Trong chua heading", 0);
            await UpdateArchiveAsync(archive.Id, a =>
            {
                a.DrumConfigTitle = "   ";
                a.DrumConfig.Add(Row("Loại", "Loại 1"));
            });

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            var config = Decode(Section(html, "<section class=\"sb-config\"", "</section>"));

            Assert.DoesNotContain("data-sb-product-grid", html);
            Assert.Contains($"<h2 class=\"sb-config__title\" id=\"sb-config-title\">{ProductArchive.DefaultConfigTitle}</h2>", config);
            Assert.Equal("Bảng tham khảo cấu hình trống", ProductArchive.DefaultConfigTitle);
            Assert.Single(Regex.Matches(html, "data-quote-request-form"));
            Assert.Contains("lead-form.js", html);
            AssertConfigPageRules(html);
        });
    }

    private static DrumConfigRow Row(string? label, string? value) =>
        new() { Label = label, Value = value };

    /// <summary>"No config" = the page as it was before Story 4.1.</summary>
    private static void AssertNoConfig(string html)
    {
        Assert.DoesNotContain("sb-config", html);
        Assert.DoesNotContain("data-quote-request-form", html);
        Assert.DoesNotContain("lead-form.js", html);
        Assert.DoesNotContain(Disclaimer, Decode(html));
        Assert.DoesNotContain(ProductArchive.DefaultConfigTitle, Decode(html));
    }

    /// <summary>
    /// AC: no "Đặt mua", no <c>&lt;form action</c>, and the disclaimer
    /// appears exactly once.
    /// </summary>
    private static void AssertConfigPageRules(string html)
    {
        var decoded = Decode(html);
        Assert.DoesNotContain("Đặt mua", decoded);
        Assert.DoesNotMatch(new Regex(@"<form\b[^>]*\baction=", RegexOptions.IgnoreCase), html);
        Assert.Single(Regex.Matches(decoded, Regex.Escape(Disclaimer)));
    }

    private static string Rule(string css, string selector)
    {
        css = css.Replace("\r\n", "\n");
        var start = css.IndexOf(selector, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected CSS rule '{selector}'.");
        var end = css.IndexOf('}', start);
        return css[start..end];
    }

    /// <summary>Edits an archive in its own DI scope (see ProductDetailPageTests.UpdatePostAsync).</summary>
    private async Task UpdateArchiveAsync(Guid id, Action<ProductArchive> change)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var page = (await api.Pages.GetByIdAsync<ProductArchive>(id))!;
        change(page);
        await api.Pages.SaveAsync(page);
    }

    private async Task WithCatalogAsync(Func<IApi, Site, CatalogBuilder, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var builder = new CatalogBuilder(api, siteB);

        try
        {
            await body(api, siteB, builder);
        }
        finally
        {
            await builder.CleanupAsync();
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
