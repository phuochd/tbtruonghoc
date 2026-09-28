using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using Piranha.Extend;
using Piranha.Extend.Blocks;
using Piranha.Extend.Fields;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using Xunit;
using static TbTruongHoc.Web.Tests.ProductCatalogTests;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 4.2 (FR-11): the "Bảng cấu hình" block (<see cref="ConfigBlock"/>,
/// <c>Views/Cms/DisplayTemplates/ConfigBlock.cshtml</c>) on ProductArchive,
/// ProductPost and LandingPage. One test per I/O &amp; Edge-Case Matrix row
/// plus the ACs; the JS-only rows (summary, nothing selected, over length)
/// assert the markup that feeds the summary here and the script behaviour
/// in <see cref="ConfigBlockScriptTests"/>. Real HTTP render on Site B
/// against the MariaDB-backed app; each test builds its own throwaway pages
/// and deletes them afterwards. Replaces Story 4.1's DrumConfigReferenceTests.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class ConfigBlockTests
{
    private const string Disclaimer = "Bảng tham khảo, không tính giá tự động";

    private readonly PiranhaWebApplicationFactory _factory;

    public ConfigBlockTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Matrix: No block.
    [Fact]
    public async Task No_Block_Leaves_Archive_Pdp_And_Landing_As_Before()
    {
        await WithCatalogAsync(async (api, siteB, c, extra) =>
        {
            var hub = await c.HubAsync("Cfg Hub None");
            var archive = await c.ArchiveAsync(hub, "Trong chua none", 0);
            var post = await c.PostAsync(archive, "P1");
            var landing = await extra.LandingAsync("Cfg Landing none");

            var archiveHtml = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            AssertNoConfig(archiveHtml);
            Assert.DoesNotContain("data-quote-request-form", archiveHtml);
            Assert.DoesNotContain("lead-form.js", archiveHtml);
            Assert.Contains("data-sb-product-grid", archiveHtml);

            foreach (var permalink in new[] { post.Permalink, landing.Permalink })
            {
                var html = await GetHtmlAsync(permalink, HostnameOf(siteB));
                AssertNoConfig(html);
                Assert.Single(Regex.Matches(html, "data-quote-request-form"));
                Assert.Contains("lead-form.js", html);
            }
        });
    }

    // Matrix: Archive full. AC: one form, no Đặt mua/price/form action.
    [Fact]
    public async Task Archive_Full_Block_Renders_All_Types_In_Order_At_Its_Position_With_One_Prefilled_Form()
    {
        await WithCatalogAsync(async (api, siteB, c, extra) =>
        {
            var hub = await c.HubAsync("Cfg Hub Full");
            var archive = await c.ArchiveAsync(hub, "Trong chua full", 0);
            await c.PostAsync(archive, "P1");
            await UpdateAsync<ProductArchive>(archive.Id, a =>
            {
                a.Blocks.Add(new HtmlBlock { Body = "<p>Before marker</p>" });
                a.Blocks.Add(FullBlock("  Cấu hình trống chùa  "));
                a.Blocks.Add(new HtmlBlock { Body = "<p>After marker</p>" });
            });

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            var raw = ConfigSection(html);
            var config = Decode(raw);

            Assert.Contains("<h2 class=\"sb-config__title\" id=\"sb-config-title\">Cấu hình trống chùa</h2>", config);
            AssertFullRows(raw);
            Assert.Contains($"<p class=\"sb-config__disclaimer\">{Disclaimer}</p>", config);

            // Position: inline at the editor's block position, above the grid.
            var configAt = main.IndexOf("data-sb-config", StringComparison.Ordinal);
            Assert.True(main.IndexOf("Before marker", StringComparison.Ordinal) < configAt);
            Assert.True(configAt < main.IndexOf("After marker", StringComparison.Ordinal));
            Assert.True(configAt < main.IndexOf("data-sb-product-grid", StringComparison.Ordinal));

            // The block brings the page's only form, prefilled, formType general.
            Assert.Single(Regex.Matches(html, "data-quote-request-form"));
            Assert.Contains("data-quote-request-form", config);
            Assert.Contains($"name=\"productOfInterest\" value=\"Tư vấn cấu hình – {archive.Title}\"", config);
            Assert.Contains("name=\"formType\" value=\"general\"", config);
            // The block's controls sit outside the <form>.
            var form = Section(raw, "<form", "</form>");
            Assert.DoesNotContain("sb-config-", form);
            Assert.Contains("lead-form.js", html);
            Assert.Contains("sb-config.js", html);
            Assert.True(html.IndexOf("sb-config.js", StringComparison.Ordinal) < html.IndexOf("lead-form.js", StringComparison.Ordinal));

            AssertConfigPageRules(html);
        });
    }

    // Matrix: PDP / landing.
    [Fact]
    public async Task Pdp_And_Landing_Render_Block_In_Place_And_Feed_The_Existing_Form()
    {
        await WithCatalogAsync(async (api, siteB, c, extra) =>
        {
            var hub = await c.HubAsync("Cfg Hub Pdp");
            var archive = await c.ArchiveAsync(hub, "Trong chua pdp", 0);
            var post = await c.PostAsync(archive, "Trong 60");
            await UpdatePostAsync(post.Id, p =>
            {
                p.Blocks.Add(new HtmlBlock { Body = "<p>Before marker</p>" });
                p.Blocks.Add(FullBlock(null));
                p.Blocks.Add(new HtmlBlock { Body = "<p>After marker</p>" });
            });
            var landing = await extra.LandingAsync("Cfg Landing full");
            await UpdateAsync<LandingPage>(landing.Id, l =>
            {
                l.Blocks.Add(new HtmlBlock { Body = "<p>Before marker</p>" });
                l.Blocks.Add(FullBlock(null));
                l.Blocks.Add(new HtmlBlock { Body = "<p>After marker</p>" });
            });

            foreach (var (permalink, formMarker) in new[] { (post.Permalink, "sb-pdp__form"), (landing.Permalink, "id=\"dat-hang\"") })
            {
                var html = await GetHtmlAsync(permalink, HostnameOf(siteB));
                var main = Section(html, "<main", "</main>");
                var raw = ConfigSection(html);

                AssertFullRows(raw);
                var configAt = main.IndexOf("data-sb-config", StringComparison.Ordinal);
                Assert.True(main.IndexOf("Before marker", StringComparison.Ordinal) < configAt);
                Assert.True(configAt < main.IndexOf("After marker", StringComparison.Ordinal));
                Assert.True(configAt < main.IndexOf(formMarker, StringComparison.Ordinal));

                // No form of its own: the page's existing form is the only one.
                Assert.DoesNotContain("data-quote-request-form", raw);
                Assert.Single(Regex.Matches(html, "data-quote-request-form"));
                Assert.Contains("sb-config.js", html);
                Assert.Contains("lead-form.js", html);
                AssertConfigPageRules(html);
            }
        });
    }

    // Matrix: Invalid rows.
    [Fact]
    public async Task Invalid_Rows_Are_Omitted_Others_Render()
    {
        await WithCatalogAsync(async (api, siteB, c, extra) =>
        {
            var hub = await c.HubAsync("Cfg Hub Half");
            var archive = await c.ArchiveAsync(hub, "Trong chua half", 0);
            await UpdateAsync<ProductArchive>(archive.Id, a => a.Blocks.Add(Block(null,
                Row("   ", ConfigInputType.Text, null),
                Row("Sơn", ConfigInputType.Option, "Sơn son\nSơn mộc"),
                Row("Không có lựa chọn", ConfigInputType.Option, " \n  \n"),
                Row("Không có multi", ConfigInputType.MultiOption, null),
                Row("Chỉ nhãn", ConfigInputType.Display, "   "),
                Row("Vẽ mặt trống", ConfigInputType.Display, "Rồng, chữ"))));

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            var config = Decode(ConfigSection(html));

            Assert.Equal(2, Regex.Matches(config, "class=\"sb-config__row ").Count);
            Assert.Contains(">Sơn</label>", config);
            Assert.Contains(">Vẽ mặt trống</span>", config);
            Assert.DoesNotContain("Không có lựa chọn", config);
            Assert.DoesNotContain("Không có multi", config);
            Assert.DoesNotContain("Chỉ nhãn", config);
            AssertConfigPageRules(html);
        });
    }

    // Matrix: All invalid.
    [Fact]
    public async Task All_Rows_Invalid_Is_Treated_As_No_Block()
    {
        await WithCatalogAsync(async (api, siteB, c, extra) =>
        {
            var hub = await c.HubAsync("Cfg Hub Invalid");
            var archive = await c.ArchiveAsync(hub, "Trong chua invalid", 0);
            await c.PostAsync(archive, "P1");
            await UpdateAsync<ProductArchive>(archive.Id, a => a.Blocks.Add(Block("Tiêu đề không được hiện",
                Row("Chỉ nhãn", ConfigInputType.Display, null),
                Row(" ", ConfigInputType.Check, null),
                Row("Option rỗng", ConfigInputType.Option, ""))));

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            AssertNoConfig(html);
            Assert.DoesNotContain("data-quote-request-form", html);
            Assert.DoesNotContain("lead-form.js", html);
            Assert.DoesNotContain("Tiêu đề không được hiện", Decode(html));
            // No empty block wrapper either.
            Assert.DoesNotContain("config-block", html);
        });
    }

    // Matrix: Two blocks.
    [Fact]
    public async Task Only_The_First_Block_With_Valid_Rows_Renders()
    {
        await WithCatalogAsync(async (api, siteB, c, extra) =>
        {
            var hub = await c.HubAsync("Cfg Hub Two");
            var archive = await c.ArchiveAsync(hub, "Trong chua two", 0);
            await UpdateAsync<ProductArchive>(archive.Id, a =>
            {
                a.Blocks.Add(Block("Khối rỗng", Row("", ConfigInputType.Text, null)));
                a.Blocks.Add(Block("Khối một", Row("Loại", ConfigInputType.Option, "1\n2")));
                a.Blocks.Add(Block("Khối hai", Row("Sơn", ConfigInputType.Text, null)));
            });

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            var decoded = Decode(html);
            Assert.Single(Regex.Matches(html, "data-sb-config[ >]"));
            Assert.Contains("Khối một", decoded);
            Assert.DoesNotContain("Khối rỗng", decoded);
            Assert.DoesNotContain("Khối hai", decoded);
            Assert.Single(Regex.Matches(html, "class=\"block config-block\""));
            Assert.Single(Regex.Matches(html, "sb-config\\.js"));
            AssertConfigPageRules(html);
        });
    }

    // Matrix: Selections submitted (markup feeding sb-config.js).
    [Fact]
    public async Task Controls_Carry_The_Labels_And_Values_The_Summary_Is_Built_From()
    {
        await WithCatalogAsync(async (api, siteB, c, extra) =>
        {
            var hub = await c.HubAsync("Cfg Hub Sel");
            var archive = await c.ArchiveAsync(hub, "Trong chua sel", 0);
            await UpdateAsync<ProductArchive>(archive.Id, a => a.Blocks.Add(Block(null,
                Row("Kích thước", ConfigInputType.Option, "40cm\n60cm"),
                Row("Loại", ConfigInputType.Option, "1\n2\n3"),
                Row("Bánh xe", ConfigInputType.Check, null),
                Row("Sơn", ConfigInputType.Text, null),
                Row("Vẽ mặt trống", ConfigInputType.MultiOption, "Rồng\nChữ"))));

            var raw = ConfigSection(await GetHtmlAsync(archive.Permalink, HostnameOf(siteB)));
            var config = Decode(raw);

            Assert.Contains("data-sb-config-row=\"option\" data-label=\"Kích thước\"", config);
            Assert.Contains("data-sb-config-row=\"option\" data-label=\"Loại\"", config);
            Assert.Contains("data-sb-config-row=\"check\" data-label=\"Bánh xe\"", config);
            Assert.Contains("data-sb-config-row=\"text\" data-label=\"Sơn\"", config);
            Assert.Contains("data-sb-config-row=\"multi\" data-label=\"Vẽ mặt trống\"", config);
            Assert.Contains("<option value=\"60cm\">60cm</option>", config);
            Assert.Contains("<option value=\"2\">2</option>", config);
            Assert.Contains("type=\"checkbox\" id=\"sb-config-2\" name=\"sb-config-2\" value=\"Có\"", config);
            Assert.Contains("type=\"checkbox\" id=\"sb-config-4-1\" name=\"sb-config-4\" value=\"Chữ\"", config);
        });
    }

    // Matrix: HTML in fields.
    [Fact]
    public async Task Html_In_Heading_Label_And_Choice_Is_Encoded()
    {
        await WithCatalogAsync(async (api, siteB, c, extra) =>
        {
            var hub = await c.HubAsync("Cfg Hub Html");
            var archive = await c.ArchiveAsync(hub, "Trong chua html", 0);
            await UpdateAsync<ProductArchive>(archive.Id, a => a.Blocks.Add(Block("<i>tieu de</i>",
                Row("<b>x</b>", ConfigInputType.Option, "<script>alert(1)</script>\n<b>y</b>"),
                Row("<u>z</u>", ConfigInputType.Display, "<em>v</em>"))));

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            var config = ConfigSection(html);

            Assert.DoesNotContain("<b>x</b>", config);
            Assert.DoesNotContain("<i>tieu de</i>", config);
            Assert.DoesNotContain("<em>v</em>", config);
            Assert.DoesNotContain("<script>alert(1)</script>", html);
            Assert.Contains("&lt;b&gt;x&lt;/b&gt;", config);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", config);
            Assert.Contains("&lt;i&gt;tieu de&lt;/i&gt;", config);
            Assert.Contains("&lt;em&gt;v&lt;/em&gt;", config);
        });
    }

    // Matrix: Unsupported page.
    [Fact]
    public async Task Block_On_Unsupported_Pages_Renders_Nothing()
    {
        await WithCatalogAsync(async (api, siteB, c, extra) =>
        {
            var hub = await c.HubAsync("Cfg Hub Unsupported");
            await UpdateAsync<ProductHubPage>(hub.Id, h => h.Blocks.Add(Block("Không hiện", Row("Loại", ConfigInputType.Text, null))));
            var page = await extra.StandardPageAsync("Cfg Standard");
            await UpdateAsync<StandardPage>(page.Id, p => p.Blocks.Add(Block("Không hiện", Row("Loại", ConfigInputType.Text, null))));

            foreach (var permalink in new[] { hub.Permalink, page.Permalink })
            {
                var html = await GetHtmlAsync(permalink, HostnameOf(siteB));
                AssertNoConfig(html);
                Assert.DoesNotContain("Không hiện", Decode(html));
                // No empty block wrapper either.
                Assert.DoesNotContain("config-block", html);
            }
        });
    }

    // Matrix: Heading blank.
    [Fact]
    public async Task Blank_Heading_Defaults_And_Block_Renders_Without_Products()
    {
        await WithCatalogAsync(async (api, siteB, c, extra) =>
        {
            var hub = await c.HubAsync("Cfg Hub Heading");
            var archive = await c.ArchiveAsync(hub, "Trong chua heading", 0);
            await UpdateAsync<ProductArchive>(archive.Id, a => a.Blocks.Add(Block("   ", Row("Loại", ConfigInputType.Display, "Loại 1"))));

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            var config = Decode(ConfigSection(html));

            Assert.DoesNotContain("data-sb-product-grid", html);
            Assert.Equal("Bảng cấu hình", ConfigBlock.DefaultHeading);
            Assert.Contains("<h2 class=\"sb-config__title\" id=\"sb-config-title\">Bảng cấu hình</h2>", config);
            Assert.Single(Regex.Matches(html, "data-quote-request-form"));
            Assert.Contains("lead-form.js", html);
            AssertConfigPageRules(html);
        });
    }

    // Always: 375px fit, >=44px targets (markup/CSS check).
    [Fact]
    public async Task Controls_Are_Full_Width_Wrap_Anywhere_And_Have_44px_Targets()
    {
        var css = await _factory.CreateClient().GetStringAsync("/assets/css/site-b.css");
        var controls = Rule(css, ".sb-config__input,\n.sb-config__select {");
        Assert.Contains("width: 100%;", controls);
        Assert.Contains("min-height: 44px;", controls);
        Assert.Contains("min-height: 44px;", Rule(css, ".sb-config__row--check,\n.sb-config__choice {"));
        Assert.Contains("min-height: 44px;", Rule(css, "\n\n.sb-config__check-label {"));
        Assert.Contains("overflow-wrap: anywhere;", Rule(css, ".sb-config__label,\n.sb-config__value,\n.sb-config__check-label {"));
        Assert.DoesNotContain("white-space: nowrap", Rule(css, ".sb-config__panel {"));
        Assert.DoesNotContain(".sb-config__table", css);
    }

    // AC: Manager - block picker, 5 input types, row unlisted at top level.
    [Fact]
    public void Block_Group_And_Row_Are_Registered_For_The_Manager()
    {
        _ = _factory.Server; // app (and its registrations) started

        var group = App.Blocks.GetByType(typeof(ConfigBlock));
        Assert.NotNull(group);
        Assert.Equal("Bảng cấu hình", group!.Name);
        Assert.False(group.IsUnlisted);
        Assert.Contains(typeof(ConfigRowBlock), group.ItemTypes);

        var row = App.Blocks.GetByType(typeof(ConfigRowBlock));
        Assert.NotNull(row);
        Assert.Equal("Dòng cấu hình", row!.Name);
        Assert.True(row.IsUnlisted);
        Assert.True(row.IsGeneric);

        Assert.NotNull(App.Fields.GetByType(typeof(SelectField<ConfigInputType>)));
        var titles = Enum.GetValues<ConfigInputType>()
            .Select(v => typeof(ConfigInputType).GetField(v.ToString())!
                .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                .Cast<System.ComponentModel.DataAnnotations.DisplayAttribute>().Single().Description)
            .ToArray();
        Assert.Equal(new[] { "Text", "Chỉ hiển thị", "Option", "Check", "Multi-option" }, titles);
        // Text is the default, so a new row with only a label is a valid row.
        Assert.Equal(ConfigInputType.Text, default(ConfigInputType));
        Assert.Equal(ConfigInputType.Text, new ConfigRowBlock { Label = "Sơn" }.InputType.Value);
        Assert.NotNull(ConfigRow.From(new ConfigRowBlock { Label = "Sơn" }));
    }

    // --- helpers ---

    /// <summary>One valid row of each of the 5 types, in this order.</summary>
    private static ConfigBlock FullBlock(string? heading) => Block(heading,
        Row("Kích thước", ConfigInputType.Display, "Mặt 40 – 120 cm"),
        Row("  Sơn ", ConfigInputType.Text, null),
        Row("Loại", ConfigInputType.Option, " 1 \n\n2\n 3"),
        Row("Bánh xe", ConfigInputType.Check, null),
        Row("Vẽ mặt trống", ConfigInputType.MultiOption, "Rồng\r\nChữ"));

    /// <summary>The <see cref="FullBlock"/> rows: labelled, in editor order, choices trimmed.</summary>
    private static void AssertFullRows(string raw)
    {
        var config = Decode(raw);
        var types = Regex.Matches(config, "class=\"sb-config__row sb-config__row--([a-z]+)\"")
            .Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(new[] { "display", "text", "option", "check", "multi" }, types);

        Assert.Contains("<span class=\"sb-config__label\">Kích thước</span>", config);
        Assert.Contains("<span class=\"sb-config__value\">Mặt 40 – 120 cm</span>", config);
        Assert.Contains("<label class=\"sb-config__label\" for=\"sb-config-1\">Sơn</label>", config);
        Assert.Contains("<input class=\"sb-config__input\" type=\"text\" id=\"sb-config-1\" name=\"sb-config-1\"", config);
        Assert.Contains("<label class=\"sb-config__label\" for=\"sb-config-2\">Loại</label>", config);
        var options = Regex.Matches(Section(config, "<select", "</select>"), "<option value=\"([^\"]*)\">([^<]*)</option>")
            .Select(m => m.Groups[2].Value).ToArray();
        Assert.Equal(new[] { "— Chưa chọn —", "1", "2", "3" }, options);
        Assert.Contains("<option value=\"\">— Chưa chọn —</option>", config);
        Assert.Contains("<label class=\"sb-config__check-label\" for=\"sb-config-3\">Bánh xe</label>", config);
        Assert.Contains("<fieldset class=\"sb-config__row sb-config__row--multi\"", config);
        Assert.Contains("<legend class=\"sb-config__label\">Vẽ mặt trống</legend>", config);
        Assert.Contains("<label class=\"sb-config__check-label\" for=\"sb-config-4-0\">Rồng</label>", config);
        Assert.Contains("<label class=\"sb-config__check-label\" for=\"sb-config-4-1\">Chữ</label>", config);

        // Every control id has a <label for>.
        foreach (Match m in Regex.Matches(config, "<(?:input|select)[^>]*\\sid=\"(sb-config-[0-9-]+)\""))
        {
            Assert.Contains($"for=\"{m.Groups[1].Value}\"", config);
        }
        Assert.Single(Regex.Matches(config, Regex.Escape(Disclaimer)));
    }

    private static ConfigBlock Block(string? heading, params ConfigRowBlock[] rows)
    {
        var block = new ConfigBlock { Heading = heading };
        foreach (var row in rows)
        {
            block.Items.Add(row);
        }
        return block;
    }

    private static ConfigRowBlock Row(string? label, ConfigInputType type, string? choices) => new()
    {
        Label = label,
        InputType = new SelectField<ConfigInputType> { Value = type },
        Choices = choices
    };

    private static string ConfigSection(string html) =>
        Section(html, "<section class=\"sb-config\"", "</section>");

    /// <summary>"No block" = no config markup, script or disclaimer.</summary>
    private static void AssertNoConfig(string html)
    {
        Assert.DoesNotContain("sb-config", html);
        Assert.DoesNotContain(Disclaimer, Decode(html));
        Assert.DoesNotContain(">" + ConfigBlock.DefaultHeading + "<", Decode(html));
    }

    /// <summary>
    /// AC: no "Đặt mua", no price text from the block, no <c>&lt;form action</c>,
    /// exactly one quote form, and the disclaimer exactly once.
    /// </summary>
    private static void AssertConfigPageRules(string html)
    {
        var decoded = Decode(html);
        Assert.DoesNotContain("Đặt mua", decoded);
        Assert.DoesNotMatch(new Regex(@"<form\b[^>]*\baction=", RegexOptions.IgnoreCase), html);
        Assert.Single(Regex.Matches(html, "data-quote-request-form"));
        Assert.Single(Regex.Matches(decoded, Regex.Escape(Disclaimer)));
        var config = Decode(ConfigSection(html));
        Assert.DoesNotContain("price", config);
        Assert.DoesNotContain("Liên hệ báo giá", config);
    }

    private static string Rule(string css, string selector)
    {
        css = css.Replace("\r\n", "\n");
        var start = css.IndexOf(selector, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected CSS rule '{selector}'.");
        var end = css.IndexOf('}', start);
        return css[start..end];
    }

    /// <summary>Edits a page in its own DI scope (see ProductDetailPageTests.UpdatePostAsync).</summary>
    private async Task UpdateAsync<T>(Guid id, Action<T> change) where T : PageBase
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var page = (await api.Pages.GetByIdAsync<T>(id))!;
        change(page);
        await api.Pages.SaveAsync(page);
    }

    private async Task UpdatePostAsync(Guid id, Action<ProductPost> change)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var post = (await api.Posts.GetByIdAsync<ProductPost>(id))!;
        change(post);
        await api.Posts.SaveAsync(post);
    }

    private async Task WithCatalogAsync(Func<IApi, Site, CatalogBuilder, ExtraPages, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var builder = new CatalogBuilder(api, siteB);
        var extra = new ExtraPages(api, siteB);

        try
        {
            await body(api, siteB, builder, extra);
        }
        finally
        {
            await extra.CleanupAsync();
            await builder.CleanupAsync();
        }
    }

    /// <summary>Top-level Site B landing/standard pages, deleted on cleanup.</summary>
    private sealed class ExtraPages
    {
        private const int NonStartPageSortOrder = 1;
        private readonly IApi _api;
        private readonly Site _site;
        private readonly List<Guid> _pages = new();

        public ExtraPages(IApi api, Site site)
        {
            _api = api;
            _site = site;
        }

        public Task<LandingPage> LandingAsync(string title) => CreateAsync<LandingPage>(title, "cfg-landing");

        public Task<StandardPage> StandardPageAsync(string title) => CreateAsync<StandardPage>(title, "cfg-page");

        private async Task<T> CreateAsync<T>(string title, string slugPrefix) where T : Page<T>
        {
            var page = await _api.Pages.CreateAsync<T>();
            page.SiteId = _site.Id;
            page.SortOrder = NonStartPageSortOrder;
            page.Title = $"{title} {Guid.NewGuid().ToString("N")[..8]}";
            page.Slug = $"{slugPrefix}-{Guid.NewGuid():N}";
            page.Published = DateTime.Now.AddMinutes(-5);
            await _api.Pages.SaveAsync(page);
            _pages.Add(page.Id);
            return (await _api.Pages.GetByIdAsync<T>(page.Id))!;
        }

        public async Task CleanupAsync()
        {
            foreach (var id in Enumerable.Reverse(_pages))
            {
                await _api.Pages.DeleteAsync(id);
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
