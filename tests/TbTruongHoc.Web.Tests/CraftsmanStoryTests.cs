using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using Piranha.Extend.Blocks;
using Piranha.Extend.Fields;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using TbTruongHoc.Web.Services;
using Xunit;
using static TbTruongHoc.Web.Tests.ProductCatalogTests;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 2.5: the craftsman story page (<c>Views/Cms/CraftsmanStory.cshtml</c>),
/// the shared trust-block and the Site B footer link - one test per I/O &amp;
/// Edge-Case Matrix row, the seed, and the page-wide acceptance checks on
/// every render. Real HTTP render against the MariaDB-backed app. Each test
/// builds its own throwaway pages/media and deletes them afterwards. Any
/// story page already published on Site B (e.g. the seeded one, once the
/// client publishes it) is unpublished for the test and restored after, so
/// no test depends on the seeded draft.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class CraftsmanStoryTests
{
    private const int NonStartPageSortOrder = 1;

    // 1x1 transparent PNG.
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private static readonly byte[] TinyVtt = Encoding.UTF8.GetBytes("WEBVTT\n\n00:00.000 --> 00:01.000\nXin chào\n");

    // Not a playable video - the page only links to the file.
    private static readonly byte[] FakeMp4 = { 0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'m', (byte)'p', (byte)'4', (byte)'2' };

    private readonly PiranhaWebApplicationFactory _factory;

    public CraftsmanStoryTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Matrix: No story page.
    [Fact]
    public async Task Draft_Or_Future_Story_Page_Renders_No_Trust_Block_Or_Footer_Link()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            var draft = await f.StoryAsync("Draft Story", published: false, quote: "Draft quote", attribution: "Nghệ nhân");
            var future = await f.StoryAsync("Future Story", publishedAt: DateTime.Now.AddDays(2), quote: "Future quote", attribution: "Nghệ nhân");
            var (hub, archive, post) = await f.CatalogAsync();

            foreach (var permalink in new[] { hub.Permalink, archive.Permalink, post.Permalink })
            {
                var html = await GetHtmlAsync(permalink, HostnameOf(siteB));
                Assert.DoesNotContain("sb-trust", html);
                Assert.DoesNotContain("sb-footer__story", html);
                Assert.DoesNotContain($"href=\"{draft.Permalink}\"", html);
                Assert.DoesNotContain($"href=\"{future.Permalink}\"", html);
                Assert.DoesNotContain("Draft quote", html);
                Assert.DoesNotContain("Future quote", html);
                AssertPageWideRules(html);
            }

            Assert.Null(await LookupAsync(siteB.Id));
        });
    }

    // Matrix: Story, quote set.
    [Fact]
    public async Task Published_Story_With_Quote_Shows_Trust_Block_On_Pdp_Archive_Hub_And_Footer_Link()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            // Hidden from nav still counts.
            var story = await f.StoryAsync("Quote Story", isHidden: true,
                quote: "Gỗ <b>mít</b> & da trâu", attribution: "Nghệ nhân <i>Trong</i>");
            var (hub, archive, post) = await f.CatalogAsync(withPostBlock: true);

            foreach (var permalink in new[] { hub.Permalink, archive.Permalink, post.Permalink })
            {
                var html = await GetHtmlAsync(permalink, HostnameOf(siteB));
                var main = Section(html, "<main", "</main>");
                var trust = Section(main, "<aside class=\"sb-trust\"", "</aside>");

                Assert.Single(Regex.Matches(html, "class=\"sb-trust\""));
                Assert.Contains("<blockquote class=\"sb-trust__quote\">", trust);
                // Encoded: the raw markup never contains the editor's tags.
                Assert.Contains("&lt;b&gt;", trust);
                Assert.Contains("&lt;i&gt;", trust);
                Assert.DoesNotContain("<b>", trust);
                Assert.DoesNotContain("<i>", trust);
                Assert.Contains("<p>Gỗ <b>mít</b> & da trâu</p>", Decode(trust));
                Assert.Contains("<figcaption class=\"sb-trust__attribution\">Nghệ nhân <i>Trong</i></figcaption>", Decode(trust));

                var link = Regex.Match(trust, "<a class=\"sb-trust__link\"[^>]*>").Value;
                Assert.Equal(story.Permalink, AttrOf(link, "href"));
                Assert.False(string.IsNullOrWhiteSpace(AttrOf(link, "aria-label")));
                Assert.Contains("Câu chuyện nghệ nhân →", Decode(trust));
                // Quote, attribution, then the link.
                Assert.True(trust.IndexOf("sb-trust__quote", StringComparison.Ordinal) < trust.IndexOf("sb-trust__attribution", StringComparison.Ordinal));
                Assert.True(trust.IndexOf("sb-trust__attribution", StringComparison.Ordinal) < trust.IndexOf("sb-trust__link", StringComparison.Ordinal));

                var footer = Section(html, "<footer class=\"sb-footer\"", "</footer>");
                Assert.Contains($"<p class=\"sb-footer__story\"><a href=\"{story.Permalink}\">Câu chuyện nghệ nhân</a></p>", Decode(footer));
                AssertPageWideRules(html);
            }

            // Placement: PDP - after the blocks, directly before the quote form.
            var pdp = Section(await GetHtmlAsync(post.Permalink, HostnameOf(siteB)), "<main", "</main>");
            var trustAt = pdp.IndexOf("class=\"sb-trust\"", StringComparison.Ordinal);
            Assert.True(pdp.IndexOf("Pdp block marker", StringComparison.Ordinal) < trustAt);
            Assert.True(trustAt < pdp.IndexOf("sb-pdp__form", StringComparison.Ordinal));
            Assert.True(trustAt < pdp.IndexOf("data-quote-request-form", StringComparison.Ordinal));

            // Archive: after the product grid and the Story 4.1 drum config.
            // Hub: after the tile grid.
            using (var scope = _factory.Services.CreateScope())
            {
                var scopedApi = scope.ServiceProvider.GetRequiredService<IApi>();
                var a = (await scopedApi.Pages.GetByIdAsync<ProductArchive>(archive.Id))!;
                a.DrumConfig.Add(new DrumConfigRow { Label = "Loại", Value = "Loại 1" });
                await scopedApi.Pages.SaveAsync(a);
            }
            var archiveMain = Section(await GetHtmlAsync(archive.Permalink, HostnameOf(siteB)), "<main", "</main>");
            var gridAt = archiveMain.IndexOf("data-sb-product-grid", StringComparison.Ordinal);
            var configAt = archiveMain.IndexOf("data-sb-config", StringComparison.Ordinal);
            var archiveTrustAt = archiveMain.IndexOf("class=\"sb-trust\"", StringComparison.Ordinal);
            Assert.True(gridAt >= 0, "Expected the product grid.");
            Assert.True(configAt > gridAt, "Config must follow the product grid.");
            Assert.True(archiveTrustAt > configAt, "Trust-block must follow the config.");
            var hubMain = Section(await GetHtmlAsync(hub.Permalink, HostnameOf(siteB)), "<main", "</main>");
            Assert.True(hubMain.IndexOf("data-sb-tile-grid", StringComparison.Ordinal) < hubMain.IndexOf("class=\"sb-trust\"", StringComparison.Ordinal));
        });
    }

    // Matrix: Story, no quote (and quote without attribution).
    [Theory]
    [InlineData("   ", "Nghệ nhân Phạm Trí Trong")]
    [InlineData("Một câu nói", "  ")]
    public async Task Story_Without_Both_Quote_And_Attribution_Shows_Link_Only(string quote, string attribution)
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            var story = await f.StoryAsync("Linkonly Story", quote: quote, attribution: attribution);
            var (hub, archive, post) = await f.CatalogAsync();

            foreach (var permalink in new[] { hub.Permalink, archive.Permalink, post.Permalink })
            {
                var html = await GetHtmlAsync(permalink, HostnameOf(siteB));
                var trust = Section(html, "<aside class=\"sb-trust\"", "</aside>");
                Assert.DoesNotContain("<blockquote", trust);
                Assert.DoesNotContain("sb-trust__attribution", trust);
                Assert.DoesNotContain("<figure", trust);
                Assert.DoesNotContain("Một câu nói", Decode(trust));
                Assert.Contains($"href=\"{story.Permalink}\"", trust);
                Assert.Contains("sb-footer__story", html);
                AssertPageWideRules(html);
            }
        });
    }

    // Matrix: Bare story page.
    [Fact]
    public async Task Bare_Story_Page_Renders_Title_Only()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            await f.SetWorkshopAsync(null, null);
            var story = await f.StoryAsync("Bare Story");

            var html = await GetHtmlAsync(story.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");

            Assert.Contains($"<title>{story.Title}</title>", Decode(html));
            Assert.Contains($"<h1 class=\"sb-story__title\">{story.Title}</h1>", Decode(main));
            Assert.DoesNotContain("<img", main);
            Assert.DoesNotContain("<video", main);
            Assert.DoesNotContain("<figure", main);
            Assert.DoesNotContain("sb-story__eyebrow", main);
            Assert.DoesNotContain("sb-story__photos", main);
            Assert.DoesNotContain("sb-story__workshop", main);
            // Nothing belonging to other pages on the story page itself.
            Assert.DoesNotContain("sb-trust", main);
            Assert.DoesNotContain("breadcrumb", main, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sb-grid", main);
            AssertPageWideRules(html);
        });
    }

    [Fact]
    public async Task Story_Page_Shows_Eyebrow_Blocks_And_Workshop_Section_In_Order()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            await f.SetWorkshopAsync("Thôn Đọi Tam, <Duy Tiên>", "https://maps.google.com/?q=doi+tam");
            var story = await f.StoryAsync("Full Story", eyebrow: "Làng nghề Đọi Tam",
                block: "<p>Story block marker</p>");

            var html = await GetHtmlAsync(story.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            var workshop = Section(main, "<section class=\"sb-story__workshop\"", "</section>");

            Assert.Contains("<p class=\"sb-story__eyebrow\">Làng nghề Đọi Tam</p>", Decode(main));
            Assert.Contains("&lt;Duy", workshop);
            Assert.DoesNotContain("<Duy", workshop);
            Assert.Contains("<p class=\"sb-story__address\">Thôn Đọi Tam, <Duy Tiên></p>", Decode(workshop));
            var maps = Regex.Match(workshop, "<a [^>]*>").Value;
            Assert.Equal("https://maps.google.com/?q=doi+tam", Decode(AttrOf(maps, "href")!));
            Assert.Equal("_blank", AttrOf(maps, "target"));
            // WCAG 2.5.3: the accessible name starts with the visible text.
            Assert.Equal("Xem bản đồ xưởng (mở tab mới)", Decode(AttrOf(maps, "aria-label")!));
            Assert.StartsWith("Xem bản đồ", Decode(Regex.Match(workshop, "<a [^>]*>([^<]*)</a>").Groups[1].Value));

            var markers = new[] { "sb-story__eyebrow", "sb-story__title", "Story block marker", "sb-story__workshop" };
            var positions = markers.Select(m => main.IndexOf(m, StringComparison.Ordinal)).ToList();
            Assert.All(positions, p => Assert.True(p >= 0));
            Assert.Equal(positions.OrderBy(p => p), positions);
            AssertPageWideRules(html);

            // Address only: the section stays, without a Maps link.
            await f.SetWorkshopAsync("Chỉ địa chỉ", null);
            main = Section(await GetHtmlAsync(story.Permalink, HostnameOf(siteB)), "<main", "</main>");
            workshop = Section(main, "<section class=\"sb-story__workshop\"", "</section>");
            Assert.Contains("Chỉ địa chỉ", Decode(workshop));
            Assert.DoesNotContain("sb-story__maps", workshop);
        });
    }

    // Matrix: Photos.
    [Fact]
    public async Task Photos_Render_As_Captioned_Figures_In_Order_Skipping_Blank_Items()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            var captioned = await f.UploadAsync();
            var plain = await f.UploadAsync();
            var withAlt = await f.UploadAsync(altText: "Nghệ nhân căng da trống");
            var story = await f.StoryAsync("Photo Story");
            await f.UpdateStoryAsync(story.Id, p =>
            {
                p.Photos.Add(new StoryPhoto { Image = captioned, Caption = "Bên tang trống <mới>" });
                p.Photos.Add(new StoryPhoto { Image = new ImageField(), Caption = "Blank item caption" });
                p.Photos.Add(new StoryPhoto { Image = plain, Caption = "  " });
                p.Photos.Add(new StoryPhoto { Image = withAlt, Caption = "Căng da" });
            });

            var html = await GetHtmlAsync(story.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            Assert.Contains("<div class=\"sb-story__photos\">", main);

            var figures = Regex.Matches(main, "<figure class=\"sb-story__figure\">.*?</figure>", RegexOptions.Singleline).Select(m => m.Value).ToList();
            Assert.Equal(3, figures.Count);
            Assert.Contains(captioned.ToString(), figures[0]);
            Assert.Contains(plain.ToString(), figures[1]);
            Assert.Contains(withAlt.ToString(), figures[2]);

            Assert.Equal(2, Regex.Matches(main, "<figcaption").Count);
            Assert.Contains("<figcaption class=\"sb-story__caption\">Bên tang trống <mới></figcaption>", Decode(figures[0]));
            Assert.DoesNotContain("<figcaption", figures[1]);
            Assert.Contains("&lt;m", figures[0]);
            Assert.DoesNotContain("<m", figures[0]);
            Assert.DoesNotContain("Blank item caption", Decode(main));

            // Alt: media AltText, then caption, then "{Title} – ảnh {n}".
            var imgs = Imgs(main);
            Assert.Equal(3, imgs.Count);
            Assert.Equal("Bên tang trống <mới>", Decode(AttrOf(imgs[0], "alt")!));
            Assert.Equal($"{story.Title} – ảnh 2", Decode(AttrOf(imgs[1], "alt")!));
            Assert.Equal("Nghệ nhân căng da trống", Decode(AttrOf(imgs[2], "alt")!));

            Assert.DoesNotContain("<button", main);
            Assert.DoesNotContain("data-sb-gallery", main);
            Assert.DoesNotContain("<script", main);
            AssertPageWideRules(html);
        });
    }

    // Matrix: Video without captions (captions optional, Phước 2026-09-28).
    [Fact]
    public async Task Video_Without_Captions_Renders_Player_Without_Track()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            var video = await f.UploadAsync(bytes: FakeMp4, extension: ".mp4");
            var story = await f.StoryAsync("Nocaption Story");
            await f.UpdateStoryAsync(story.Id, p => p.Video = video);

            var html = await GetHtmlAsync(story.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            var videoTag = Regex.Match(main, "<video\\b[^>]*>").Value;
            Assert.Matches(@"\scontrols[\s>=]", videoTag);
            Assert.Equal("none", AttrOf(videoTag, "preload"));
            Assert.Contains(video.ToString(), AttrOf(Regex.Match(main, "<source\\b[^>]*>").Value, "src")!);
            Assert.DoesNotContain("<track", main);
            Assert.DoesNotContain("autoplay", main, StringComparison.OrdinalIgnoreCase);
            AssertPageWideRules(html);
        });
    }

    [Fact]
    public async Task Video_With_Non_Vtt_Captions_Document_Renders_Player_Without_Track()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            var video = await f.UploadAsync(bytes: FakeMp4, extension: ".mp4");
            var notVtt = await f.UploadAsync(bytes: Encoding.UTF8.GetBytes("%PDF-1.4 %%EOF"), extension: ".pdf");
            var story = await f.StoryAsync("Pdfcaption Story");
            await f.UpdateStoryAsync(story.Id, p =>
            {
                p.Video = video;
                p.VideoCaptions = notVtt;
            });

            var html = await GetHtmlAsync(story.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            Assert.Contains("<video", main);
            Assert.Contains(video.ToString(), main);
            Assert.DoesNotContain("<track", main);
            Assert.DoesNotContain(notVtt.ToString(), main);
            AssertPageWideRules(html);
        });
    }

    // Matrix: Video + captions.
    [Fact]
    public async Task Video_With_Captions_Renders_Self_Hosted_Player_With_Captions_Track()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            var video = await f.UploadAsync(bytes: FakeMp4, extension: ".mp4");
            var captions = await f.UploadAsync(bytes: TinyVtt, extension: ".vtt");
            var story = await f.StoryAsync("Video Story");
            await f.UpdateStoryAsync(story.Id, p =>
            {
                p.Video = video;
                p.VideoCaptions = captions;
            });

            var html = await GetHtmlAsync(story.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");

            var videoTag = Regex.Match(main, "<video\\b[^>]*>").Value;
            Assert.Matches(@"\scontrols[\s>=]", videoTag);
            Assert.Equal("none", AttrOf(videoTag, "preload"));
            Assert.DoesNotContain("autoplay", main, StringComparison.OrdinalIgnoreCase);

            var source = Regex.Match(main, "<source\\b[^>]*>").Value;
            Assert.Contains(video.ToString(), AttrOf(source, "src")!);
            Assert.Equal("video/mp4", AttrOf(source, "type"));

            var track = Regex.Match(main, "<track\\b[^>]*>").Value;
            Assert.Equal("captions", AttrOf(track, "kind"));
            Assert.Equal("vi", AttrOf(track, "srclang"));
            Assert.Matches(@"\sdefault[\s>=]", track);
            Assert.Contains(captions.ToString(), AttrOf(track, "src")!);
            AssertPageWideRules(html);
        });
    }

    /// <summary>
    /// Static files serve .vtt as text/vtt (ASP.NET Core's default map has no
    /// .vtt entry). Uses a file in the web root directly: in the test host
    /// Piranha's local storage writes uploads under the test bin folder, not
    /// the served web root.
    /// </summary>
    [Fact]
    public async Task Vtt_Files_Are_Served_As_Text_Vtt()
    {
        var env = _factory.Services.GetRequiredService<IWebHostEnvironment>();
        var dir = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(dir);
        var name = $"story-test-{Guid.NewGuid():N}.vtt";
        var path = Path.Combine(dir, name);
        await File.WriteAllBytesAsync(path, TinyVtt);

        try
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync($"/uploads/{name}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/vtt", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(TinyVtt, await response.Content.ReadAsByteArrayAsync());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Trust_Block_Shows_On_Empty_Archive_And_Hub_Without_Tiles()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            var story = await f.StoryAsync("Empty Cat Story", quote: "Câu nói", attribution: "Nghệ nhân");
            var (hub, archive) = await f.EmptyCatalogAsync();

            foreach (var permalink in new[] { hub.Permalink, archive.Permalink })
            {
                var html = await GetHtmlAsync(permalink, HostnameOf(siteB));
                var main = Section(html, "<main", "</main>");
                Assert.DoesNotContain("data-sb-tile-grid", main);
                Assert.DoesNotContain("data-sb-product-grid", main);
                var trust = Section(main, "<aside class=\"sb-trust\"", "</aside>");
                var link = Regex.Match(trust, "<a class=\"sb-trust__link\"[^>]*>").Value;
                Assert.Equal(story.Permalink, AttrOf(link, "href"));
                AssertPageWideRules(html);
            }
        });
    }

    [Fact]
    public async Task Story_Nested_Under_A_Parent_Page_Is_Found()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            var parent = await f.StandardPageAsync("Story Parent");
            var story = await f.StoryAsync("Nested Story", parentId: parent.Id, sortOrder: 0);
            Assert.StartsWith($"/{parent.Slug}/", story.Permalink);
            var (hub, archive, post) = await f.CatalogAsync();

            Assert.Equal(story.Permalink, (await LookupAsync(siteB.Id))!.Permalink);
            foreach (var permalink in new[] { hub.Permalink, archive.Permalink, post.Permalink })
            {
                var html = await GetHtmlAsync(permalink, HostnameOf(siteB));
                var trust = Section(html, "<aside class=\"sb-trust\"", "</aside>");
                Assert.Equal(story.Permalink, AttrOf(Regex.Match(trust, "<a class=\"sb-trust__link\"[^>]*>").Value, "href"));
                var footer = Section(html, "<footer class=\"sb-footer\"", "</footer>");
                Assert.Contains($"<p class=\"sb-footer__story\"><a href=\"{story.Permalink}\">", footer);
                AssertPageWideRules(html);
            }
        });
    }

    // Matrix: Site A.
    [Fact]
    public async Task Site_A_Never_Shows_Trust_Block_Or_Story_Link()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
            var storyB = await f.StoryAsync("Site B Story", quote: "Câu nói B", attribution: "Nghệ nhân");
            // Hidden from Site A's own nav, so any link to it would come from the story lookup.
            var storyA = await f.StoryAsync("Site A Story", site: siteA, isHidden: true, quote: "Câu nói A", attribution: "Nghệ nhân");
            var (hubA, archiveA, postA) = await f.CatalogAsync(siteA);

            foreach (var permalink in new[] { "/", hubA.Permalink, archiveA.Permalink, postA.Permalink })
            {
                var html = await GetHtmlAsync(permalink, HostnameOf(siteA));
                Assert.DoesNotContain("sb-trust", html);
                Assert.DoesNotContain("sb-footer__story", html);
                Assert.DoesNotContain($"href=\"{storyA.Permalink}\"", html);
                Assert.DoesNotContain($"href=\"{storyB.Permalink}\"", html);
                Assert.DoesNotContain("Câu nói", Decode(html));
                AssertPageWideRules(html);
            }

            Assert.Null(await LookupAsync(siteA.Id));
            Assert.NotNull(await LookupAsync(siteB.Id));
            Assert.Equal(storyB.Permalink, (await LookupAsync(siteB.Id))!.Permalink);
        });
    }

    [Fact]
    public async Task Lookup_Picks_First_Published_Story_In_Sitemap_Order()
    {
        await WithStoryAsync(async (api, siteB, f) =>
        {
            var second = await f.StoryAsync("Second Story", sortOrder: 3);
            var first = await f.StoryAsync("First Story", sortOrder: 2);

            Assert.Equal(first.Permalink, (await LookupAsync(siteB.Id))!.Permalink);
            Assert.NotEqual(second.Permalink, first.Permalink);
        });
    }

    // Seed: fresh site, re-run, deleted.
    [Fact]
    public async Task Seed_Creates_One_Draft_Story_Page_And_Is_Idempotent_Per_Slug()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = $"story-seed-test-{suffix}",
            Title = $"Story Seed Test {suffix}",
            Hostnames = $"story-seed-test-{suffix}.local",
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

            await CraftsmanStorySeed.EnsureSeededAsync(api, site.Id);

            var sitemap = await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false);
            Assert.Equal(2, sitemap.Count);
            Assert.Equal(existing.Id, sitemap[0].Id);
            var info = await api.Pages.GetBySlugAsync<PageInfo>("cau-chuyen-nghe-nhan", site.Id);
            Assert.NotNull(info);
            Assert.Equal(info!.Id, sitemap[1].Id);
            var seeded = (await api.Pages.GetByIdAsync<CraftsmanStoryPage>(info.Id))!;
            Assert.Equal(nameof(CraftsmanStoryPage), seeded.TypeId);
            Assert.Equal("Câu chuyện nghệ nhân", seeded.Title);
            Assert.Null(seeded.Published);
            Assert.Equal("Nghệ nhân Phạm Trí Trong", seeded.QuoteAttributionText);
            Assert.Null(seeded.QuoteText);
            Assert.Empty(seeded.PhotoItems);
            Assert.Empty(seeded.Blocks);
            Assert.False(seeded.HasVideo);
            Assert.True(seeded.IsHidden);

            // Re-run: nothing created, editor's rename kept.
            var renamed = $"Renamed {suffix}";
            seeded.Title = renamed;
            await api.Pages.SaveAsync(seeded);
            await CraftsmanStorySeed.EnsureSeededAsync(api, site.Id);
            await CraftsmanStorySeed.EnsureSeededAsync(api, site.Id);
            sitemap = await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false);
            Assert.Equal(2, sitemap.Count);
            Assert.Equal(renamed, (await api.Pages.GetByIdAsync<PageInfo>(info.Id))!.Title);

            // Deleted: re-seeded as a fresh draft.
            await api.Pages.DeleteAsync(info.Id);
            await CraftsmanStorySeed.EnsureSeededAsync(api, site.Id);
            var recreated = await api.Pages.GetBySlugAsync<PageInfo>("cau-chuyen-nghe-nhan", site.Id);
            Assert.NotNull(recreated);
            Assert.NotEqual(info.Id, recreated!.Id);
            Assert.Null(recreated.Published);
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
    public async Task Startup_Seeded_A_Story_Page_On_Site_B()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);

        var info = await api.Pages.GetBySlugAsync<PageInfo>("cau-chuyen-nghe-nhan", siteB.Id);
        Assert.NotNull(info);
        Assert.Equal(nameof(CraftsmanStoryPage), info!.TypeId);
    }

    // --- helpers ---

    /// <summary>
    /// AC: every <c>&lt;img&gt;</c> has a non-empty alt; no autoplay, no
    /// iframe, no "Đặt mua".
    /// </summary>
    private static void AssertPageWideRules(string html)
    {
        var decoded = Decode(html);
        Assert.DoesNotContain("Đặt mua", decoded);
        Assert.DoesNotContain("autoplay", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
        foreach (var img in Imgs(html))
        {
            Assert.False(string.IsNullOrWhiteSpace(AttrOf(img, "alt")), $"Image without alt: {img}");
        }
    }

    private static List<string> Imgs(string html) =>
        Regex.Matches(html, "<img\\b[^>]*>").Select(m => m.Value).ToList();

    private static string? AttrOf(string tag, string name)
    {
        var m = Regex.Match(tag, $"\\s{Regex.Escape(name)}=\"([^\"]*)\"");
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>The lookup, in a fresh scope (the service memoizes per scope).</summary>
    private async Task<CraftsmanStoryLink?> LookupAsync(Guid siteId)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<CraftsmanStory>().GetPublishedAsync(siteId);
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

    private async Task WithStoryAsync(Func<IApi, Site, Fixture, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var fixture = new Fixture(_factory.Services, api, siteB);

        try
        {
            await fixture.HidePublishedStoriesAsync();
            await body(api, siteB, fixture);
        }
        finally
        {
            await fixture.CleanupAsync();
        }
    }

    /// <summary>
    /// Builds and tears down one test's pages, media and settings changes.
    /// Page/media edits run in their own DI scopes (see the Story 2.4 notes
    /// in <see cref="ProductDetailPageTests"/>).
    /// </summary>
    private sealed class Fixture
    {
        private readonly IServiceProvider _services;
        private readonly IApi _api;
        private readonly Site _siteB;
        private readonly Dictionary<Guid, CatalogBuilder> _catalogs = new();
        private readonly List<Guid> _pages = new();
        private readonly List<Guid> _media = new();
        private readonly List<(Guid Id, DateTime Published)> _hidden = new();
        private (string? Address, string? MapsUrl)? _workshop;

        public Fixture(IServiceProvider services, IApi api, Site siteB)
        {
            _services = services;
            _api = api;
            _siteB = siteB;
        }

        public string Suffix { get; } = Guid.NewGuid().ToString("N")[..8];

        public async Task<CraftsmanStoryPage> StoryAsync(string title, Site? site = null, bool published = true,
            DateTime? publishedAt = null, bool isHidden = false, string? quote = null, string? attribution = null,
            string? eyebrow = null, string? block = null, int sortOrder = NonStartPageSortOrder, Guid? parentId = null)
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            var page = await api.Pages.CreateAsync<CraftsmanStoryPage>();
            page.SiteId = (site ?? _siteB).Id;
            page.ParentId = parentId;
            page.SortOrder = sortOrder;
            page.Title = $"{title} {Suffix}";
            var parentSlug = parentId.HasValue ? (await api.Pages.GetByIdAsync<PageInfo>(parentId.Value))!.Slug + "/" : "";
            page.Slug = $"{parentSlug}story-test-{Guid.NewGuid():N}";
            page.IsHidden = isHidden;
            page.Published = published ? publishedAt ?? DateTime.Now.AddMinutes(-5) : null;
            page.Quote = quote;
            page.QuoteAttribution = attribution;
            page.Eyebrow = eyebrow;
            if (block != null)
            {
                page.Blocks.Add(new HtmlBlock { Body = block });
            }
            await api.Pages.SaveAsync(page);
            _pages.Add(page.Id);
            return (await api.Pages.GetByIdAsync<CraftsmanStoryPage>(page.Id))!;
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
            page.Slug = $"story-parent-{Guid.NewGuid():N}";
            page.Published = DateTime.Now.AddMinutes(-5);
            await api.Pages.SaveAsync(page);
            _pages.Add(page.Id);
            return (await api.Pages.GetByIdAsync<StandardPage>(page.Id))!;
        }

        /// <summary>A hub with one archive and no products (so no tiles either).</summary>
        public async Task<(ProductHubPage Hub, ProductArchive Archive)> EmptyCatalogAsync()
        {
            if (!_catalogs.TryGetValue(_siteB.Id, out var catalog))
            {
                catalog = new CatalogBuilder(_api, _siteB);
                _catalogs[_siteB.Id] = catalog;
            }

            var hub = await catalog.HubAsync("Empty Story Hub");
            var archive = await catalog.ArchiveAsync(hub, "Empty Story Cat", 0);
            return (hub, archive);
        }

        public async Task UpdateStoryAsync(Guid id, Action<CraftsmanStoryPage> change)
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            var page = (await api.Pages.GetByIdAsync<CraftsmanStoryPage>(id))!;
            change(page);
            await api.Pages.SaveAsync(page);
        }

        /// <summary>A hub with one archive holding one published product.</summary>
        public async Task<(ProductHubPage Hub, ProductArchive Archive, ProductPost Post)> CatalogAsync(
            Site? site = null, bool withPostBlock = false)
        {
            site ??= _siteB;
            if (!_catalogs.TryGetValue(site.Id, out var catalog))
            {
                catalog = new CatalogBuilder(_api, site);
                _catalogs[site.Id] = catalog;
            }

            var hub = await catalog.HubAsync("Story Hub");
            var archive = await catalog.ArchiveAsync(hub, "Story Cat", 0);
            var post = await catalog.PostAsync(archive, "Story Product");
            if (withPostBlock)
            {
                using var scope = _services.CreateScope();
                var api = scope.ServiceProvider.GetRequiredService<IApi>();
                var p = (await api.Posts.GetByIdAsync<ProductPost>(post.Id))!;
                p.Blocks.Add(new HtmlBlock { Body = "<p>Pdp block marker</p>" });
                await api.Posts.SaveAsync(p);
            }
            return (hub, archive, post);
        }

        public async Task<Guid> UploadAsync(string? altText = null, byte[]? bytes = null, string extension = ".png")
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            using var stream = new MemoryStream(bytes ?? TinyPng);
            var content = new StreamMediaContent
            {
                Filename = $"story-test-{Guid.NewGuid():N}{extension}",
                Data = stream
            };
            await api.Media.SaveAsync(content);
            var id = content.Id!.Value;
            _media.Add(id);

            if (altText != null)
            {
                var media = (await api.Media.GetByIdAsync(id))!;
                media.AltText = altText;
                await api.Media.SaveAsync(media);
            }
            return id;
        }

        /// <summary>Sets Site B's Address/Maps URL; the originals are restored on cleanup.</summary>
        public async Task SetWorkshopAsync(string? address, string? mapsUrl)
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(_siteB.Id)
                ?? await api.Sites.CreateContentAsync<SiteSettings>();
            _workshop ??= (settings.Address?.Value, settings.MapsUrl?.Value);
            settings.Address = address;
            settings.MapsUrl = mapsUrl;
            await api.Sites.SaveContentAsync(_siteB.Id, settings);
        }

        /// <summary>
        /// Unpublishes every story page already published on Site B so the
        /// test controls the lookup; restored on cleanup.
        /// </summary>
        public async Task HidePublishedStoriesAsync()
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            foreach (var item in Flatten(await api.Sites.GetSitemapAsync(_siteB.Id, onlyPublished: false)))
            {
                if (!item.Published.HasValue)
                {
                    continue;
                }
                var info = await api.Pages.GetByIdAsync<PageInfo>(item.Id);
                if (info?.TypeId != nameof(CraftsmanStoryPage))
                {
                    continue;
                }
                var page = (await api.Pages.GetByIdAsync<CraftsmanStoryPage>(item.Id))!;
                _hidden.Add((page.Id, page.Published!.Value));
                page.Published = null;
                await api.Pages.SaveAsync(page);
            }
        }

        public async Task CleanupAsync()
        {
            using var scope = _services.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            try
            {
                foreach (var catalog in _catalogs.Values)
                {
                    await catalog.CleanupAsync();
                }
                // Children (created after their parent) first.
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
                if (_workshop is { } w)
                {
                    var settings = (await api.Sites.GetContentByIdAsync<SiteSettings>(_siteB.Id))!;
                    settings.Address = w.Address;
                    settings.MapsUrl = w.MapsUrl;
                    await api.Sites.SaveContentAsync(_siteB.Id, settings);
                }
                foreach (var (id, published) in _hidden)
                {
                    var page = (await api.Pages.GetByIdAsync<CraftsmanStoryPage>(id))!;
                    page.Published = published;
                    await api.Pages.SaveAsync(page);
                }
            }
        }

        private static IEnumerable<SitemapItem> Flatten(IEnumerable<SitemapItem> items) =>
            items.SelectMany(i => new[] { i }.Concat(Flatten(i.Items)));
    }
}
