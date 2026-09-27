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
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 2.3: Site B's "Thùng rượu gỗ" and "Bồn tắm gỗ" category pages
/// (<see cref="ProductLineSeed"/>). Seed tests run on a throwaway site via
/// the internal overload. Render tests use the startup-seeded pages on the
/// real Site B, set the publish state they need, and restore every page and
/// post they touched afterwards.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class ProductLineTests
{
    private readonly PiranhaWebApplicationFactory _factory;

    public ProductLineTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Matrix: Fresh Site B.
    [Fact]
    public async Task Seed_Creates_Two_Draft_Archives_After_Existing_Pages_And_Three_Draft_Variants()
    {
        await WithThrowawaySiteAsync(async (api, siteId, existing) =>
        {
            await ProductLineSeed.EnsureSeededAsync(api, siteId);

            var sitemap = await api.Sites.GetSitemapAsync(siteId, onlyPublished: false);
            Assert.Equal(3, sitemap.Count);
            Assert.Equal(existing.Id, sitemap[0].Id);

            var thung = await AssertDraftArchiveAsync(api, siteId, "thung-ruou-go", "Thùng rượu gỗ");
            var bon = await AssertDraftArchiveAsync(api, siteId, "bon-tam-go", "Bồn tắm gỗ");
            Assert.Equal(thung.Id, sitemap[1].Id);
            Assert.Equal(bon.Id, sitemap[2].Id);
            Assert.True(thung.SortOrder > existing.SortOrder);
            Assert.True(bon.SortOrder > thung.SortOrder);

            var posts = (await api.Posts.GetAllAsync<ProductPost>(thung.Id)).ToList();
            Assert.Equal(3, posts.Count);
            Assert.All(posts, p => Assert.Null(p.Published));
            Assert.All(posts, p => Assert.Null(p.PriceText));
            var expected = new[] { "Thùng rượu gỗ sồi – ngựa kéo", "Thùng rượu gỗ sồi – 1 ngựa", "Thùng rượu gỗ sồi – 2 ngựa" };
            Assert.Equal(expected, ProductLineSeed.ThungRuouVariants);
            Assert.Equal(expected.OrderBy(t => t), posts.Select(p => p.Title).OrderBy(t => t));
            Assert.Equal(3, posts.Select(p => p.Slug).Distinct().Count());

            Assert.Empty(await api.Posts.GetAllAsync<PostInfo>(bon.Id));
        });
    }

    // Matrix: Re-run, one renamed.
    [Fact]
    public async Task Seed_Rerun_Creates_Nothing_And_Keeps_Renamed_Title()
    {
        await WithThrowawaySiteAsync(async (api, siteId, _) =>
        {
            await ProductLineSeed.EnsureSeededAsync(api, siteId);

            var thungInfo = await api.Pages.GetBySlugAsync<PageInfo>(ProductLineSeed.ThungRuouSlug, siteId);
            var thung = await api.Pages.GetByIdAsync<ProductArchive>(thungInfo!.Id);
            var renamed = $"Thùng rượu renamed {Guid.NewGuid():N}";
            thung!.Title = renamed;
            await api.Pages.SaveAsync(thung);

            var pagesBefore = await PageIdsAsync(api, siteId);
            var postsBefore = await PostIdsAsync(api, thung.Id);

            await ProductLineSeed.EnsureSeededAsync(api, siteId);
            await ProductLineSeed.EnsureSeededAsync(api, siteId);

            Assert.Equal(pagesBefore, await PageIdsAsync(api, siteId));
            Assert.Equal(postsBefore, await PostIdsAsync(api, thung.Id));
            Assert.Equal(renamed, (await api.Pages.GetByIdAsync<PageInfo>(thung.Id))!.Title);
        });
    }

    // Matrix: One deleted.
    [Fact]
    public async Task Seed_Recreates_Only_Deleted_Bon_Tam_Without_Duplicating_Variants()
    {
        await WithThrowawaySiteAsync(async (api, siteId, _) =>
        {
            await ProductLineSeed.EnsureSeededAsync(api, siteId);

            var thung = await api.Pages.GetBySlugAsync<PageInfo>(ProductLineSeed.ThungRuouSlug, siteId);
            var bon = await api.Pages.GetBySlugAsync<PageInfo>(ProductLineSeed.BonTamSlug, siteId);

            // An editor also deletes one variant - it must not come back.
            var posts = await PostIdsAsync(api, thung!.Id);
            await api.Posts.DeleteAsync(posts[0]);
            var postsBefore = await PostIdsAsync(api, thung.Id);
            Assert.Equal(2, postsBefore.Count);

            await api.Pages.DeleteAsync(bon!.Id);

            await ProductLineSeed.EnsureSeededAsync(api, siteId);

            var recreated = await AssertDraftArchiveAsync(api, siteId, "bon-tam-go", "Bồn tắm gỗ");
            Assert.NotEqual(bon.Id, recreated.Id);
            Assert.Equal(thung.Id, (await api.Pages.GetBySlugAsync<PageInfo>(ProductLineSeed.ThungRuouSlug, siteId))!.Id);
            Assert.Equal(postsBefore, await PostIdsAsync(api, thung.Id));
            Assert.Equal(3, (await api.Sites.GetSitemapAsync(siteId, onlyPublished: false)).Count);
        });
    }

    // Matrix: Drafts, anonymous.
    [Fact]
    public async Task Draft_Pages_Are_Absent_From_Nav_And_Return_404()
    {
        await WithSiteBPagesAsync(async (api, siteB, thung, bon, _) =>
        {
            await SetPublishedAsync(api, thung.Id, null);
            await SetPublishedAsync(api, bon.Id, null);

            var hub = await api.Pages.GetBySlugAsync<PageInfo>(TrongCatalogSeed.HubSlug, siteB.Id);
            var html = await GetAsync(hub!.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var nav = Section(html, "<header class=\"sb-nav\"", "</header>");
            Assert.DoesNotContain($"href=\"{thung.Permalink}\"", nav);
            Assert.DoesNotContain($"href=\"{bon.Permalink}\"", nav);

            await GetAsync(thung.Permalink, HostnameOf(siteB), HttpStatusCode.NotFound);
            await GetAsync(bon.Permalink, HostnameOf(siteB), HttpStatusCode.NotFound);
        });
    }

    // Matrix: Thùng rượu published.
    [Fact]
    public async Task Published_Thung_Ruou_Shows_Contact_For_Quote_Cards_And_Nav_Link_After_Trong()
    {
        await WithSiteBPagesAsync(async (api, siteB, thung, bon, posts) =>
        {
            await SetPublishedAsync(api, thung.Id, DateTime.Now.AddMinutes(-10));
            await SetPublishedAsync(api, bon.Id, null);
            for (var i = 0; i < posts.Count; i++)
            {
                var post = await api.Posts.GetByIdAsync<ProductPost>(posts[i].Id);
                post!.Published = DateTime.Now.AddMinutes(-5 + i);
                post.Price = null;
                await api.Posts.SaveAsync(post);
            }

            var archive = (await api.Pages.GetByIdAsync<ProductArchive>(thung.Id))!;
            var html = await GetAsync(archive.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var decoded = Decode(html);

            Assert.Contains($"<title>{archive.Title}</title>", decoded);
            Assert.DoesNotContain("Đặt mua", decoded);

            var cards = Cards(html);
            Assert.Equal(posts.Count, cards.Count);
            foreach (var seeded in ProductLineSeed.ThungRuouVariants)
            {
                Assert.Contains(posts, p => p.Title == seeded);
            }
            foreach (var p in posts)
            {
                var post = (await api.Posts.GetByIdAsync<ProductPost>(p.Id))!;
                var card = Decode(cards.Single(k => Decode(k).Contains($">{post.Title}</a>")));
                Assert.Contains($"<p class=\"sb-card__eyebrow\">{archive.Title}</p>", card);
                Assert.Contains("<p class=\"sb-card__price sb-card__price--contact\">Liên hệ báo giá</p>", card);
                var hrefs = Regex.Matches(card, "<a [^>]*href=\"([^\"]+)\"").Select(m => m.Groups[1].Value).Distinct().ToList();
                Assert.Equal(new[] { post.Permalink }, hrefs);
                Assert.Contains($"<h1 class=\"sb-pdp__title\">{post.Title}</h1>", Decode(await GetAsync(post.Permalink, HostnameOf(siteB), HttpStatusCode.OK)));
            }

            // Nav: flat link, after Trống; Bồn tắm (still a draft) absent.
            var nav = Section(html, "<header class=\"sb-nav\"", "</header>");
            var hub = await api.Pages.GetBySlugAsync<PageInfo>(TrongCatalogSeed.HubSlug, siteB.Id);
            var trongAt = nav.IndexOf($"href=\"{hub!.Permalink}\"", StringComparison.Ordinal);
            var thungAt = nav.IndexOf($"class=\"sb-nav__link\" href=\"{archive.Permalink}\"", StringComparison.Ordinal);
            Assert.True(trongAt >= 0 && thungAt > trongAt, "Thùng rượu gỗ must follow Trống in the nav.");
            Assert.DoesNotContain($"sb-sub-{archive.Id:N}", nav);
            Assert.DoesNotContain($"href=\"{bon.Permalink}\"", nav);
        });
    }

    // --- helpers ---

    private async Task WithThrowawaySiteAsync(Func<IApi, Guid, PageBase, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = $"line-seed-test-{suffix}",
            Title = $"Line Seed Test {suffix}",
            Hostnames = $"line-seed-test-{suffix}.local",
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

            await body(api, site.Id, existing);
        }
        finally
        {
            var all = Flatten(await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false)).ToList();
            foreach (var item in all)
            {
                foreach (var postId in await PostIdsAsync(api, item.Id))
                {
                    await api.Posts.DeleteAsync(postId);
                }
            }
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

    /// <summary>
    /// Runs <paramref name="body"/> against the startup-seeded Site B pages,
    /// then restores both pages' and every Thùng rượu post's Published/Price.
    /// </summary>
    private async Task WithSiteBPagesAsync(Func<IApi, Site, PageInfo, PageInfo, IReadOnlyList<PostInfo>, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await api.Sites.GetByInternalIdAsync(SiteSeed.TrongDoiTamInternalId);
        Assert.NotNull(siteB);

        // Startup ran the seed.
        var thung = await api.Pages.GetBySlugAsync<PageInfo>(ProductLineSeed.ThungRuouSlug, siteB!.Id);
        var bon = await api.Pages.GetBySlugAsync<PageInfo>(ProductLineSeed.BonTamSlug, siteB.Id);
        Assert.NotNull(thung);
        Assert.NotNull(bon);
        Assert.Equal(nameof(ProductArchive), thung!.TypeId);
        Assert.Equal(nameof(ProductArchive), bon!.TypeId);

        var posts = (await api.Posts.GetAllAsync<PostInfo>(thung.Id)).ToList();
        var pageStates = new[] { (thung.Id, thung.Published), (bon.Id, bon.Published) };
        var postStates = new List<(Guid Id, DateTime? Published, string? Price)>();
        foreach (var p in posts)
        {
            var full = (await api.Posts.GetByIdAsync<ProductPost>(p.Id))!;
            postStates.Add((full.Id, full.Published, full.Price?.Value));
        }

        try
        {
            await body(api, siteB, thung, bon, posts);
        }
        finally
        {
            foreach (var (id, published, price) in postStates)
            {
                var post = (await api.Posts.GetByIdAsync<ProductPost>(id))!;
                post.Published = published;
                post.Price = price;
                await api.Posts.SaveAsync(post);
            }
            foreach (var (id, published) in pageStates)
            {
                await SetPublishedAsync(api, id, published);
            }
        }
    }

    private static async Task SetPublishedAsync(IApi api, Guid pageId, DateTime? published)
    {
        var page = (await api.Pages.GetByIdAsync<ProductArchive>(pageId))!;
        page.Published = published;
        await api.Pages.SaveAsync(page);
    }

    private static async Task<PageInfo> AssertDraftArchiveAsync(IApi api, Guid siteId, string slug, string title)
    {
        var page = await api.Pages.GetBySlugAsync<PageInfo>(slug, siteId);
        Assert.NotNull(page);
        Assert.Equal(title, page!.Title);
        Assert.Equal(slug, page.Slug);
        Assert.Equal(nameof(ProductArchive), page.TypeId);
        Assert.Null(page.ParentId);
        Assert.Null(page.Published);
        return page;
    }

    private static async Task<List<Guid>> PageIdsAsync(IApi api, Guid siteId) =>
        Flatten(await api.Sites.GetSitemapAsync(siteId, onlyPublished: false)).Select(i => i.Id).OrderBy(i => i).ToList();

    private static async Task<List<Guid>> PostIdsAsync(IApi api, Guid blogId) =>
        (await api.Posts.GetAllAsync<PostInfo>(blogId)).Select(p => p.Id).OrderBy(i => i).ToList();

    private static IEnumerable<SitemapItem> Flatten(IEnumerable<SitemapItem> items) =>
        items.SelectMany(i => new[] { i }.Concat(Flatten(i.Items)));

    private static List<string> Cards(string html)
    {
        var cards = new List<string>();
        var at = 0;
        while ((at = html.IndexOf("<article class=\"sb-card\">", at, StringComparison.Ordinal)) >= 0)
        {
            var end = html.IndexOf("</article>", at, StringComparison.Ordinal);
            Assert.True(end >= 0, "Expected '</article>' after '<article class=\"sb-card\">'.");
            cards.Add(html[at..(end + "</article>".Length)]);
            at = end;
        }
        return cards;
    }

    private static string Decode(string html) => WebUtility.HtmlDecode(html);

    private static string Section(string html, string startMarker, string endMarker)
    {
        var start = html.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected to find '{startMarker}'.");
        var end = html.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(end > start, $"Expected '{endMarker}' after '{startMarker}'.");
        return html[start..(end + endMarker.Length)];
    }

    private static string HostnameOf(Site site)
    {
        var hostname = site.Hostnames?.Split(',').FirstOrDefault()?.Trim();
        Assert.False(string.IsNullOrEmpty(hostname));
        return hostname!;
    }

    private async Task<string> GetAsync(string permalink, string hostname, HttpStatusCode expected)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, permalink);
        request.Headers.Host = hostname;

        var response = await client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }
}
