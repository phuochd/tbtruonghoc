using System;
using System.Collections.Generic;
using System.IO;
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
using Xunit;
using static TbTruongHoc.Web.Tests.ProductCatalogTests;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 5.1 (FR-14): the <see cref="BlogArchive"/> listing and minimal
/// <see cref="BlogPost"/> page, plus <see cref="BlogSeed"/>. Render tests
/// build their own throwaway archive/posts on Site B (unique slugs) and
/// delete them after; seed tests run on a throwaway site via the internal
/// overload. The startup-seeded "blog" page is only read.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class BlogListingTests
{
    private const int NonStartPageSortOrder = 1;
    private const string EmptyMessage = "Bài viết đang được cập nhật";

    private readonly PiranhaWebApplicationFactory _factory;

    public BlogListingTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Matrix: Listing.
    [Fact]
    public async Task Listing_Shows_Published_Posts_Newest_First_As_Single_Link_Cards()
    {
        await WithBlogAsync(async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Listing");
            var oldest = await b.PostAsync(archive, "Oldest", publishedAt: new DateTime(2026, 1, 5, 9, 0, 0));
            var middle = await b.PostAsync(archive, "Middle", publishedAt: new DateTime(2026, 3, 7, 9, 0, 0));
            var newest = await b.PostAsync(archive, "Newest", publishedAt: new DateTime(2026, 9, 1, 9, 0, 0));

            var html = await GetAsync(archive.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            Assert.Contains("data-sb-blog-grid", html);
            Assert.Contains("class=\"sb-grid sb-grid--blog\"", html);
            Assert.DoesNotContain("sb-pager", html);
            Assert.DoesNotContain(EmptyMessage, Decode(html));

            var cards = Cards(html);
            Assert.Equal(3, cards.Count);
            var expected = new[] { newest, middle, oldest };
            for (var i = 0; i < expected.Length; i++)
            {
                var card = Decode(cards[i]);
                var post = expected[i];
                Assert.Contains(post.Title, card);

                // One tap target: a single stretched title link, no CTA/price/eyebrow.
                var hrefs = Regex.Matches(card, "<a [^>]*href=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();
                Assert.Equal(new[] { post.Permalink }, hrefs);
                Assert.Contains($"<a class=\"sb-card__link\" href=\"{post.Permalink}\">{post.Title}</a>", card);
                Assert.DoesNotContain("sb-card__cta", card);
                Assert.DoesNotContain("sb-card__price", card);
                Assert.DoesNotContain("sb-card__eyebrow", card);
                Assert.DoesNotContain("Liên hệ báo giá", card);
            }

            var newestCard = Decode(cards[0]);
            Assert.Contains("<time class=\"sb-card__date\" datetime=\"2026-09-01\">01/09/2026</time>", newestCard);
            Assert.Contains("<time class=\"sb-card__date\" datetime=\"2026-01-05\">05/01/2026</time>", Decode(cards[2]));
        });
    }

    // Matrix: Paging.
    [Fact]
    public async Task Listing_Pages_Twelve_Per_Page_With_Prev_Next_Links()
    {
        await WithBlogAsync(async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Paging");
            var start = DateTime.Now.AddDays(-30);
            for (var i = 0; i < 13; i++)
            {
                await b.PostAsync(archive, $"Item {i:D2}", publishedAt: start.AddHours(i));
            }

            var page1 = await GetAsync(archive.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            Assert.Equal(12, Cards(page1).Count);
            var pager1 = Decode(Section(page1, "<nav class=\"sb-pager\"", "</nav>"));
            Assert.Contains($"href=\"{archive.Permalink}/page/2\" rel=\"next\">Trang sau</a>", pager1);
            Assert.Contains("Trang 1 / 2", pager1);
            Assert.DoesNotContain("Trang trước", pager1);

            var page2 = await GetAsync($"{archive.Permalink}/page/2", HostnameOf(siteB), HttpStatusCode.OK);
            var cards2 = Cards(page2);
            Assert.Single(cards2);
            // Newest first: the oldest post lands alone on page 2.
            Assert.Contains($"Item 00 {b.Suffix}", Decode(cards2[0]));
            var pager2 = Decode(Section(page2, "<nav class=\"sb-pager\"", "</nav>"));
            Assert.Contains($"href=\"{archive.Permalink}\" rel=\"prev\">Trang trước</a>", pager2);
            Assert.Contains("Trang 2 / 2", pager2);
            Assert.DoesNotContain("Trang sau", pager2);
        });
    }

    // Matrix: Empty.
    [Fact]
    public async Task Empty_Listing_Shows_Updating_Message_Without_Grid_Or_Pager()
    {
        await WithBlogAsync(async (api, siteB, b) =>
        {
            var marker = $"blog-block-marker-{b.Suffix}";
            var archive = await b.ArchiveAsync("Empty", blockHtml: $"<p>{marker}</p>");
            var draft = await b.PostAsync(archive, "Draft only", published: false);
            var future = await b.PostAsync(archive, "Future", publishedAt: DateTime.Now.AddDays(1));

            var html = await GetAsync(archive.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var decoded = Decode(html);
            Assert.Contains($"<h1>{archive.Title}</h1>", decoded);
            Assert.Contains(marker, html);
            Assert.Contains($"<p class=\"sb-empty\">{EmptyMessage}</p>", decoded);
            Assert.DoesNotContain("data-sb-blog-grid", html);
            Assert.DoesNotContain("sb-card", html);
            Assert.DoesNotContain("sb-pager", html);
            Assert.DoesNotContain(draft.Title, decoded);
            Assert.DoesNotContain(future.Title, decoded);
        });
    }

    // Matrix: No image / excerpt.
    [Fact]
    public async Task Card_Without_Image_Or_Excerpt_Shows_Gradient_Frame_And_Omits_Excerpt()
    {
        await WithBlogAsync(async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Bare");
            var bare = await b.PostAsync(archive, "Bare", publishedAt: DateTime.Now.AddHours(-2));
            var withExcerpt = await b.PostAsync(archive, "With excerpt", excerpt: "Cách giữ gỗ bền đẹp", publishedAt: DateTime.Now.AddHours(-1));

            var html = await GetAsync(archive.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var cards = Cards(html);
            Assert.Equal(2, cards.Count);

            var bareCard = Decode(cards.Single(c => Decode(c).Contains(bare.Title)));
            Assert.Contains("<div class=\"sb-card__thumb\">", bareCard);
            Assert.DoesNotContain("<img", bareCard);
            Assert.DoesNotContain("sb-card__desc", bareCard);

            var excerptCard = Decode(cards.Single(c => Decode(c).Contains(withExcerpt.Title)));
            Assert.Contains("<p class=\"sb-card__desc\">Cách giữ gỗ bền đẹp</p>", excerptCard);
        });
    }

    // Matrix: Post page + AC: the post and the listing each carry their own meta.
    [Fact]
    public async Task Post_Page_Renders_Title_Date_Blocks_And_Own_Meta_While_Listing_Keeps_Archive_Meta()
    {
        await WithBlogAsync(async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Meta", metaTitle: $"Blog meta {b.Suffix}", metaDescription: $"Blog description {b.Suffix}");
            var marker = $"post-body-marker-{b.Suffix}";
            var post = await b.PostAsync(archive, "Article", publishedAt: new DateTime(2026, 2, 3, 10, 0, 0),
                blockHtml: $"<p>{marker}</p>", metaTitle: $"Post meta {b.Suffix}", metaDescription: $"Post description {b.Suffix}");

            var postHtml = await GetAsync(post.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var decodedPost = Decode(postHtml);
            var main = Decode(Section(postHtml, "<main class=\"sb-article\"", "</main>"));
            Assert.Contains($"<h1 class=\"sb-article__title\">{post.Title}</h1>", main);
            Assert.Contains("<time class=\"sb-article__date\" datetime=\"2026-02-03\">03/02/2026</time>", main);
            Assert.Contains(marker, main);
            Assert.Contains($"<title>Post meta {b.Suffix}</title>", decodedPost);
            Assert.Contains($"<meta name=\"description\" content=\"Post description {b.Suffix}\">", decodedPost);
            Assert.DoesNotContain($"Blog meta {b.Suffix}", decodedPost);
            // Site layout chrome (nav) is present.
            Assert.Contains("<header class=\"sb-nav\"", postHtml);

            var listHtml = Decode(await GetAsync(archive.Permalink, HostnameOf(siteB), HttpStatusCode.OK));
            Assert.Contains($"<title>Blog meta {b.Suffix}</title>", listHtml);
            Assert.Contains($"<meta name=\"description\" content=\"Blog description {b.Suffix}\">", listHtml);
            Assert.DoesNotContain($"Post meta {b.Suffix}", listHtml);
        });
    }

    // Matrix: Post page, unpublished -> 404.
    [Fact]
    public async Task Unpublished_Post_Returns_404()
    {
        await WithBlogAsync(async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Draft post");
            var draft = await b.PostAsync(archive, "Draft", published: false);

            await GetAsync(draft.Permalink, HostnameOf(siteB), HttpStatusCode.NotFound);
        });
    }

    [Fact]
    public async Task Unpublished_Archive_Returns_404()
    {
        await WithBlogAsync(async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Draft archive", published: false);

            await GetAsync(archive.Permalink, HostnameOf(siteB), HttpStatusCode.NotFound);
        });
    }

    [Fact]
    public async Task Card_With_Image_Renders_One_Resized_Img_With_AltText_Or_Title()
    {
        await WithBlogAsync(async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Images");
            var withAltMedia = await b.UploadAsync(altText: "Mặt trống gỗ mít");
            var noAltMedia = await b.UploadAsync();
            var withAlt = await b.PostAsync(archive, "With alt", primaryImage: withAltMedia, publishedAt: DateTime.Now.AddHours(-2));
            var noAlt = await b.PostAsync(archive, "No alt", primaryImage: noAltMedia, publishedAt: DateTime.Now.AddHours(-1));

            var html = await GetAsync(archive.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var cards = Cards(html);
            Assert.Equal(2, cards.Count);

            foreach (var (post, alt) in new[] { (withAlt, "Mặt trống gỗ mít"), (noAlt, noAlt.Title) })
            {
                var card = Decode(cards.Single(c => Decode(c).Contains(post.Title)));
                var imgs = Regex.Matches(card, "<img\\b[^>]*>").Select(m => m.Value).ToList();
                var img = Assert.Single(imgs);
                Assert.Matches("src=\"[^\"]*_600x450[^\"]*\"", img);
                Assert.Contains($"alt=\"{alt}\"", img);
            }
        });
    }

    [Fact]
    public async Task Config_Block_Never_Renders_On_Blog_Archive_Or_Post()
    {
        await WithBlogAsync(async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Config", configBlock: ConfigBlockWithRow());
            var post = await b.PostAsync(archive, "Config post", configBlock: ConfigBlockWithRow());

            foreach (var permalink in new[] { archive.Permalink, post.Permalink })
            {
                var html = await GetAsync(permalink, HostnameOf(siteB), HttpStatusCode.OK);
                Assert.DoesNotContain("sb-config", html);
                Assert.DoesNotContain("config-block", html);
                Assert.DoesNotContain("Bảng tham khảo, không tính giá tự động", Decode(html));
                Assert.DoesNotContain(">" + ConfigBlock.DefaultHeading + "<", Decode(html));
                Assert.DoesNotContain("Cấu hình blog", Decode(html));
                Assert.DoesNotContain("sb-config.js", html);
            }
        });
    }

    // Matrix: Seed rerun (+ fresh seed shape).
    [Fact]
    public async Task Seed_Creates_One_Published_Visible_Blog_Archive_And_Rerun_Changes_Nothing()
    {
        await WithThrowawaySiteAsync(async (api, siteId, existing) =>
        {
            await BlogSeed.EnsureSeededAsync(api, siteId);

            var sitemap = await api.Sites.GetSitemapAsync(siteId, onlyPublished: false);
            Assert.Equal(2, sitemap.Count);
            Assert.Equal(existing.Id, sitemap[0].Id);

            var info = await api.Pages.GetBySlugAsync<PageInfo>("blog", siteId);
            Assert.NotNull(info);
            Assert.Equal(info!.Id, sitemap[1].Id);
            var seeded = (await api.Pages.GetByIdAsync<BlogArchive>(info.Id))!;
            Assert.Equal(nameof(BlogArchive), seeded.TypeId);
            Assert.Equal("Blog", seeded.Title);
            Assert.Null(seeded.ParentId);
            Assert.False(seeded.IsHidden);
            Assert.True(seeded.Published.HasValue && seeded.Published.Value <= DateTime.Now);
            Assert.True(seeded.SortOrder > existing.SortOrder);
            Assert.Empty(seeded.Blocks);
            Assert.Empty(await api.Posts.GetAllAsync<PostInfo>(seeded.Id));

            // An editor edits the page; re-runs create and change nothing.
            var renamed = $"Tin tức {Guid.NewGuid():N}";
            seeded.Title = renamed;
            seeded.IsHidden = true;
            await api.Pages.SaveAsync(seeded);

            await BlogSeed.EnsureSeededAsync(api, siteId);
            await BlogSeed.EnsureSeededAsync(api, siteId);

            sitemap = await api.Sites.GetSitemapAsync(siteId, onlyPublished: false);
            Assert.Equal(2, sitemap.Count);
            var reloaded = (await api.Pages.GetByIdAsync<PageInfo>(info.Id))!;
            Assert.Equal(renamed, reloaded.Title);
            Assert.True(reloaded.IsHidden);

            // An editor renames the slug: still no second Blog page.
            var edited = (await api.Pages.GetByIdAsync<BlogArchive>(info.Id))!;
            edited.Slug = $"tin-tuc-{Guid.NewGuid():N}";
            await api.Pages.SaveAsync(edited);

            await BlogSeed.EnsureSeededAsync(api, siteId);

            sitemap = await api.Sites.GetSitemapAsync(siteId, onlyPublished: false);
            Assert.Equal(2, sitemap.Count);
            var blogArchives = (await api.Pages.GetAllAsync<PageInfo>(siteId))
                .Where(p => p.TypeId == nameof(BlogArchive)).ToList();
            Assert.Equal(info.Id, Assert.Single(blogArchives).Id);
        });
    }

    // Seed goes on Site B only; the nav shows Blog even with zero posts.
    [Fact]
    public async Task Startup_Seeded_Blog_On_Site_B_Only_And_It_Is_In_The_Nav()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);

        var info = await api.Pages.GetBySlugAsync<PageInfo>("blog", siteB.Id);
        Assert.NotNull(info);
        Assert.Equal(nameof(BlogArchive), info!.TypeId);

        var siteAPages = Flatten(await api.Sites.GetSitemapAsync(siteA.Id, onlyPublished: false)).ToList();
        foreach (var item in siteAPages)
        {
            var page = await api.Pages.GetByIdAsync<PageInfo>(item.Id);
            Assert.NotEqual(nameof(BlogArchive), page!.TypeId);
        }

        // The render checks need the page as seeded (published, visible);
        // skip them if an editor has since changed that on this database.
        if (info.IsHidden || info.Published == null || info.Published > DateTime.Now)
        {
            return;
        }

        var html = await GetAsync(info.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
        var nav = Decode(Section(html, "<header class=\"sb-nav\"", "</header>"));
        Assert.Contains($"href=\"{info.Permalink}\" aria-current=\"page\">{(string.IsNullOrWhiteSpace(info.NavigationTitle) ? info.Title : info.NavigationTitle)}</a>", nav);

        var published = (await api.Posts.GetAllAsync<PostInfo>(info.Id))
            .Count(p => p.Published.HasValue && p.Published.Value <= DateTime.Now);
        if (published == 0)
        {
            Assert.Contains($"<p class=\"sb-empty\">{EmptyMessage}</p>", Decode(html));
        }
    }

    // --- helpers ---

    // 220x90 solid PNG (a 1x1 source can't be resized to the card's 600x450 crop).
    private static readonly byte[] WidePng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAANwAAABaCAIAAABc0a8TAAAAwElEQVR42u3SQQ0AAAjEsPODIyxhmuCCR5MqWJbpglciAaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMaUKmBJMiSnBlJgSTIkpwZRgSkwJpsSUYEpMCabElGBKMCWmBFNiSjAlpgRTgikxJZgSU4IpMSWYElOCKcGUmBJMiSnBlJgSTAmmxJRgSkwJpsSUYEpMCaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMSWYEkyJKcGUmBJMiSnBlHBTLjqcMk/EWR93AAAAAElFTkSuQmCC");

    private static ConfigBlock ConfigBlockWithRow()
    {
        var block = new ConfigBlock { Heading = "Cấu hình blog" };
        block.Items.Add(new ConfigRowBlock
        {
            Label = "Loại",
            InputType = new SelectField<ConfigInputType> { Value = ConfigInputType.Option },
            Choices = "1\n2"
        });
        return block;
    }

    internal sealed class BlogBuilder
    {
        private readonly IApi _api;
        private readonly IServiceProvider _services;
        private readonly Site _site;
        private readonly List<Guid> _pages = new();
        private readonly List<Guid> _posts = new();
        private readonly List<Guid> _media = new();

        public BlogBuilder(IApi api, IServiceProvider services, Site site)
        {
            _api = api;
            _services = services;
            _site = site;
        }

        public string Suffix { get; } = Guid.NewGuid().ToString("N")[..8];

        public async Task<BlogArchive> ArchiveAsync(string title, string? blockHtml = null,
            string? metaTitle = null, string? metaDescription = null, bool published = true,
            ConfigBlock? configBlock = null)
        {
            var archive = await _api.Pages.CreateAsync<BlogArchive>();
            archive.SiteId = _site.Id;
            archive.SortOrder = NonStartPageSortOrder;
            archive.Title = $"{title} {Suffix}";
            archive.Slug = $"bl-blog-{Guid.NewGuid():N}";
            archive.MetaTitle = metaTitle;
            archive.MetaDescription = metaDescription;
            archive.Published = published ? DateTime.Now.AddMinutes(-5) : null;
            if (blockHtml != null)
            {
                archive.Blocks.Add(new HtmlBlock { Body = blockHtml });
            }
            if (configBlock != null)
            {
                archive.Blocks.Add(configBlock);
            }
            await _api.Pages.SaveAsync(archive);
            _pages.Add(archive.Id);
            return (await _api.Pages.GetByIdAsync<BlogArchive>(archive.Id))!;
        }

        public async Task<BlogPost> PostAsync(PageBase archive, string title, bool published = true,
            string? excerpt = null, DateTime? publishedAt = null, string? blockHtml = null,
            string? metaTitle = null, string? metaDescription = null, Guid? primaryImage = null,
            ConfigBlock? configBlock = null, string? category = null, IEnumerable<string>? tags = null)
        {
            var post = await _api.Posts.CreateAsync<BlogPost>();
            post.BlogId = archive.Id;
            post.Category = category ?? "General";
            foreach (var tag in tags ?? Enumerable.Empty<string>())
            {
                post.Tags.Add(tag);
            }
            post.Title = $"{title} {Suffix}";
            post.Slug = $"bl-post-{Guid.NewGuid():N}";
            post.Excerpt = excerpt;
            post.MetaTitle = metaTitle;
            post.MetaDescription = metaDescription;
            post.Published = published ? publishedAt ?? DateTime.Now.AddMinutes(-1) : null;
            if (primaryImage != null)
            {
                post.PrimaryImage = primaryImage.Value;
            }
            if (blockHtml != null)
            {
                post.Blocks.Add(new HtmlBlock { Body = blockHtml });
            }
            if (configBlock != null)
            {
                post.Blocks.Add(configBlock);
            }
            await _api.Posts.SaveAsync(post);
            _posts.Add(post.Id);
            return (await _api.Posts.GetByIdAsync<BlogPost>(post.Id))!;
        }

        public async Task<Guid> UploadAsync(string? altText = null)
        {
            using var stream = new MemoryStream(WidePng);
            var content = new StreamMediaContent
            {
                Filename = $"blog-test-{Guid.NewGuid():N}.png",
                Data = stream
            };
            await _api.Media.SaveAsync(content);
            var id = content.Id!.Value;
            _media.Add(id);

            if (altText != null)
            {
                var media = (await _api.Media.GetByIdAsync(id))!;
                media.AltText = altText;
                await _api.Media.SaveAsync(media);
            }
            return id;
        }

        public async Task CleanupAsync()
        {
            try
            {
                foreach (var id in _posts)
                {
                    await _api.Posts.DeleteAsync(id);
                }
                for (var i = _pages.Count - 1; i >= 0; i--)
                {
                    await _api.Pages.DeleteAsync(_pages[i]);
                }
            }
            finally
            {
                // Fresh scope: the render may have added resized versions
                // through another DbContext (see ProductDetailPageTests).
                using var scope = _services.CreateScope();
                var api = scope.ServiceProvider.GetRequiredService<IApi>();
                foreach (var id in _media)
                {
                    await api.Media.DeleteAsync(id);
                }
            }
        }
    }

    private Task WithBlogAsync(Func<IApi, Site, BlogBuilder, Task> body) => WithBlogAsync(_factory, body);

    /// <summary>
    /// Runs <paramref name="body"/> with a <see cref="BlogBuilder"/> on Site B
    /// and deletes everything it built afterwards (shared with Story 5.2).
    /// </summary>
    internal static async Task WithBlogAsync(PiranhaWebApplicationFactory factory, Func<IApi, Site, BlogBuilder, Task> body)
    {
        using var scope = factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var builder = new BlogBuilder(api, factory.Services, siteB);

        try
        {
            await body(api, siteB, builder);
        }
        finally
        {
            await builder.CleanupAsync();
        }
    }

    private async Task WithThrowawaySiteAsync(Func<IApi, Guid, PageBase, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = $"blog-seed-test-{suffix}",
            Title = $"Blog Seed Test {suffix}",
            Hostnames = $"blog-seed-test-{suffix}.local",
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

    internal static List<string> Cards(string html) =>
        Regex.Matches(html, "<article class=\"sb-card\">.*?</article>", RegexOptions.Singleline)
            .Select(m => m.Value).ToList();

    private static IEnumerable<SitemapItem> Flatten(IEnumerable<SitemapItem> items) =>
        items.SelectMany(i => new[] { i }.Concat(Flatten(i.Items)));

    private Task<string> GetAsync(string permalink, string hostname, HttpStatusCode expected) =>
        GetAsync(_factory, permalink, hostname, expected);

    internal static async Task<string> GetAsync(PiranhaWebApplicationFactory factory, string permalink, string hostname,
        HttpStatusCode expected)
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, permalink);
        request.Headers.Host = hostname;

        var response = await client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }
}
