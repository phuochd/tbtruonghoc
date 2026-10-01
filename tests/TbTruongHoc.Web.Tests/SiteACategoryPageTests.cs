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
using Piranha.Extend.Blocks;
using Piranha.Extend.Fields;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using TbTruongHoc.Web.Services;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 6.3: Site A category pages (Site A's <see cref="ProductArchive"/>
/// view) and the <see cref="SiteACatalogSeed"/>. Renders run on the real
/// Site A (shared dev DB: asserts are scoped to the test's own pages);
/// seed tests run on a throwaway site via the internal overload.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class SiteACategoryPageTests
{
    private readonly PiranhaWebApplicationFactory _factory;

    public SiteACategoryPageTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Matrix: Category with products.
    [Fact]
    public async Task Category_Renders_Breadcrumb_Header_Blocks_And_Published_Cards_Only()
    {
        await WithSiteAAsync(async (api, siteA, c) =>
        {
            var hub = await c.HubAsync("Sản phẩm");
            var marker = $"sa-archive-block-{c.Suffix}";
            var archive = await CreateArchiveWithConfigBlockAsync(siteA, hub, c, $"<p>{marker}</p>");
            var one = await c.PostAsync(archive, "One", publishedAt: DateTime.Now.AddMinutes(-10));
            var two = await c.PostAsync(archive, "Two", publishedAt: DateTime.Now.AddMinutes(-5));
            var draft = await c.PostAsync(archive, "Draft", published: false);

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteA));
            var main = Decode(Section(html, "<main class=\"sa-catalog\">", "</main>"));

            // Breadcrumb: Trang chủ / hub (link) / category (plain).
            var crumb = Section(main, "<nav class=\"sa-breadcrumb", "</nav>");
            Assert.Contains("aria-label=\"Breadcrumb\"", crumb);
            Assert.Matches(new Regex(
                $"<a href=\"/\">Trang chủ</a>\\s*<span class=\"sa-breadcrumb__sep\" aria-hidden=\"true\">/</span>\\s*" +
                $"<a href=\"{Regex.Escape(hub.Permalink)}\">{Regex.Escape(hub.Title)}</a>\\s*<span class=\"sa-breadcrumb__sep\" aria-hidden=\"true\">/</span>\\s*" +
                $"<span aria-current=\"page\">{Regex.Escape(archive.Title)}</span>"), crumb);

            // Breadcrumb directly before the header, then blocks, then the grid.
            Assert.Contains($"<h1>{archive.Title}</h1>", main);
            Assert.Contains("<p class=\"sa-catalog__lead\">Mô tả danh mục</p>", main);
            var headerAt = main.IndexOf("sa-catalog__header", StringComparison.Ordinal);
            var blockAt = main.IndexOf(marker, StringComparison.Ordinal);
            var gridAt = main.IndexOf("sa-product-grid", StringComparison.Ordinal);
            Assert.True(main.IndexOf("sa-breadcrumb", StringComparison.Ordinal) < headerAt && headerAt < blockAt && blockAt < gridAt);

            // ConfigBlock skipped on Site A.
            Assert.DoesNotContain("sb-config", html);
            Assert.DoesNotContain("sb-config.js", html);

            var cards = Cards(html);
            Assert.Equal(2, cards.Count);
            Assert.Contains(cards, k => k.Contains(one.Permalink));
            Assert.Contains(cards, k => k.Contains(two.Permalink));
            Assert.DoesNotContain(draft.Title, html);
            Assert.DoesNotContain("sa-pager", html);

            // No Site B markup, no closing contact band / trust block.
            Assert.DoesNotContain("class=\"sb-", main);
            Assert.DoesNotContain("sb-trust", html);

            // Archive order (newest first): Two before One.
            Assert.True(main.IndexOf(two.Permalink, StringComparison.Ordinal) < main.IndexOf(one.Permalink, StringComparison.Ordinal));
        });
    }

    // Matrix: 13 products.
    [Fact]
    public async Task Thirteen_Products_Page_Twelve_Then_One_With_Pager()
    {
        await WithSiteAAsync(async (api, siteA, c) =>
        {
            var hub = await c.HubAsync("Hub Paging");
            var archive = await c.ArchiveAsync(hub, "Paging", 0);
            for (var i = 0; i < 13; i++)
            {
                await c.PostAsync(archive, $"Item {i:D2}", publishedAt: DateTime.Now.AddMinutes(-20 + i));
            }

            var page1 = await GetHtmlAsync(archive.Permalink, HostnameOf(siteA));
            Assert.Equal(12, Cards(page1).Count);
            var pager1 = Decode(Section(page1, "<nav class=\"sa-pager\"", "</nav>"));
            Assert.Contains("aria-label=\"Phân trang\"", pager1);
            Assert.Contains($"href=\"{archive.Permalink}/page/2\" rel=\"next\">Trang sau</a>", pager1);
            Assert.Contains("Trang 1 / 2", pager1);
            Assert.DoesNotContain("Trang trước", pager1);

            var page2 = await GetHtmlAsync($"{archive.Permalink}/page/2", HostnameOf(siteA));
            Assert.Single(Cards(page2));
            var pager2 = Decode(Section(page2, "<nav class=\"sa-pager\"", "</nav>"));
            Assert.Contains($"href=\"{archive.Permalink}\" rel=\"prev\">Trang trước</a>", pager2);
            Assert.Contains("Trang 2 / 2", pager2);
            Assert.DoesNotContain("Trang sau", pager2);
        });
    }

    // Matrix: Price blank vs set; No image.
    [Fact]
    public async Task Card_Is_One_Link_With_Encoded_Price_Fallback_And_Image_Or_Placeholder()
    {
        await WithSiteAAsync(async (api, siteA, c) =>
        {
            var hub = await c.HubAsync("Hub Cards");
            var archive = await c.ArchiveAsync(hub, "Cards", 0);
            const string price = "từ 2.500.000đ";
            var priced = await c.PostAsync(archive, "Priced", price: price);
            var unpriced = await c.PostAsync(archive, "Unpriced", price: "   ");
            var injected = await c.PostAsync(archive, "Injected", price: "<script>alert(1)</script>");

            var withImage = await c.PostAsync(archive, "Photo");
            var mediaId = await UploadPngAsync(api);
            var photo = (await api.Posts.GetByIdAsync<ProductPost>(withImage.Id))!;
            photo.PrimaryImage = mediaId;
            await api.Posts.SaveAsync(photo);

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteA));
            var cards = Cards(html);
            Assert.Equal(4, cards.Count);

            string CardOf(ProductPost post) => cards.Single(k => k.Contains($"href=\"{post.Permalink}\""));

            var pricedCard = Decode(CardOf(priced));
            Assert.Contains($">{price}</span>", pricedCard);
            Assert.Contains("class=\"sa-pcard__price\"", pricedCard);
            Assert.DoesNotContain("Liên hệ để nhận báo giá", pricedCard);

            var unpricedCard = Decode(CardOf(unpriced));
            Assert.Contains("class=\"sa-pcard__price sa-pcard__price--contact\"", unpricedCard);
            Assert.Contains(">Liên hệ để nhận báo giá</span>", unpricedCard);

            var injectedRaw = CardOf(injected);
            Assert.DoesNotContain("<script>", injectedRaw);
            Assert.Contains("&lt;script&gt;", injectedRaw);

            foreach (var (raw, post) in new[] { (CardOf(priced), priced), (CardOf(unpriced), unpriced), (CardOf(withImage), withImage) })
            {
                var card = Decode(raw);
                // Exactly one link, over the whole card, named by the title; no button.
                Assert.Single(Regex.Matches(card, "<a "));
                Assert.StartsWith($"<a class=\"sa-pcard\" href=\"{post.Permalink}\"", card);
                var titleId = Regex.Match(card, "aria-labelledby=\"([^\"]+)\"").Groups[1].Value;
                Assert.Contains($"<h2 class=\"sa-pcard__title\" id=\"{titleId}\">{post.Title}</h2>", card);
                Assert.DoesNotContain("<button", card);
                Assert.DoesNotContain("Giảm giá", card);
            }

            // No image -> plain teal block, no <img>.
            var noImage = Decode(CardOf(priced));
            Assert.Contains("<div class=\"sa-pcard__media sa-pcard__media--empty\"></div>", noImage);
            Assert.DoesNotContain("<img", noImage);

            // Image -> <img> with alt = product title.
            var imageCard = Decode(CardOf(withImage));
            Assert.Matches(new Regex($"<div class=\"sa-pcard__media\">\\s*<img src=\"[^\"]+\" alt=\"{Regex.Escape(withImage.Title)}\""), imageCard);
            Assert.DoesNotContain("sa-pcard__media--empty", imageCard);
        });
    }

    // Matrix: Empty category by URL.
    [Fact]
    public async Task Empty_Category_Renders_Header_And_Blocks_Only()
    {
        await WithSiteAAsync(async (api, siteA, c) =>
        {
            var hub = await c.HubAsync("Hub Empty");
            var marker = $"sa-empty-block-{c.Suffix}";
            var archive = await c.ArchiveAsync(hub, "Nothing", 0, blockHtml: $"<p>{marker}</p>");
            await c.PostAsync(archive, "Draft only", published: false);

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteA));
            var main = Decode(Section(html, "<main class=\"sa-catalog\">", "</main>"));
            Assert.Contains($"<h1>{archive.Title}</h1>", main);
            Assert.Contains(marker, main);
            Assert.DoesNotContain("sa-product-grid", main);
            Assert.DoesNotContain("sa-pcard", main);
            Assert.DoesNotContain("sa-pager", main);
            Assert.DoesNotContain("Không có", main);
        });
    }

    // Matrix: Top-level category.
    [Fact]
    public async Task Category_Without_Hub_Parent_Has_Two_Step_Breadcrumb()
    {
        await WithSiteAAsync(async (api, siteA, c) =>
        {
            var archive = await api.Pages.CreateAsync<ProductArchive>();
            archive.SiteId = siteA.Id;
            archive.ParentId = null;
            archive.SortOrder = 50;
            archive.Title = $"Top Cat {c.Suffix}";
            archive.Slug = $"sa-top-cat-{c.Suffix}";
            archive.IsHidden = true;
            archive.Published = DateTime.Now.AddMinutes(-5);
            await api.Pages.SaveAsync(archive);
            c.Track(archive.Id);
            var loaded = (await api.Pages.GetByIdAsync<ProductArchive>(archive.Id))!;
            await c.PostAsync(loaded, "Top post");

            var html = await GetHtmlAsync(loaded.Permalink, HostnameOf(siteA));
            var crumb = Decode(Section(html, "<nav class=\"sa-breadcrumb", "</nav>"));
            Assert.Single(Regex.Matches(crumb, "<a "));
            Assert.Matches(new Regex(
                $"<a href=\"/\">Trang chủ</a>\\s*<span class=\"sa-breadcrumb__sep\" aria-hidden=\"true\">/</span>\\s*" +
                $"<span aria-current=\"page\">{Regex.Escape(loaded.Title)}</span>"), crumb);
            Assert.Single(Cards(html));
        });
    }

    [Fact]
    public async Task Category_Under_Unpublished_Hub_Has_Two_Step_Breadcrumb()
    {
        await WithSiteAAsync(async (api, siteA, c) =>
        {
            var hub = await c.HubAsync("Hub Draft", published: false);
            var archive = await c.ArchiveAsync(hub, "Under Draft", 0);
            await c.PostAsync(archive, "P");

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteA));
            AssertTwoStepBreadcrumb(html, archive.Title, hub.Permalink);
        });
    }

    [Fact]
    public async Task Category_Under_Non_Hub_Parent_Has_Two_Step_Breadcrumb()
    {
        await WithSiteAAsync(async (api, siteA, c) =>
        {
            var parent = await api.Pages.CreateAsync<StandardPage>();
            parent.SiteId = siteA.Id;
            parent.SortOrder = 51;
            parent.Title = $"Plain Parent {c.Suffix}";
            parent.Slug = $"sa-plain-parent-{c.Suffix}";
            parent.IsHidden = true;
            parent.Published = DateTime.Now.AddMinutes(-5);
            await api.Pages.SaveAsync(parent);
            var parentInfo = (await api.Pages.GetByIdAsync<PageInfo>(parent.Id))!;
            // Deleted after the builder's own pages (its child archive).
            var archive = await c.ArchiveAsync(parentInfo, "Under Plain", 0);
            c.TrackLast(parent.Id);
            await c.PostAsync(archive, "P");

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteA));
            AssertTwoStepBreadcrumb(html, archive.Title, parentInfo.Permalink);
        });
    }

    private static void AssertTwoStepBreadcrumb(string html, string title, string parentPermalink)
    {
        var crumb = Decode(Section(html, "<nav class=\"sa-breadcrumb", "</nav>"));
        Assert.Single(Regex.Matches(crumb, "<a "));
        Assert.DoesNotContain($"href=\"{parentPermalink}\"", crumb);
        Assert.Matches(new Regex(
            $"<a href=\"/\">Trang chủ</a>\\s*<span class=\"sa-breadcrumb__sep\" aria-hidden=\"true\">/</span>\\s*" +
            $"<span aria-current=\"page\">{Regex.Escape(title)}</span>"), crumb);
    }

    // Seed is wired at startup and targets Site A.
    [Fact]
    public async Task Startup_Seeded_Catalog_Hub_On_Site_A()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await ProductCatalogTests.GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);

        var hub = await api.Pages.GetBySlugAsync<PageInfo>(SiteACatalogSeed.HubSlug, siteA.Id);
        Assert.NotNull(hub);
        Assert.Equal(nameof(ProductHubPage), hub!.TypeId);
    }

    // Program.cs order on an empty site: home stays the start page.
    [Fact]
    public async Task Home_Then_Catalog_Seed_On_Empty_Site_Keeps_Home_As_Start_Page()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = $"sa-cat-order-{suffix}",
            Title = $"Site A Catalog Order {suffix}",
            Hostnames = $"sa-cat-order-{suffix}.local",
            IsDefault = false
        };

        try
        {
            await api.Sites.SaveAsync(site);

            await SiteAHomeSeed.EnsureSeededAsync(api, site.Id);
            await SiteACatalogSeed.EnsureSeededAsync(api, site.Id);

            var start = await api.Pages.GetStartpageAsync<PageInfo>(site.Id);
            Assert.NotNull(start);
            Assert.Equal(nameof(SiteAHomePage), start!.TypeId);
            Assert.Equal(SiteAHomeSeed.Slug, start.Slug);

            var sitemap = await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false);
            Assert.Equal(2, sitemap.Count);
            Assert.Equal(start.Id, sitemap[0].Id);
            Assert.True(ProductHubPage.IsHub(sitemap[1]));
        }
        finally
        {
            await DeleteSitePagesAsync(api, site.Id);
        }
    }

    // AC: each category resolves at its own slug with its own <title>/meta.
    [Fact]
    public async Task Each_Category_Has_Its_Own_Slug_Title_And_Meta()
    {
        await WithSiteAAsync(async (api, siteA, c) =>
        {
            var hub = await c.HubAsync("Hub Seo");
            var first = await c.ArchiveAsync(hub, "Seo One", 0);
            var second = await c.ArchiveAsync(hub, "Seo Two", 1);
            foreach (var (page, n) in new[] { (first, 1), (second, 2) })
            {
                var edit = (await api.Pages.GetByIdAsync<ProductArchive>(page.Id))!;
                edit.MetaTitle = $"SEO title {n} {c.Suffix}";
                edit.MetaDescription = $"SEO description {n} {c.Suffix}";
                await api.Pages.SaveAsync(edit);
            }
            Assert.NotEqual(first.Permalink, second.Permalink);

            foreach (var (page, n) in new[] { (first, 1), (second, 2) })
            {
                var html = Decode(await GetHtmlAsync(page.Permalink, HostnameOf(siteA)));
                Assert.Contains($"<title>SEO title {n} {c.Suffix}</title>", html);
                Assert.Contains($"<meta name=\"description\" content=\"SEO description {n} {c.Suffix}\">", html);
                Assert.Contains($"<h1>{page.Title}</h1>", html);
            }
        });
    }

    // Rule: an empty category stays out of the nav dropdown and aggregate page.
    [Fact]
    public async Task Empty_Category_Is_Absent_From_Dropdown_And_Aggregate()
    {
        await WithSiteAAsync(async (api, siteA, c) =>
        {
            var hub = await c.HubAsync("Hub Hide");
            var full = await c.ArchiveAsync(hub, "Full", 0);
            await c.PostAsync(full, "P");
            var empty = await c.ArchiveAsync(hub, "Empty", 1);
            await c.PostAsync(empty, "Draft", published: false);

            var html = await GetHtmlAsync(hub.Permalink, HostnameOf(siteA));
            var nav = Section(html, "<header class=\"sa-nav\"", "</header>");
            Assert.Contains($"href=\"{full.Permalink}\"", nav);
            Assert.DoesNotContain(empty.Permalink, nav);

            var main = Section(html, "<main class=\"sa-catalog\">", "</main>");
            Assert.Contains($"href=\"{full.Permalink}\"", main);
            Assert.DoesNotContain(empty.Permalink, main);

            // The empty category itself still loads by URL.
            var emptyHtml = await GetHtmlAsync(empty.Permalink, HostnameOf(siteA));
            Assert.Contains($"<h1>{empty.Title}</h1>", Decode(emptyHtml));
        });
    }

    // Matrix: Site B archive.
    [Fact]
    public async Task Site_B_Archive_Keeps_Its_Own_View()
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

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            Assert.Contains("<main class=\"sb-catalog\">", html);
            Assert.Contains("data-sb-product-grid", html);
            Assert.Contains("<article class=\"sb-card\">", html);
            Assert.Contains("sb-trust", html);
            Assert.DoesNotContain("sa-breadcrumb", html);
            Assert.DoesNotContain("sa-pcard", html);
            Assert.DoesNotContain("sa-product-grid", html);
        }
        finally
        {
            await c.CleanupAsync();
        }
    }

    // --- seed ---

    // Matrix: Seed, empty Site A (only home).
    [Fact]
    public async Task Seed_Creates_Published_Hub_And_Eleven_Draft_Categories_After_Home()
    {
        await WithThrowawaySiteAsync(async (api, siteId, home) =>
        {
            await SiteACatalogSeed.EnsureSeededAsync(api, siteId);

            var sitemap = await api.Sites.GetSitemapAsync(siteId, onlyPublished: false);
            Assert.Equal(2, sitemap.Count);
            Assert.Equal(home.Id, sitemap[0].Id);

            var hubItem = sitemap[1];
            Assert.True(ProductHubPage.IsHub(hubItem));
            var hub = (await api.Pages.GetByIdAsync<PageInfo>(hubItem.Id))!;
            Assert.Equal("Sản phẩm", hub.Title);
            Assert.Equal("san-pham", hub.Slug);
            Assert.NotNull(hub.Published);
            Assert.False(hub.IsHidden);

            Assert.Equal(SiteACatalogSeed.Categories.Count, hubItem.Items.Count);
            Assert.Equal(11, hubItem.Items.Count);
            var children = hubItem.Items.OrderBy(i => i.SortOrder).ToList();
            for (var i = 0; i < children.Count; i++)
            {
                var page = (await api.Pages.GetByIdAsync<ProductArchive>(children[i].Id))!;
                Assert.Equal(SiteACatalogSeed.Categories[i].Title, page.Title);
                Assert.Equal($"san-pham/{SiteACatalogSeed.Categories[i].Slug}", page.Slug);
                Assert.Null(page.Published);
                Assert.True(string.IsNullOrEmpty(page.Excerpt));
                Assert.Empty(page.Blocks);
                Assert.Empty(await api.Posts.GetAllAsync<PostInfo>(page.Id));
            }

            // Nav: no visible categories -> no dropdown -> "Sản phẩm" is a plain link.
            using var scope = _factory.Services.CreateScope();
            var catalog = scope.ServiceProvider.GetRequiredService<ProductCatalog>();
            Assert.Empty(await catalog.GetHubTilesAsync(siteId, hub.Id));
        });
    }

    // Matrix: Seed, any of the 12 slugs exists.
    [Fact]
    public async Task Seed_Rerun_Creates_Or_Modifies_Nothing()
    {
        await WithThrowawaySiteAsync(async (api, siteId, _) =>
        {
            await SiteACatalogSeed.EnsureSeededAsync(api, siteId);

            // Editor renames the hub, deletes a category.
            var hubInfo = (await api.Pages.GetBySlugAsync<PageInfo>("san-pham", siteId))!;
            var hub = (await api.Pages.GetByIdAsync<ProductHubPage>(hubInfo.Id))!;
            var renamed = $"Renamed {Guid.NewGuid():N}";
            hub.Title = renamed;
            await api.Pages.SaveAsync(hub);
            var deleted = (await api.Pages.GetBySlugAsync<PageInfo>("san-pham/ban-thi-nghiem", siteId))!;
            await api.Pages.DeleteAsync(deleted.Id);

            var before = await SnapshotAsync(api, siteId);
            await SiteACatalogSeed.EnsureSeededAsync(api, siteId);
            await SiteACatalogSeed.EnsureSeededAsync(api, siteId);
            Assert.Equal(before, await SnapshotAsync(api, siteId));
            Assert.Equal(renamed, (await api.Pages.GetByIdAsync<PageInfo>(hub.Id))!.Title);
            Assert.Null(await api.Pages.GetBySlugAsync<PageInfo>("san-pham/ban-thi-nghiem", siteId));
        });
    }

    [Fact]
    public async Task Seed_Is_Skipped_When_Only_One_Category_Slug_Exists()
    {
        await WithThrowawaySiteAsync(async (api, siteId, home) =>
        {
            var lone = await api.Pages.CreateAsync<StandardPage>();
            lone.SiteId = siteId;
            lone.SortOrder = 1;
            lone.Title = "Lone";
            lone.Slug = "san-pham/noi-that-mam-non";
            lone.Published = DateTime.Now.AddMinutes(-5);
            await api.Pages.SaveAsync(lone);

            var before = await SnapshotAsync(api, siteId);
            await SiteACatalogSeed.EnsureSeededAsync(api, siteId);
            Assert.Equal(before, await SnapshotAsync(api, siteId));
            Assert.Null(await api.Pages.GetBySlugAsync<PageInfo>("san-pham", siteId));
        });
    }

    [Fact]
    public async Task Dev_Sample_Seed_Publishes_Draft_Categories_And_Keeps_The_Last_Empty()
    {
        await WithThrowawaySiteAsync(async (api, siteId, _) =>
        {
            await SiteACatalogSeed.EnsureSeededAsync(api, siteId);

            // An editor-published category with its own post is left alone.
            var keptInfo = (await api.Pages.GetBySlugAsync<PageInfo>("san-pham/bang-tuong-tac", siteId))!;
            var kept = (await api.Pages.GetByIdAsync<ProductArchive>(keptInfo.Id))!;
            kept.Published = DateTime.Now.AddMinutes(-10);
            await api.Pages.SaveAsync(kept);
            var editorPost = await api.Posts.CreateAsync<ProductPost>();
            editorPost.BlogId = kept.Id;
            editorPost.Category = "General";
            editorPost.Title = "Editor product";
            editorPost.Published = DateTime.Now.AddMinutes(-5);
            await api.Posts.SaveAsync(editorPost);

            await SiteASampleSeed.EnsureSeededAsync(api, siteId);
            var afterFirst = await SnapshotAsync(api, siteId);
            await SiteASampleSeed.EnsureSeededAsync(api, siteId);
            Assert.Equal(afterFirst, await SnapshotAsync(api, siteId));

            foreach (var (title, slug) in SiteACatalogSeed.Categories)
            {
                var info = (await api.Pages.GetBySlugAsync<PageInfo>($"san-pham/{slug}", siteId))!;
                Assert.NotNull(info.Published);
                var posts = (await api.Posts.GetAllAsync<PostInfo>(info.Id)).ToList();
                if (slug == "bang-tuong-tac")
                {
                    Assert.Equal("Editor product", Assert.Single(posts).Title);
                }
                else
                {
                    Assert.Equal(SiteASampleSeed.Products[slug].Length, posts.Count);
                }
            }
            Assert.Empty(SiteASampleSeed.Products[SiteACatalogSeed.Categories[^1].Slug]);
        });
    }

    // --- helpers ---

    private static readonly byte[] WidePng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAANwAAABaCAIAAABc0a8TAAAAwElEQVR42u3SQQ0AAAjEsPODIyxhmuCCR5MqWJbpglciAaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMaUKmBJMiSnBlJgSTIkpwZRgSkwJpsSUYEpMCabElGBKMCWmBFNiSjAlpgRTgikxJZgSU4IpMSWYElOCKcGUmBJMiSnBlJgSTAmmxJRgSkwJpsSUYEpMCaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMSWYEkyJKcGUmBJMiSnBlHBTLjqcMk/EWR93AAAAAElFTkSuQmCC");

    private readonly List<Guid> _media = new();

    private async Task<Guid> UploadPngAsync(IApi api)
    {
        using var stream = new System.IO.MemoryStream(WidePng);
        var content = new StreamMediaContent { Filename = $"sa-pcard-test-{Guid.NewGuid():N}.png", Data = stream };
        await api.Media.SaveAsync(content);
        _media.Add(content.Id!.Value);
        return content.Id!.Value;
    }

    /// <summary>
    /// A published category (excerpt "Mô tả danh mục") with an HTML block and
    /// a renderable config block, created in its own DI scope and tracked.
    /// </summary>
    private async Task<ProductArchive> CreateArchiveWithConfigBlockAsync(Site site, PageBase hub, Builder c, string blockHtml)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var archive = await api.Pages.CreateAsync<ProductArchive>();
        archive.SiteId = site.Id;
        archive.ParentId = hub.Id;
        archive.SortOrder = 0;
        archive.Title = $"Du che {c.Suffix}";
        archive.Slug = $"{hub.Slug}/du-che-{c.Suffix}";
        archive.Excerpt = "Mô tả danh mục";
        archive.Published = DateTime.Now.AddMinutes(-5);
        archive.Blocks.Add(new HtmlBlock { Body = blockHtml });
        var config = new ConfigBlock { Heading = "Cấu hình" };
        config.Items.Add(new ConfigRowBlock
        {
            Label = "Kích thước",
            InputType = new SelectField<ConfigInputType> { Value = ConfigInputType.Text },
        });
        archive.Blocks.Add(config);
        await api.Pages.SaveAsync(archive);
        c.Track(archive.Id);

        var loaded = (await api.Pages.GetByIdAsync<ProductArchive>(archive.Id))!;
        Assert.NotNull(ConfigBlock.FirstRenderable(loaded.Blocks));
        return loaded;
    }

    /// <summary>Product cards (each `a.sa-pcard`), raw.</summary>
    private static List<string> Cards(string html)
    {
        var cards = new List<string>();
        var at = 0;
        while ((at = html.IndexOf("<a class=\"sa-pcard\"", at, StringComparison.Ordinal)) >= 0)
        {
            var end = html.IndexOf("</a>", at, StringComparison.Ordinal);
            if (end < 0)
            {
                break;
            }
            cards.Add(html[at..(end + 4)]);
            at = end;
        }
        return cards;
    }

    /// <summary>Every page (id, title, slug, published) and post id of the site.</summary>
    private static async Task<List<string>> SnapshotAsync(IApi api, Guid siteId)
    {
        var rows = new List<string>();
        foreach (var page in await api.Pages.GetAllAsync<PageInfo>(siteId))
        {
            rows.Add($"{page.Id}|{page.Title}|{page.Slug}|{page.Published:O}|{page.ParentId}|{page.SortOrder}");
            foreach (var post in await api.Posts.GetAllAsync<PostInfo>(page.Id))
            {
                rows.Add($"post {post.Id}|{post.Title}|{post.Published:O}");
            }
        }
        rows.Sort(StringComparer.Ordinal);
        return rows;
    }

    private sealed class Builder
    {
        private readonly ProductCatalogTests.CatalogBuilder _inner;
        private readonly IServiceProvider _services;
        private readonly List<Guid> _extra = new();
        private readonly List<Guid> _last = new();

        public Builder(IApi api, Site site, IServiceProvider services)
        {
            _services = services;
            _inner = new ProductCatalogTests.CatalogBuilder(api, site);
        }

        public string Suffix => _inner.Suffix;

        public Task<ProductHubPage> HubAsync(string title, bool published = true) => _inner.HubAsync(title, published: published);

        public Task<ProductArchive> ArchiveAsync(PageBase hub, string title, int sortOrder,
            string? excerpt = null, string? blockHtml = null) =>
            _inner.ArchiveAsync(hub, title, sortOrder, excerpt: excerpt, blockHtml: blockHtml);

        public Task<ProductPost> PostAsync(PageBase archive, string title, bool published = true,
            string? price = null, DateTime? publishedAt = null) =>
            _inner.PostAsync(archive, title, published: published, price: price, publishedAt: publishedAt);

        /// <summary>
        /// A page created outside the inner builder (in its own DI scope);
        /// deleted with its posts in a fresh scope, before the inner pages -
        /// the shared scope's DbContext must never track its blocks.
        /// </summary>
        public void Track(Guid pageId) => _extra.Add(pageId);

        /// <summary>A parent page created outside the builder; deleted last, after the builder's pages.</summary>
        public void TrackLast(Guid pageId) => _last.Add(pageId);

        public async Task CleanupAsync()
        {
            using (var scope = _services.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<IApi>();
                foreach (var id in _extra)
                {
                    foreach (var post in await api.Posts.GetAllAsync<PostInfo>(id))
                    {
                        await api.Posts.DeleteAsync(post.Id);
                    }
                    await api.Pages.DeleteAsync(id);
                }
            }
            await _inner.CleanupAsync();

            using var lastScope = _services.CreateScope();
            var lastApi = lastScope.ServiceProvider.GetRequiredService<IApi>();
            foreach (var id in _last)
            {
                await lastApi.Pages.DeleteAsync(id);
            }
        }
    }

    private async Task WithSiteAAsync(Func<IApi, Site, Builder, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await ProductCatalogTests.GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var builder = new Builder(api, siteA, _factory.Services);

        try
        {
            await body(api, siteA, builder);
        }
        finally
        {
            await builder.CleanupAsync();
            using var mediaScope = _factory.Services.CreateScope();
            var mediaApi = mediaScope.ServiceProvider.GetRequiredService<IApi>();
            foreach (var id in _media)
            {
                await mediaApi.Media.DeleteAsync(id);
            }
            _media.Clear();
        }
    }

    /// <summary>A throwaway site with only a published "Trang chủ"; deleted afterwards.</summary>
    private async Task WithThrowawaySiteAsync(Func<IApi, Guid, PageBase, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = $"sa-cat-seed-{suffix}",
            Title = $"Site A Catalog Seed {suffix}",
            Hostnames = $"sa-cat-seed-{suffix}.local",
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

            await body(api, site.Id, home);
        }
        finally
        {
            await DeleteSitePagesAsync(api, site.Id);
        }
    }

    /// <summary>Deletes every post and page of a throwaway site, then the site.</summary>
    private static async Task DeleteSitePagesAsync(IApi api, Guid siteId)
    {
        var pages = (await api.Pages.GetAllAsync<PageInfo>(siteId)).ToList();
        foreach (var page in pages)
        {
            foreach (var post in await api.Posts.GetAllAsync<PostInfo>(page.Id))
            {
                await api.Posts.DeleteAsync(post.Id);
            }
        }
        foreach (var page in pages.Where(p => p.ParentId != null))
        {
            await api.Pages.DeleteAsync(page.Id);
        }
        foreach (var page in pages.Where(p => p.ParentId == null))
        {
            await api.Pages.DeleteAsync(page.Id);
        }
        if (await api.Sites.GetByIdAsync(siteId) != null)
        {
            await api.Sites.DeleteAsync(siteId);
        }
    }

    private static string Section(string html, string start, string end) => ProductCatalogTests.Section(html, start, end);

    private static string Decode(string html) => ProductCatalogTests.Decode(html);

    private static string HostnameOf(Site site) => ProductCatalogTests.HostnameOf(site);

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
