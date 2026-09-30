using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Piranha.Extend.Blocks;
using Piranha.Extend.Fields;
using TbTruongHoc.Web.Models;
using Xunit;
using static TbTruongHoc.Web.Tests.BlogListingTests;
using static TbTruongHoc.Web.Tests.ProductCatalogTests;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 5.2 (FR-14): the enriched <see cref="BlogPost"/> article page -
/// breadcrumb, reading column, and the "Bài viết liên quan" strip (published
/// posts of the same archive sharing at least one tag) or, with none, the
/// "← Quay lại Blog" back-link. Uses the Story 5.1 throwaway blog builder.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class ArticleDetailTests
{
    private const string RelatedLabel = "Bài viết liên quan";

    private readonly PiranhaWebApplicationFactory _factory;

    public ArticleDetailTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Matrix: Related exist.
    [Fact]
    public async Task Related_Posts_Show_The_Three_Newest_Sharing_A_Tag_As_H3_Cards_Without_BackLink()
    {
        await WithBlogAsync(_factory, async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Related");
            var tag = $"go-mit-{b.Suffix}";
            var baseTime = DateTime.Now.AddDays(-10);
            var current = await b.PostAsync(archive, "Current", publishedAt: baseTime, tags: new[] { tag });
            var r1 = await b.PostAsync(archive, "R1", publishedAt: baseTime.AddHours(1), tags: new[] { tag });
            var r2 = await b.PostAsync(archive, "R2", publishedAt: baseTime.AddHours(2), tags: new[] { tag, $"other-{b.Suffix}" });
            var r3 = await b.PostAsync(archive, "R3", publishedAt: baseTime.AddHours(3), tags: new[] { tag });
            var r4 = await b.PostAsync(archive, "R4", publishedAt: baseTime.AddHours(4), tags: new[] { tag });
            // Same category, no shared tag: category plays no part.
            var untagged = await b.PostAsync(archive, "Untagged", publishedAt: baseTime.AddHours(5));

            var html = await GetAsync(_factory, current.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var related = Section(html, "<section class=\"sb-related\"", "</section>");
            Assert.Contains($"<h2 class=\"sb-related__title\" id=\"sb-related-title\">{RelatedLabel}</h2>", Decode(related));

            var cards = Cards(related);
            Assert.Equal(3, cards.Count);
            var expected = new[] { r4, r3, r2 };
            for (var i = 0; i < expected.Length; i++)
            {
                var card = Decode(cards[i]);
                Assert.Contains($"<h3 class=\"sb-card__title\"><a class=\"sb-card__link\" href=\"{expected[i].Permalink}\">{expected[i].Title}</a></h3>", card);
                Assert.DoesNotContain("<h2", card);
            }

            var decoded = Decode(html);
            Assert.DoesNotContain(r1.Title, Decode(related));
            Assert.DoesNotContain(untagged.Title, decoded);
            Assert.DoesNotContain("sb-backlink", html);
            Assert.DoesNotContain("Quay lại", decoded);
        });
    }

    // Multi-tag merge: per-tag results are de-duplicated and re-sorted newest first.
    [Fact]
    public async Task Related_Posts_From_Several_Tags_Are_Merged_Once_And_Sorted_Newest_First()
    {
        await WithBlogAsync(_factory, async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Multi tag");
            var t1 = $"t1-{b.Suffix}";
            var t2 = $"t2-{b.Suffix}";
            var baseTime = DateTime.Now.AddDays(-10);
            var current = await b.PostAsync(archive, "Current", publishedAt: baseTime, tags: new[] { t1, t2 });
            var z = await b.PostAsync(archive, "Z", publishedAt: baseTime.AddHours(1), tags: new[] { t1 });
            var x = await b.PostAsync(archive, "X", publishedAt: baseTime.AddHours(2), tags: new[] { t1, t2 });
            var y = await b.PostAsync(archive, "Y", publishedAt: baseTime.AddHours(3), tags: new[] { t2 });

            var html = await GetAsync(_factory, current.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var related = Section(html, "<section class=\"sb-related\"", "</section>");
            var hrefs = Cards(related)
                .Select(c => Regex.Match(Decode(c), "<a class=\"sb-card__link\" href=\"([^\"]+)\"").Groups[1].Value)
                .ToList();
            Assert.Equal(new[] { y.Permalink, x.Permalink, z.Permalink }, hrefs);
        });
    }

    // Matrix: No related (no qualifying posts, and a post with no tags).
    [Fact]
    public async Task No_Related_Posts_Omit_The_Module_And_Show_BackLink_To_The_Archive()
    {
        await WithBlogAsync(_factory, async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Lonely");
            var lonelyTagged = await b.PostAsync(archive, "Lonely tagged", publishedAt: DateTime.Now.AddHours(-3),
                category: "Shared", tags: new[] { $"only-me-{b.Suffix}" });
            var noTags = await b.PostAsync(archive, "No tags", publishedAt: DateTime.Now.AddHours(-2), category: "Shared");
            await b.PostAsync(archive, "Other", publishedAt: DateTime.Now.AddHours(-1),
                category: "Shared", tags: new[] { $"someone-else-{b.Suffix}" });

            foreach (var post in new[] { lonelyTagged, noTags })
            {
                var html = await GetAsync(_factory, post.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
                AssertBackLinkOnly(html, archive);
            }
        });
    }

    // Matrix: Only drafts qualify.
    [Fact]
    public async Task Only_Unpublished_Qualifying_Posts_Count_As_No_Related()
    {
        await WithBlogAsync(_factory, async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Drafts");
            var tag = $"draft-tag-{b.Suffix}";
            var current = await b.PostAsync(archive, "Current", publishedAt: DateTime.Now.AddHours(-1), tags: new[] { tag });
            var draft = await b.PostAsync(archive, "Draft", published: false, tags: new[] { tag });
            var future = await b.PostAsync(archive, "Future", publishedAt: DateTime.Now.AddDays(1), tags: new[] { tag });

            var html = await GetAsync(_factory, current.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            AssertBackLinkOnly(html, archive);
            Assert.DoesNotContain(draft.Title, Decode(html));
            Assert.DoesNotContain(future.Title, Decode(html));
        });
    }

    // Matrix: Self excluded.
    [Fact]
    public async Task A_Post_Never_Appears_In_Its_Own_Related_Strip()
    {
        await WithBlogAsync(_factory, async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Self");
            var tag = $"pair-{b.Suffix}";
            var a = await b.PostAsync(archive, "A", publishedAt: DateTime.Now.AddHours(-1), tags: new[] { tag });
            var other = await b.PostAsync(archive, "B", publishedAt: DateTime.Now.AddHours(-2), tags: new[] { tag });

            foreach (var (post, partner) in new[] { (a, other), (other, a) })
            {
                var html = await GetAsync(_factory, post.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
                var related = Section(html, "<section class=\"sb-related\"", "</section>");
                var card = Decode(Assert.Single(Cards(related)));
                Assert.Contains($"href=\"{partner.Permalink}\"", card);
                Assert.DoesNotContain($"href=\"{post.Permalink}\"", Decode(related));
                Assert.DoesNotContain("sb-backlink", html);
            }
        });
    }

    // Matrix: Breadcrumb.
    [Fact]
    public async Task Breadcrumb_Links_Home_And_Archive_And_Marks_The_Current_Title()
    {
        await WithBlogAsync(_factory, async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Crumbs");
            var post = await b.PostAsync(archive, "Crumb post");

            var html = await GetAsync(_factory, post.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var nav = Decode(Section(html, "<nav class=\"sb-breadcrumb\"", "</nav>"));
            var items = Regex.Matches(nav, "<li class=\"sb-breadcrumb__item\">(.*?)</li>", RegexOptions.Singleline)
                .Select(m => m.Groups[1].Value).ToList();
            Assert.Equal(3, items.Count);
            Assert.Equal("<a class=\"sb-breadcrumb__link\" href=\"/\">Trang chủ</a>", items[0]);
            Assert.Equal($"<a class=\"sb-breadcrumb__link\" href=\"{archive.Permalink}\">{archive.Title}</a>", items[1]);
            Assert.Equal($"<span class=\"sb-breadcrumb__current\" aria-current=\"page\">{post.Title}</span>", items[2]);
            Assert.Contains("aria-label=\"Đường dẫn trang\"", nav);
        });
    }

    // Matrix: Subheads + AC: single h1, muted date, body in the reading column.
    [Fact]
    public async Task Body_With_Subheads_Renders_In_The_Reading_Column_Under_A_Single_H1()
    {
        await WithBlogAsync(_factory, async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Subheads");
            var marker = $"subhead-marker-{b.Suffix}";
            var post = await b.PostAsync(archive, "Long read", publishedAt: new DateTime(2026, 4, 5, 8, 0, 0),
                blockHtml: $"<h2>Chọn gỗ {marker}</h2><p>Đoạn một.</p><h3>Phơi gỗ {marker}</h3><p>Đoạn hai.</p>");

            var html = await GetAsync(_factory, post.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var main = Section(html, "<main class=\"sb-article\"", "</main>");
            Assert.Single(Regex.Matches(html, "<h1\\b"));
            Assert.Contains($"<h1 class=\"sb-article__title\">{post.Title}</h1>", Decode(main));
            Assert.Contains("<time class=\"sb-article__date\" datetime=\"2026-04-05\">05/04/2026</time>", main);

            var column = Decode(Section(html, "<div class=\"sb-article__column\">", "sb-backlink"));
            var body = column[column.IndexOf("<div class=\"sb-article__body\">", StringComparison.Ordinal)..];
            Assert.Contains($"<h2>Chọn gỗ {marker}</h2>", body);
            Assert.Contains($"<h3>Phơi gỗ {marker}</h3>", body);
        });
    }

    // Gallery fix: each photo honours its ImageBlock Aspect ("Original" never
    // crops, no fixed 1100x450 crop) and carries the contained-frame class.
    [Fact]
    public async Task Gallery_Photos_Follow_Their_Aspect_Setting_And_Are_Never_Force_Cropped()
    {
        await WithBlogAsync(_factory, async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Gallery");
            var original = await b.UploadAsync();
            var landscape = await b.UploadAsync();
            var gallery = new ImageGalleryBlock();
            gallery.Items.Add(new ImageBlock { Body = original });
            gallery.Items.Add(new ImageBlock
            {
                Body = landscape,
                Aspect = new SelectField<ImageAspect> { Value = ImageAspect.Landscape }
            });
            var post = await b.PostAsync(archive, "Gallery post", blocks: new[] { gallery });

            var html = await GetAsync(_factory, post.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var carousel = Section(html, "<div class=\"carousel-inner\">", "carousel-control-prev");
            var imgs = Regex.Matches(carousel, "<img [^>]*>").Select(m => m.Value).ToList();
            Assert.Equal(2, imgs.Count);
            Assert.All(imgs, img => Assert.Contains("gallery-block__image", img));
            Assert.DoesNotContain("x450", carousel);

            // Original: width-only version, the photo keeps its own ratio.
            Assert.Matches($"src=\"[^\"]*{original}[^\"]*_1100[.]png\"", imgs[0]);
            // Landscape: cropped to 3:2 at the same width.
            Assert.Matches($"src=\"[^\"]*{landscape}[^\"]*_1100x733[.]png\"", imgs[1]);
        });
    }

    // Listing cards keep their h2 titles (the h3 switch is related-strip only).
    [Fact]
    public async Task Listing_Cards_Still_Render_H2_Titles()
    {
        await WithBlogAsync(_factory, async (api, siteB, b) =>
        {
            var archive = await b.ArchiveAsync("Listing h2");
            var tag = $"list-{b.Suffix}";
            var first = await b.PostAsync(archive, "First", publishedAt: DateTime.Now.AddHours(-2), tags: new[] { tag });
            var second = await b.PostAsync(archive, "Second", publishedAt: DateTime.Now.AddHours(-1), tags: new[] { tag });

            var html = await GetAsync(_factory, archive.Permalink, HostnameOf(siteB), HttpStatusCode.OK);
            var cards = Cards(html);
            Assert.Equal(2, cards.Count);
            foreach (var (card, post) in cards.Zip(new[] { second, first }))
            {
                Assert.Contains($"<h2 class=\"sb-card__title\"><a class=\"sb-card__link\" href=\"{post.Permalink}\">{post.Title}</a></h2>", Decode(card));
                Assert.DoesNotContain("<h3", card);
            }
            Assert.DoesNotContain("sb-related", html);
        });
    }

    private static void AssertBackLinkOnly(string html, BlogArchive archive)
    {
        var decoded = Decode(html);
        Assert.DoesNotContain("sb-related", html);
        Assert.DoesNotContain("data-sb-related", html);
        Assert.DoesNotContain(RelatedLabel, decoded);
        Assert.DoesNotContain("<article class=\"sb-card\">", html);
        Assert.Contains($"<p class=\"sb-backlink\"><a class=\"sb-backlink__link\" href=\"{archive.Permalink}\"><span aria-hidden=\"true\">←</span> Quay lại {archive.Title}</a></p>", decoded);
    }
}
