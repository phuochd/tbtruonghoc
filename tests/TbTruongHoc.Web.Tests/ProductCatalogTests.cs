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
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using TbTruongHoc.Web.Services;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 2.2: Trống hub (<see cref="ProductHubPage"/>) + subcategory
/// (<see cref="ProductArchive"/>) pages with the product-card grid. Real HTTP
/// render against the real MariaDB-backed app on Site B. Each test builds its
/// own throwaway hub/archives/posts (unique slugs) and deletes them after, so
/// the startup-seeded "trong" hub is never touched except by the seed test,
/// which restores what it changes.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class ProductCatalogTests
{
    private const int NonStartPageSortOrder = 1;

    private readonly PiranhaWebApplicationFactory _factory;

    public ProductCatalogTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Hub_Shows_Only_Visible_NonEmpty_Subcategories_In_Sitemap_Order()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Hub Mixed");
            var withPosts = await c.ArchiveAsync(hub, "Cat With Posts", 0, excerpt: "Excerpt for cat with posts");
            var empty = await c.ArchiveAsync(hub, "Cat Empty", 1);
            var hidden = await c.ArchiveAsync(hub, "Cat Hidden", 2, isHidden: true);
            var another = await c.ArchiveAsync(hub, "Cat Another", 3);
            var draftOnly = await c.ArchiveAsync(hub, "Cat Draft Only", 4);

            await c.PostAsync(withPosts, "P1");
            await c.PostAsync(withPosts, "P2");
            await c.PostAsync(hidden, "P3");
            await c.PostAsync(another, "P4");
            await c.PostAsync(draftOnly, "P5 draft", published: false);

            // Not a ProductArchive (TypeId filter), even with a published post.
            var standard = await c.StandardArchiveAsync(hub, "Cat Standard", 5);
            await c.StandardPostAsync(standard, "P6 standard");

            // Only post is future-dated -> counts as empty.
            var future = await c.ArchiveAsync(hub, "Cat Future", 6);
            await c.PostAsync(future, "P7 future", publishedAt: DateTime.Now.AddDays(1));

            // The one place for the hide rule.
            var catalog = new ProductCatalog(api);
            var tiles = await catalog.GetHubTilesAsync(siteB.Id, hub.Id);
            Assert.Equal(new[] { withPosts.Title, another.Title }, tiles.Select(t => t.Title).ToArray());
            Assert.Equal(new[] { withPosts.Permalink, another.Permalink }, tiles.Select(t => t.Permalink).ToArray());

            var html = await GetHtmlAsync(hub.Permalink, HostnameOf(siteB));
            var main = Decode(Section(html, "<main", "</main>"));
            var grid = Section(main, "<ul class=\"sb-grid\" data-sb-tile-grid", "</ul>");

            Assert.Equal(2, Count(grid, "class=\"sb-tile\""));
            var firstAt = grid.IndexOf($"href=\"{withPosts.Permalink}\"", StringComparison.Ordinal);
            var secondAt = grid.IndexOf($"href=\"{another.Permalink}\"", StringComparison.Ordinal);
            Assert.True(firstAt >= 0 && secondAt > firstAt, "Tiles must follow sitemap order.");
            Assert.Contains($"<span class=\"sb-tile__eyebrow\">{hub.Title}</span>", grid);
            Assert.Contains($"<h2 class=\"sb-tile__title\">{withPosts.Title}</h2>", grid);
            Assert.Contains("Excerpt for cat with posts", grid);
            Assert.DoesNotContain(empty.Title, grid);
            Assert.DoesNotContain(hidden.Title, grid);
            Assert.DoesNotContain(draftOnly.Title, grid);
            Assert.DoesNotContain(standard.Title, grid);
            Assert.DoesNotContain(future.Title, grid);

            // Nav submenu under the hub lists the non-hidden subcategories.
            var nav = Section(html, "<header class=\"sb-nav\"", "</header>");
            var submenu = Section(nav, $"<ul class=\"sb-nav__submenu\" id=\"sb-sub-{hub.Id:N}\"", "</ul>");
            foreach (var a in new[] { withPosts, empty, another, draftOnly })
            {
                Assert.Contains($"href=\"{a.Permalink}\"", submenu);
            }
            Assert.DoesNotContain($"href=\"{hidden.Permalink}\"", submenu);

            Assert.Contains($"<title>{hub.Title}</title>", Decode(html));
            Assert.Contains("<meta property=\"og:title\"", html);
        });
    }

    [Fact]
    public async Task Hub_With_All_Empty_Children_Omits_Grid_But_Renders_Blocks()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var marker = $"hub-block-marker-{c.Suffix}";
            var hub = await c.HubAsync("Hub Empty", blockHtml: $"<p>{marker}</p>");
            await c.ArchiveAsync(hub, "Empty One", 0);
            var draftOnly = await c.ArchiveAsync(hub, "Empty Two", 1);
            await c.PostAsync(draftOnly, "Draft", published: false);

            var html = await GetHtmlAsync(hub.Permalink, HostnameOf(siteB));
            Assert.Contains(marker, html);
            Assert.DoesNotContain("data-sb-tile-grid", html);
            Assert.DoesNotContain("sb-tile", html);
        });
    }

    [Fact]
    public async Task Archive_Cards_Follow_Product_Card_Anatomy()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Hub Cards");
            var archive = await c.ArchiveAsync(hub, "Cards", 0);
            const string price = "từ 2.500.000đ";
            var priced = await c.PostAsync(archive, "Priced", price: price, excerpt: "Priced excerpt");
            var unpriced = await c.PostAsync(archive, "Unpriced", excerpt: "Unpriced excerpt");
            var injected = await c.PostAsync(archive, "Injected", price: "<b>1.000.000đ</b>");

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            Assert.Contains($"<title>{archive.Title}</title>", Decode(html));
            Assert.DoesNotContain("Đặt mua", Decode(html));

            var cards = Cards(html);
            Assert.Equal(3, cards.Count);

            var pricedCard = Decode(cards.Single(k => k.Contains(priced.Title)));
            Assert.Contains($"<p class=\"sb-card__price\">{price}</p>", pricedCard);
            Assert.DoesNotContain("Liên hệ báo giá", pricedCard);
            Assert.Contains($"<p class=\"sb-card__eyebrow\">{archive.Title}</p>", pricedCard);
            Assert.Contains("<p class=\"sb-card__desc\">Priced excerpt</p>", pricedCard);

            var unpricedCard = Decode(cards.Single(k => k.Contains(unpriced.Title)));
            Assert.Contains("<p class=\"sb-card__price sb-card__price--contact\">Liên hệ báo giá</p>", unpricedCard);

            // Price is HTML-encoded exactly as typed.
            var injectedRaw = cards.Single(k => k.Contains(injected.Title));
            Assert.DoesNotContain("<b>", injectedRaw);
            var injectedCard = Decode(injectedRaw);
            Assert.Contains("<p class=\"sb-card__price\"><b>1.000.000đ</b></p>", injectedCard);

            foreach (var (card, post) in new[] { (pricedCard, priced), (unpricedCard, unpriced) })
            {
                // No image -> gradient frame only, no <img>.
                Assert.Contains("<div class=\"sb-card__thumb\">", card);
                Assert.DoesNotContain("<img", card);

                // Two separate anchors (stretched title link + CTA), same URL, not nested.
                var hrefs = Regex.Matches(card, "<a [^>]*href=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();
                Assert.Equal(new[] { post.Permalink, post.Permalink }, hrefs);
                var firstClose = card.IndexOf("</a>", StringComparison.Ordinal);
                var secondOpen = card.IndexOf("<a ", card.IndexOf("<a ", StringComparison.Ordinal) + 1, StringComparison.Ordinal);
                Assert.True(firstClose < secondOpen, "Anchors must not be nested.");
                Assert.Contains($"class=\"sb-card__link\" href=\"{post.Permalink}\">{post.Title}</a>", card);
                Assert.Contains($"class=\"sb-card__cta\" href=\"{post.Permalink}\" aria-label=\"Xem chi tiết {post.Title}\">Xem chi tiết</a>", card);
            }
        });
    }

    [Fact]
    public async Task Empty_Archive_Renders_Header_And_Blocks_Only()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Hub Empty Archive");
            var marker = $"archive-block-marker-{c.Suffix}";
            var archive = await c.ArchiveAsync(hub, "Nothing Here", 0, blockHtml: $"<p>{marker}</p>");

            var html = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            Assert.Contains($"<h1>{archive.Title}</h1>", Decode(html));
            Assert.Contains(marker, html);
            Assert.DoesNotContain("data-sb-product-grid", html);
            Assert.DoesNotContain("sb-card", html);
            Assert.DoesNotContain("sb-pager", html);
            Assert.DoesNotContain("Không có", Decode(html));
        });
    }

    [Fact]
    public async Task Archive_Pages_Twelve_Per_Page_With_Prev_Next_Links()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Hub Paging");
            var archive = await c.ArchiveAsync(hub, "Paging", 0);
            for (var i = 0; i < 13; i++)
            {
                await c.PostAsync(archive, $"Item {i:D2}");
            }

            var page1 = await GetHtmlAsync(archive.Permalink, HostnameOf(siteB));
            Assert.Equal(12, Cards(page1).Count);
            var pager1 = Decode(Section(page1, "<nav class=\"sb-pager\"", "</nav>"));
            Assert.Contains($"href=\"{archive.Permalink}/page/2\" rel=\"next\">Trang sau</a>", pager1);
            Assert.DoesNotContain("Trang trước", pager1);

            var page2 = await GetHtmlAsync($"{archive.Permalink}/page/2", HostnameOf(siteB));
            Assert.Single(Cards(page2));
            var pager2 = Decode(Section(page2, "<nav class=\"sb-pager\"", "</nav>"));
            Assert.Contains($"href=\"{archive.Permalink}\" rel=\"prev\">Trang trước</a>", pager2);
            Assert.DoesNotContain("Trang sau", pager2);
        });
    }

    [Fact]
    public async Task Trong_Seed_Is_Idempotent_And_Keeps_Editor_Changes()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);

        // Startup already ran the seed.
        var hub = await api.Pages.GetBySlugAsync<PageInfo>(TrongCatalogSeed.HubSlug, siteB.Id);
        Assert.NotNull(hub);
        Assert.Equal(nameof(ProductHubPage), hub!.TypeId);

        var before = Flatten(await api.Sites.GetSitemapAsync(siteB.Id, onlyPublished: false)).Select(i => i.Id).OrderBy(i => i).ToList();

        await TrongCatalogSeed.EnsureSeededAsync(api);
        await TrongCatalogSeed.EnsureSeededAsync(api);

        var after = Flatten(await api.Sites.GetSitemapAsync(siteB.Id, onlyPublished: false)).Select(i => i.Id).OrderBy(i => i).ToList();
        Assert.Equal(before, after);

        // An editor's title change survives a re-run.
        var editable = await api.Pages.GetByIdAsync<ProductHubPage>(hub.Id);
        var originalTitle = editable!.Title;
        var editedTitle = $"Trống edited {Guid.NewGuid():N}";
        try
        {
            editable.Title = editedTitle;
            await api.Pages.SaveAsync(editable);

            await TrongCatalogSeed.EnsureSeededAsync(api);

            var reloaded = await api.Pages.GetByIdAsync<PageInfo>(hub.Id);
            Assert.Equal(editedTitle, reloaded!.Title);
            Assert.Equal(before, Flatten(await api.Sites.GetSitemapAsync(siteB.Id, onlyPublished: false)).Select(i => i.Id).OrderBy(i => i).ToList());
        }
        finally
        {
            var restore = await api.Pages.GetByIdAsync<ProductHubPage>(hub.Id);
            restore!.Title = originalTitle;
            await api.Pages.SaveAsync(restore);
        }
    }

    [Fact]
    public async Task Unpublished_Hub_Draft_Preview_Still_Gets_Tiles()
    {
        await WithCatalogAsync(async (api, siteB, c) =>
        {
            var hub = await c.HubAsync("Hub Draft", published: false);
            var archive = await c.ArchiveAsync(hub, "Draft Hub Child", 0);
            await c.PostAsync(archive, "Visible");

            var tiles = await new ProductCatalog(api).GetHubTilesAsync(siteB.Id, hub.Id);

            var tile = Assert.Single(tiles);
            Assert.Equal(archive.Title, tile.Title);
            Assert.Equal(archive.Permalink, tile.Permalink);
        });
    }

    [Fact]
    public async Task Trong_Seed_Creates_Hub_And_Five_Archives_Then_Skips_After_Hub_Rename()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = $"seed-test-{suffix}",
            Title = $"Seed Test {suffix}",
            Hostnames = $"seed-test-{suffix}.local",
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

            await TrongCatalogSeed.EnsureSeededAsync(api, site.Id);

            var sitemap = await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false);
            Assert.Equal(2, sitemap.Count);
            Assert.Equal(existing.Id, sitemap[0].Id);

            var hub = await api.Pages.GetBySlugAsync<PageInfo>(TrongCatalogSeed.HubSlug, site.Id);
            Assert.NotNull(hub);
            Assert.Equal("Trống", hub!.Title);
            Assert.Equal("trong", hub.Slug);
            Assert.Equal(nameof(ProductHubPage), hub.TypeId);
            Assert.True(hub.Published.HasValue && hub.Published.Value <= DateTime.Now);
            Assert.Null(hub.ParentId);
            Assert.True(hub.SortOrder > existing.SortOrder);
            Assert.Equal(hub.Id, sitemap[1].Id);

            var expected = new[]
            {
                ("Trường học", "trong/truong-hoc"),
                ("Lân", "trong/lan"),
                ("Đội", "trong/doi"),
                ("Lễ hội", "trong/le-hoi"),
                ("Chùa", "trong/chua"),
            };
            var children = sitemap[1].Items.OrderBy(i => i.SortOrder).ToList();
            Assert.Equal(5, children.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                var child = await api.Pages.GetByIdAsync<PageInfo>(children[i].Id);
                Assert.NotNull(child);
                Assert.Equal(expected[i].Item1, child!.Title);
                Assert.Equal(expected[i].Item2, child.Slug);
                Assert.Equal(nameof(ProductArchive), child.TypeId);
                Assert.Equal(hub.Id, child.ParentId);
                Assert.True(child.Published.HasValue && child.Published.Value <= DateTime.Now);
            }

            // Editor renames the hub slug; the children keep "trong/*".
            var editable = await api.Pages.GetByIdAsync<ProductHubPage>(hub.Id);
            editable!.Slug = $"trong-renamed-{suffix}";
            await api.Pages.SaveAsync(editable);

            var before = Flatten(await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false)).Select(i => i.Id).OrderBy(i => i).ToList();
            await TrongCatalogSeed.EnsureSeededAsync(api, site.Id);
            var after = Flatten(await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false)).Select(i => i.Id).OrderBy(i => i).ToList();
            Assert.Equal(before, after);
        }
        finally
        {
            // Deepest pages first, then the site.
            var all = Flatten(await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false)).ToList();
            foreach (var item in all.Where(i => i.ParentId != null).Reverse())
            {
                await api.Pages.DeleteAsync(item.Id);
            }
            foreach (var item in all.Where(i => i.ParentId == null))
            {
                await api.Pages.DeleteAsync(item.Id);
            }
            if (await api.Sites.GetByIdAsync(site.Id) != null)
            {
                await api.Sites.DeleteAsync(site.Id);
            }
        }
    }

    // --- helpers ---

    internal sealed class CatalogBuilder
    {
        private readonly IApi _api;
        private readonly Site _site;
        private readonly List<Guid> _pages = new();
        private readonly List<Guid> _posts = new();

        public CatalogBuilder(IApi api, Site site)
        {
            _api = api;
            _site = site;
        }

        public string Suffix { get; } = Guid.NewGuid().ToString("N")[..8];

        public async Task<ProductHubPage> HubAsync(string title, string? blockHtml = null, bool published = true)
        {
            var hub = await _api.Pages.CreateAsync<ProductHubPage>();
            hub.SiteId = _site.Id;
            hub.SortOrder = NonStartPageSortOrder;
            hub.Title = $"{title} {Suffix}";
            hub.Slug = $"pc-hub-{Suffix}";
            hub.Published = published ? DateTime.Now.AddMinutes(-5) : null;
            if (blockHtml != null)
            {
                hub.Blocks.Add(new HtmlBlock { Body = blockHtml });
            }
            await _api.Pages.SaveAsync(hub);
            _pages.Add(hub.Id);
            return (await _api.Pages.GetByIdAsync<ProductHubPage>(hub.Id))!;
        }

        public async Task<ProductArchive> ArchiveAsync(PageBase hub, string title, int sortOrder,
            bool isHidden = false, string? excerpt = null, string? blockHtml = null)
        {
            var slugPart = Regex.Replace(title.ToLowerInvariant(), "[^a-z0-9]+", "-");
            var archive = await _api.Pages.CreateAsync<ProductArchive>();
            archive.SiteId = _site.Id;
            archive.ParentId = hub.Id;
            archive.SortOrder = sortOrder;
            archive.Title = $"{title} {Suffix}";
            archive.Slug = $"{hub.Slug}/{slugPart}-{Suffix}";
            archive.Excerpt = excerpt;
            archive.IsHidden = isHidden;
            archive.Published = DateTime.Now.AddMinutes(-5);
            if (blockHtml != null)
            {
                archive.Blocks.Add(new HtmlBlock { Body = blockHtml });
            }
            await _api.Pages.SaveAsync(archive);
            _pages.Add(archive.Id);
            return (await _api.Pages.GetByIdAsync<ProductArchive>(archive.Id))!;
        }

        public async Task<ProductPost> PostAsync(PageBase archive, string title,
            bool published = true, string? price = null, string? excerpt = null, DateTime? publishedAt = null)
        {
            var post = await _api.Posts.CreateAsync<ProductPost>();
            post.BlogId = archive.Id;
            post.Category = "General";
            post.Title = $"{title} {Suffix}";
            post.Slug = $"pc-post-{Guid.NewGuid():N}";
            post.Excerpt = excerpt;
            post.Price = price;
            post.Published = published ? publishedAt ?? DateTime.Now.AddMinutes(-1) : null;
            await _api.Posts.SaveAsync(post);
            _posts.Add(post.Id);
            return (await _api.Posts.GetByIdAsync<ProductPost>(post.Id))!;
        }

        public async Task<StandardArchive> StandardArchiveAsync(PageBase hub, string title, int sortOrder)
        {
            var archive = await _api.Pages.CreateAsync<StandardArchive>();
            archive.SiteId = _site.Id;
            archive.ParentId = hub.Id;
            archive.SortOrder = sortOrder;
            archive.Title = $"{title} {Suffix}";
            archive.Slug = $"{hub.Slug}/standard-{Guid.NewGuid():N}";
            archive.Published = DateTime.Now.AddMinutes(-5);
            await _api.Pages.SaveAsync(archive);
            _pages.Add(archive.Id);
            return (await _api.Pages.GetByIdAsync<StandardArchive>(archive.Id))!;
        }

        public async Task StandardPostAsync(PageBase archive, string title)
        {
            var post = await _api.Posts.CreateAsync<StandardPost>();
            post.BlogId = archive.Id;
            post.Category = "General";
            post.Title = $"{title} {Suffix}";
            post.Slug = $"pc-post-{Guid.NewGuid():N}";
            post.Published = DateTime.Now.AddMinutes(-1);
            await _api.Posts.SaveAsync(post);
            _posts.Add(post.Id);
        }

        public async Task CleanupAsync()
        {
            foreach (var id in _posts)
            {
                await _api.Posts.DeleteAsync(id);
            }
            // Children (created after their hub) before the hub.
            for (var i = _pages.Count - 1; i >= 0; i--)
            {
                await _api.Pages.DeleteAsync(_pages[i]);
            }
        }
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

    private static List<string> Cards(string html)
    {
        var cards = new List<string>();
        var at = 0;
        while ((at = html.IndexOf("<article class=\"sb-card\">", at, StringComparison.Ordinal)) >= 0)
        {
            var end = html.IndexOf("</article>", at, StringComparison.Ordinal);
            cards.Add(html[at..(end + "</article>".Length)]);
            at = end;
        }
        return cards;
    }

    private static IEnumerable<SitemapItem> Flatten(IEnumerable<SitemapItem> items) =>
        items.SelectMany(i => new[] { i }.Concat(Flatten(i.Items)));

    private static int Count(string haystack, string needle) =>
        Regex.Matches(haystack, Regex.Escape(needle)).Count;

    internal static string Decode(string html) => WebUtility.HtmlDecode(html);

    internal static string Section(string html, string startMarker, string endMarker)
    {
        var start = html.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected to find '{startMarker}'.");
        var end = html.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(end > start, $"Expected '{endMarker}' after '{startMarker}'.");
        return html[start..(end + endMarker.Length)];
    }

    internal static async Task<Site> GetSiteAsync(IApi api, string internalId)
    {
        var site = await api.Sites.GetByInternalIdAsync(internalId);
        Assert.NotNull(site);
        return site!;
    }

    internal static string HostnameOf(Site site)
    {
        var hostname = site.Hostnames?.Split(',').FirstOrDefault()?.Trim();
        Assert.False(string.IsNullOrEmpty(hostname));
        return hostname!;
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
