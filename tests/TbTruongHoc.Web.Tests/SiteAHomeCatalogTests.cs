#nullable enable

using System;
using System.Collections.Generic;
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
using TbTruongHoc.Web.Services;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 6.2: Site A homepage featured-category grid + trust band, and the
/// aggregate "Danh mục sản phẩm" page (Site A's <see cref="ProductHubPage"/>
/// view). Pure helpers are unit-tested; rendering runs through the real app.
/// Homepage renders use a throwaway site (its own start page and hub), so
/// the shared dev DB's Site A sample hub can't change which hub is "first".
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class SiteAHomeCatalogTests
{
    private readonly PiranhaWebApplicationFactory _factory;

    public SiteAHomeCatalogTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // --- pure helpers ---

    private static List<CategoryTileModel> Tiles(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new CategoryTileModel($"Cat {i}", "", $"/c{i}") { Id = Guid.NewGuid() })
            .ToList();

    // Matrix: Home, 10 visible categories (selection part).
    [Fact]
    public void Featured_Without_Picks_Is_The_First_Six_Tiles()
    {
        var tiles = Tiles(10);
        var featured = SiteAHomePage.SelectFeatured(tiles, Array.Empty<Guid>());
        Assert.Equal(tiles.Take(6), featured);
    }

    // Matrix: Home, 3 visible (selection part).
    [Fact]
    public void Featured_With_Fewer_Tiles_Shows_Them_All()
    {
        var tiles = Tiles(3);
        Assert.Equal(tiles, SiteAHomePage.SelectFeatured(tiles, Array.Empty<Guid>()));
    }

    // Matrix: Home, no hub/tiles (selection part).
    [Fact]
    public void Featured_Of_No_Tiles_Is_Empty()
    {
        Assert.Empty(SiteAHomePage.SelectFeatured(Array.Empty<CategoryTileModel>(), new[] { Guid.NewGuid() }));
    }

    [Fact]
    public void Featured_Picks_Keep_Pick_Order_Skip_Unknown_And_Repeats_And_Cap_At_Six()
    {
        var tiles = Tiles(10);
        var picks = new[]
        {
            tiles[8].Id, Guid.NewGuid() /* hidden/empty: not a tile */, tiles[2].Id, tiles[8].Id,
            tiles[0].Id, tiles[1].Id, tiles[3].Id, tiles[4].Id, tiles[5].Id,
        };

        var featured = SiteAHomePage.SelectFeatured(tiles, picks);

        Assert.Equal(new[] { tiles[8], tiles[2], tiles[0], tiles[1], tiles[3], tiles[4] }, featured);
    }

    [Fact]
    public void Featured_With_Only_Invalid_Picks_Falls_Back_To_Hub_Order()
    {
        var tiles = Tiles(8);
        var featured = SiteAHomePage.SelectFeatured(tiles, new[] { Guid.NewGuid(), Guid.NewGuid() });
        Assert.Equal(tiles.Take(6), featured);
    }

    // Matrix: Groups.
    [Fact]
    public void Groups_Are_Trimmed_Deduped_Case_Insensitively_In_First_Seen_Order()
    {
        Assert.Equal(new[] { "Mầm non", "Ngoài trời" }, ProductArchive.SplitList(" Mầm non , Ngoài trời, ,mầm non,"));
        Assert.Empty(ProductArchive.SplitList(" , ,"));
        Assert.Empty(ProductArchive.SplitList(null!));

        var a = new CategoryTileModel("A", "", "/a") { Groups = ProductArchive.SplitList("Mầm non, Ngoài trời") };
        var b = new CategoryTileModel("B", "", "/b") { Groups = ProductArchive.SplitList("mầm non") };
        Assert.Equal(new[] { "Mầm non", "Ngoài trời" }, ProductHubPage.DistinctGroups(new[] { a, b }));
        Assert.Equal(ProductHubPage.GroupKey("Mầm non"), ProductHubPage.GroupKey(" mầm non"));

        // NFD (macOS paste) and NFC spellings are one group / one chip key.
        var nfd = "Mầm non".Normalize(System.Text.NormalizationForm.FormD);
        Assert.Equal(new[] { "Mầm non" }, ProductArchive.SplitList($"Mầm non, {nfd}"));
        Assert.Equal(ProductHubPage.GroupKey("Mầm non"), ProductHubPage.GroupKey(nfd));
    }

    // Matrix: Certification (parsing part).
    [Fact]
    public void Certifications_Keep_The_First_Two()
    {
        var archive = new ProductArchive { Certifications = "ASTM, CARB P2, TT 38" };
        Assert.Equal(new[] { "ASTM", "CARB P2" }, archive.CertificationList);
        Assert.Empty(new ProductArchive { Certifications = "  " }.CertificationList);
    }

    [Fact]
    public void Trust_Stats_Drop_Items_Missing_A_Number_Or_Label()
    {
        var home = new SiteAHomePage();
        home.TrustStats.Add(new TrustStat { Number = "500+", Label = "Trường" });
        home.TrustStats.Add(new TrustStat { Number = " ", Label = "Không số" });
        home.TrustStats.Add(new TrustStat { Number = "20 năm", Label = "" });
        Assert.Equal(new[] { "500+" }, home.TrustStatItems.Select(s => s.NumberText));
    }

    // --- homepage renders (throwaway site) ---

    // Matrix: Home, 10 visible categories.
    [Fact]
    public async Task Home_With_Ten_Categories_Shows_Six_Tiles_View_All_Ten_Then_Band()
    {
        await WithHomeSiteAsync(async (api, site, home, hostname) =>
        {
            var hub = await CreateHubAsync(api, site, "Sản phẩm");
            var archives = new List<ProductArchive>();
            for (var i = 0; i < 10; i++)
            {
                archives.Add(await CreateArchiveAsync(api, hub, $"Nhóm {i}", i, withPost: true));
            }
            var empty = await CreateArchiveAsync(api, hub, "Nhóm rỗng", 10, withPost: false);

            // Certifications are aggregate-only: a featured tile never shows them.
            archives[0].Certifications = "ASTM";
            await api.Pages.SaveAsync(archives[0]);

            home.TrustStats.Add(new TrustStat { Number = "500+", Label = "Trường đã lắp đặt" });
            home.TrustStats.Add(new TrustStat { Number = "20 năm", Label = "Kinh nghiệm" });
            await api.Pages.SaveAsync(home);

            var html = Decode(await GetHtmlAsync("/", hostname));
            var featured = Section(html, "<section class=\"sa-featured\"", "</section>");
            var grid = Section(featured, "<ul class=\"sa-cat-grid sa-cat-grid--home\">", "</ul>");

            Assert.Equal(6, Count(grid, "class=\"sa-tile\""));
            for (var i = 0; i < 6; i++)
            {
                Assert.Contains($"href=\"{archives[i].Permalink}\"", grid);
            }
            Assert.DoesNotContain(archives[6].Permalink, grid);
            Assert.DoesNotContain(empty.Permalink, featured);
            Assert.Contains($"<a class=\"sa-featured__all\" href=\"{hub.Permalink}\">Xem tất cả 10 nhóm sản phẩm →</a>", featured);
            Assert.Contains("<h3 class=\"sa-tile__title\">", grid);
            Assert.DoesNotContain("sa-tile__certs", grid);
            Assert.DoesNotContain("ASTM", grid);

            // Band after the grid, number over label, in order.
            var band = Section(html, "<section class=\"sa-trust\"", "</section>");
            Assert.True(html.IndexOf("sa-trust\"", StringComparison.Ordinal) > html.IndexOf("sa-featured\"", StringComparison.Ordinal));
            Assert.Matches(new Regex("sa-trust__num\">500\\+</span>\\s*<span class=\"sa-trust__label\">Trường đã lắp đặt</span>[\\s\\S]*20 năm"), band);
        });
    }

    // Matrix: Home, 3 visible.
    [Fact]
    public async Task Home_With_Three_Categories_And_Picks_Shows_Picked_Order()
    {
        await WithHomeSiteAsync(async (api, site, home, hostname) =>
        {
            var hub = await CreateHubAsync(api, site, "Sản phẩm");
            var one = await CreateArchiveAsync(api, hub, "Một", 0, withPost: true);
            var two = await CreateArchiveAsync(api, hub, "Hai", 1, withPost: true);
            var three = await CreateArchiveAsync(api, hub, "Ba", 2, withPost: true);

            var html = Decode(await GetHtmlAsync("/", hostname));
            var featured = Section(html, "<section class=\"sa-featured\"", "</section>");
            Assert.Equal(3, Count(featured, "class=\"sa-tile\""));
            Assert.Contains("Xem tất cả 3 nhóm sản phẩm →", featured);
            // No stats -> no band.
            Assert.DoesNotContain("class=\"sa-trust\"", html);

            // Editor picks: order follows the picks.
            home.FeaturedCategories.Add(three.Id);
            home.FeaturedCategories.Add(one.Id);
            await api.Pages.SaveAsync(home);

            html = Decode(await GetHtmlAsync("/", hostname));
            featured = Section(html, "<section class=\"sa-featured\"", "</section>");
            Assert.Equal(2, Count(featured, "class=\"sa-tile\""));
            Assert.True(featured.IndexOf(three.Permalink, StringComparison.Ordinal) < featured.IndexOf(one.Permalink, StringComparison.Ordinal));
            Assert.DoesNotContain($"href=\"{two.Permalink}\"", featured);
            Assert.Contains("Xem tất cả 3 nhóm sản phẩm →", featured);
        });
    }

    [Fact]
    public async Task Home_Uses_The_First_Visible_Hub_In_Sitemap_Order()
    {
        await WithHomeSiteAsync(async (api, site, home, hostname) =>
        {
            var hidden = await CreateHubAsync(api, site, "Ẩn", sortOrder: 1, slug: "hub-an", isHidden: true);
            await CreateArchiveAsync(api, hidden, "Ẩn cat", 0, withPost: true);
            var first = await CreateHubAsync(api, site, "Sản phẩm", sortOrder: 2);
            var firstCat = await CreateArchiveAsync(api, first, "Một", 0, withPost: true);
            var second = await CreateHubAsync(api, site, "Khác", sortOrder: 3, slug: "hub-khac");
            await CreateArchiveAsync(api, second, "Khác cat", 0, withPost: true);
            await CreateArchiveAsync(api, second, "Khác cat 2", 1, withPost: true);

            var html = Decode(await GetHtmlAsync("/", hostname));
            var featured = Section(html, "<section class=\"sa-featured\"", "</section>");
            Assert.Contains($"href=\"{firstCat.Permalink}\"", featured);
            Assert.Equal(1, Count(featured, "class=\"sa-tile\""));
            Assert.Contains($"<a class=\"sa-featured__all\" href=\"{first.Permalink}\">Xem tất cả 1 nhóm sản phẩm →</a>", featured);
        });
    }

    // Matrix: Home, no hub/tiles.
    [Fact]
    public async Task Home_Without_Hub_Tiles_Omits_The_Section_But_Keeps_The_Band()
    {
        await WithHomeSiteAsync(async (api, site, home, hostname) =>
        {
            var hub = await CreateHubAsync(api, site, "Sản phẩm");
            await CreateArchiveAsync(api, hub, "Rỗng", 0, withPost: false);
            home.TrustStats.Add(new TrustStat { Number = "24/7", Label = "Hỗ trợ" });
            await api.Pages.SaveAsync(home);

            var html = Decode(await GetHtmlAsync("/", hostname));
            Assert.DoesNotContain("sa-featured", html);
            Assert.Contains("<span class=\"sa-trust__num\">24/7</span>", html);
        });
    }

    // --- aggregate page renders (real Site A) ---

    // Matrix: Aggregate, 10 tiles; Groups; Certification.
    [Fact]
    public async Task Aggregate_Lists_Every_Visible_Category_Chips_Certs_And_Placeholder_Last()
    {
        await WithSiteACatalogAsync(async (api, siteA, c) =>
        {
            var hub = await c.HubAsync("Danh mục sản phẩm");
            var archives = new List<ProductArchive>();
            for (var i = 0; i < 10; i++)
            {
                var archive = await c.ArchiveAsync(hub, $"Agg {i}", i, excerpt: $"Mô tả {i}");
                await c.PostAsync(archive, $"P{i}");
                archives.Add(archive);
            }
            var empty = await c.ArchiveAsync(hub, "Agg Empty", 10);

            archives[0].FilterGroups = "Mầm non, Ngoài trời";
            archives[0].Certifications = "ASTM, CARB P2, TT 38";
            await api.Pages.SaveAsync(archives[0]);
            archives[1].FilterGroups = "mầm non";
            await api.Pages.SaveAsync(archives[1]);

            var mediaId = await UploadPngAsync(api);
            archives[2].PrimaryImage = mediaId;
            await api.Pages.SaveAsync(archives[2]);

            var html = await GetHtmlAsync(hub.Permalink, HostnameOf(siteA));
            var main = Decode(Section(html, "<main class=\"sa-catalog\">", "</main>"));

            // Breadcrumb + header.
            var crumb = Section(main, "<nav class=\"sa-breadcrumb", "</nav>");
            Assert.Contains("aria-label=\"Breadcrumb\"", crumb);
            Assert.Contains(">Trang chủ</a>", crumb);
            Assert.Contains($"<span aria-current=\"page\">{hub.Title}</span>", crumb);
            Assert.Contains($"<h1>{hub.Title}</h1>", main);

            // 10 tiles in sitemap order, empty one absent, placeholder last.
            var grid = Section(main, "<ul class=\"sa-cat-grid sa-cat-grid--all\">", "data-sa-cat-empty");
            Assert.Equal(10, Count(grid, "data-sa-cat-tile"));
            var positions = archives.Select(a => grid.IndexOf($"href=\"{a.Permalink}\"", StringComparison.Ordinal)).ToList();
            Assert.True(positions.All(p => p >= 0) && positions.SequenceEqual(positions.OrderBy(p => p)), "Tiles follow sitemap order.");
            Assert.DoesNotContain(empty.Permalink, main);
            var placeholderAt = grid.IndexOf("sa-cat-grid__placeholder", StringComparison.Ordinal);
            Assert.True(placeholderAt > positions.Max(), "Placeholder is last.");
            Assert.Contains("Chưa thấy thiết bị bạn cần?", grid[placeholderAt..]);
            Assert.Contains("<h2 class=\"sa-tile__title\">", grid);

            // The live search reads title + excerpt from each tile.
            Assert.Contains($"data-sa-search=\"{archives[0].Title} Mô tả 0\"", grid);

            // Primary image -> <img> in the icon block; none -> empty teal square.
            Assert.Matches(new Regex($"href=\"{Regex.Escape(archives[2].Permalink)}\">\\s*<span class=\"sa-tile__icon\">\\s*<img src=\"[^\"]+\" alt=\"\""), grid);
            Assert.Matches(new Regex($"href=\"{Regex.Escape(archives[3].Permalink)}\">\\s*<span class=\"sa-tile__icon\">\\s*</span>"), grid);

            // Placeholder contact link follows Site A's SiteSettings: tel:, else Zalo (new tab), else no link.
            var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(siteA.Id);
            var tel = ContactLinks.TelHref(settings?.Phone?.Value);
            var zalo = ContactLinks.SafeUrl(settings?.ZaloUrl?.Value);
            var placeholder = grid[placeholderAt..];
            if (tel != null)
            {
                Assert.Contains($"<a class=\"sa-tile sa-tile--placeholder\" href=\"tel:{tel}\">", placeholder);
            }
            else if (zalo != null)
            {
                Assert.Contains($"<a class=\"sa-tile sa-tile--placeholder\" href=\"{zalo}\" target=\"_blank\" rel=\"noopener\">", placeholder);
            }
            else
            {
                Assert.Contains("<div class=\"sa-tile sa-tile--placeholder\">", placeholder);
            }

            // Chips: distinct groups, first spelling; tile groups as keys.
            var chips = Section(main, "<div class=\"sa-catsearch__chips\"", "</div>");
            Assert.Equal(2, Count(chips, "class=\"sa-chip\""));
            Assert.Contains("aria-pressed=\"false\" data-sa-cat-chip=\"mầm non\">Mầm non</button>", chips);
            Assert.Contains("data-sa-cat-chip=\"ngoài trời\">Ngoài trời</button>", chips);
            Assert.Contains("data-sa-groups=\"[\"mầm non\",\"ngoài trời\"]\"", Regex.Unescape(main));

            // Certs: first two, only on the tile that has them.
            var certs = Section(main, "<ul class=\"sa-tile__certs\"", "</ul>");
            Assert.Contains("<li class=\"sa-badge\">ASTM</li>", certs);
            Assert.Contains("<li class=\"sa-badge\">CARB P2</li>", certs);
            Assert.DoesNotContain("TT 38", main);
            Assert.Equal(1, Count(main, "sa-tile__certs"));

            // Search input + live region.
            Assert.Contains("data-sa-cat-search", main);
            Assert.Contains("role=\"status\" aria-live=\"polite\" data-sa-cat-empty", main);
            Assert.Contains("data-message=\"Không tìm thấy nhóm sản phẩm phù hợp.\"", main);
            Assert.DoesNotContain("sb-", main);
        });
    }

    [Fact]
    public async Task Aggregate_Without_Groups_Has_No_Chip_Row()
    {
        await WithSiteACatalogAsync(async (api, siteA, c) =>
        {
            var hub = await c.HubAsync("Hub No Groups");
            var archive = await c.ArchiveAsync(hub, "Plain", 0);
            await c.PostAsync(archive, "P");

            var main = Decode(Section(await GetHtmlAsync(hub.Permalink, HostnameOf(siteA)), "<main class=\"sa-catalog\">", "</main>"));
            Assert.Contains("data-sa-cat-search", main);
            Assert.DoesNotContain("sa-catsearch__chips", main);
            Assert.DoesNotContain("sa-tile__certs", main);
        });
    }

    // Matrix: Site B hub.
    [Fact]
    public async Task Site_B_Hub_Keeps_Its_Own_View()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await ProductCatalogTests.GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var c = new ProductCatalogTests.CatalogBuilder(api, siteB);
        try
        {
            var hub = await c.HubAsync("Hub B");
            var archive = await c.ArchiveAsync(hub, "B Cat", 0);
            await c.PostAsync(archive, "P");
            await api.Pages.SaveAsync(await SetAsync(api, archive.Id, a => a.FilterGroups = "Nhóm"));

            var html = await GetHtmlAsync(hub.Permalink, HostnameOf(siteB));
            Assert.Contains("<main class=\"sb-catalog\">", html);
            Assert.Contains("<ul class=\"sb-grid\" data-sb-tile-grid>", html);
            Assert.DoesNotContain("sa-catsearch", html);
            Assert.DoesNotContain("sa-cat-grid", html);
            Assert.DoesNotContain("sa-breadcrumb", html);
        }
        finally
        {
            await c.CleanupAsync();
        }
    }

    // --- helpers ---

    // 220x90 solid PNG (same as SiteAShellTests / BlogListingTests).
    private static readonly byte[] WidePng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAANwAAABaCAIAAABc0a8TAAAAwElEQVR42u3SQQ0AAAjEsPODIyxhmuCCR5MqWJbpglciAaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMaUKmBJMiSnBlJgSTIkpwZRgSkwJpsSUYEpMCabElGBKMCWmBFNiSjAlpgRTgikxJZgSU4IpMSWYElOCKcGUmBJMiSnBlJgSTAmmxJRgSkwJpsSUYEpMCaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMSWYEkyJKcGUmBJMiSnBlHBTLjqcMk/EWR93AAAAAElFTkSuQmCC");

    private readonly List<Guid> _media = new();

    private async Task<Guid> UploadPngAsync(IApi api)
    {
        using var stream = new System.IO.MemoryStream(WidePng);
        var content = new StreamMediaContent { Filename = $"sa-tile-test-{Guid.NewGuid():N}.png", Data = stream };
        await api.Media.SaveAsync(content);
        _media.Add(content.Id!.Value);
        return content.Id!.Value;
    }

    private async Task DeleteUploadedMediaAsync()
    {
        // Fresh scope: the render may have added resized versions through another DbContext.
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        foreach (var id in _media)
        {
            await api.Media.DeleteAsync(id);
        }
        _media.Clear();
    }

    private static async Task<ProductArchive> SetAsync(IApi api, Guid id, Action<ProductArchive> mutate)
    {
        var archive = (await api.Pages.GetByIdAsync<ProductArchive>(id))!;
        mutate(archive);
        return archive;
    }

    private static async Task<ProductHubPage> CreateHubAsync(IApi api, Site site, string title,
        int sortOrder = 1, string slug = "san-pham", bool isHidden = false)
    {
        var hub = await api.Pages.CreateAsync<ProductHubPage>();
        hub.SiteId = site.Id;
        hub.SortOrder = sortOrder;
        hub.Title = title;
        hub.Slug = slug;
        hub.IsHidden = isHidden;
        hub.Published = DateTime.Now.AddMinutes(-5);
        await api.Pages.SaveAsync(hub);
        return (await api.Pages.GetByIdAsync<ProductHubPage>(hub.Id))!;
    }

    private static async Task<ProductArchive> CreateArchiveAsync(IApi api, PageBase hub, string title, int sortOrder, bool withPost)
    {
        var archive = await api.Pages.CreateAsync<ProductArchive>();
        archive.SiteId = hub.SiteId;
        archive.ParentId = hub.Id;
        archive.SortOrder = sortOrder;
        archive.Title = title;
        archive.Slug = $"{hub.Slug}/cat-{sortOrder}";
        archive.Published = DateTime.Now.AddMinutes(-5);
        await api.Pages.SaveAsync(archive);

        if (withPost)
        {
            var post = await api.Posts.CreateAsync<ProductPost>();
            post.BlogId = archive.Id;
            post.Category = "General";
            post.Title = $"{title} post";
            post.Slug = $"post-{Guid.NewGuid():N}";
            post.Published = DateTime.Now.AddMinutes(-1);
            await api.Posts.SaveAsync(post);
        }

        return (await api.Pages.GetByIdAsync<ProductArchive>(archive.Id))!;
    }

    /// <summary>
    /// A throwaway site with a published <see cref="SiteAHomePage"/> as its
    /// start page; everything is deleted afterwards.
    /// </summary>
    private async Task WithHomeSiteAsync(Func<IApi, Site, SiteAHomePage, string, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var hostname = $"sa-home-{suffix}.local";
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = $"sa-home-{suffix}",
            Title = $"Site A Home {suffix}",
            Hostnames = hostname,
            IsDefault = false
        };

        try
        {
            await api.Sites.SaveAsync(site);

            var home = await api.Pages.CreateAsync<SiteAHomePage>();
            home.SiteId = site.Id;
            home.SortOrder = 0;
            home.Title = "Trang chủ";
            home.Slug = "trang-chu";
            home.Published = DateTime.Now.AddMinutes(-5);
            await api.Pages.SaveAsync(home);

            await body(api, site, (await api.Pages.GetByIdAsync<SiteAHomePage>(home.Id))!, hostname);
        }
        finally
        {
            await DeleteSiteAsync(api, site.Id);
        }
    }

    private static async Task DeleteSiteAsync(IApi api, Guid siteId)
    {
        async Task DeleteAsync(SitemapItem item)
        {
            foreach (var child in item.Items)
            {
                await DeleteAsync(child);
            }
            foreach (var post in await api.Posts.GetAllAsync<PostInfo>(item.Id))
            {
                await api.Posts.DeleteAsync(post.Id);
            }
            await api.Pages.DeleteAsync(item.Id);
        }

        foreach (var top in await api.Sites.GetSitemapAsync(siteId, onlyPublished: false))
        {
            await DeleteAsync(top);
        }
        if (await api.Sites.GetByIdAsync(siteId) != null)
        {
            await api.Sites.DeleteAsync(siteId);
        }
    }

    private async Task WithSiteACatalogAsync(Func<IApi, Site, ProductCatalogTests.CatalogBuilder, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await ProductCatalogTests.GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var builder = new ProductCatalogTests.CatalogBuilder(api, siteA);

        try
        {
            await body(api, siteA, builder);
        }
        finally
        {
            await builder.CleanupAsync();
            await DeleteUploadedMediaAsync();
        }
    }

    private static string Section(string html, string start, string end) => ProductCatalogTests.Section(html, start, end);

    private static string Decode(string html) => ProductCatalogTests.Decode(html);

    private static string HostnameOf(Site site) => ProductCatalogTests.HostnameOf(site);

    private static int Count(string haystack, string needle) =>
        Regex.Matches(haystack, Regex.Escape(needle)).Count;

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
