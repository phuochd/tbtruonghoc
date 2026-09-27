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
/// Story 2.4: the product-detail-page (<c>Views/Cms/ProductPost.cshtml</c>),
/// one test per I/O &amp; Edge-Case Matrix row plus the page-wide acceptance
/// checks. Real HTTP render on Site B against the MariaDB-backed app; each
/// test builds its own throwaway hub/archive/post and uploads its own tiny
/// PNGs, and deletes all of it afterwards.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class ProductDetailPageTests
{
    // 1x1 transparent PNG.
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    // 220x90 solid PNG: the shared ImageGalleryBlock / PostBlock templates crop to
    // 1100x450 / 540x200, which ImageSharp rejects for a 1x1 source (height 0).
    private static readonly byte[] WidePng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAANwAAABaCAIAAABc0a8TAAAAwElEQVR42u3SQQ0AAAjEsPODIyxhmuCCR5MqWJbpglciAaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMaUKmBJMiSnBlJgSTIkpwZRgSkwJpsSUYEpMCabElGBKMCWmBFNiSjAlpgRTgikxJZgSU4IpMSWYElOCKcGUmBJMiSnBlJgSTAmmxJRgSkwJpsSUYEpMCaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMSWYEkyJKcGUmBJMiSnBlHBTLjqcMk/EWR93AAAAAElFTkSuQmCC");

    private readonly PiranhaWebApplicationFactory _factory;

    public ProductDetailPageTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Bare_Post_Renders_Eyebrow_Title_Contact_Price_And_Prefilled_Form_Only()
    {
        await WithPdpAsync(async (api, siteB, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Hub Bare");
            var archive = await c.ArchiveAsync(hub, "Pdp Bare Parent", 0);
            var post = await c.PostAsync(archive, "Bare");

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteB));
            var decoded = Decode(html);
            var main = Section(html, "<main", "</main>");
            var mainDecoded = Decode(main);

            Assert.Contains($"<title>{post.Title}</title>", decoded);
            Assert.Contains($"<p class=\"sb-pdp__eyebrow\">{archive.Title}</p>", mainDecoded);
            Assert.Contains($"<h1 class=\"sb-pdp__title\">{post.Title}</h1>", mainDecoded);
            Assert.Contains("<p class=\"sb-product__price sb-product__price--contact\">Liên hệ báo giá</p>", mainDecoded);
            Assert.Contains("data-quote-request-form", main);
            Assert.Contains($"name=\"productOfInterest\" value=\"{post.Title}\"", mainDecoded);
            Assert.Contains("lead-form.js", html);

            Assert.DoesNotContain("<img", main);
            Assert.Contains("class=\"sb-pdp__layout\"", main);
            Assert.DoesNotContain("sb-pdp__layout--gallery", main);
            Assert.DoesNotContain("sb-pdp__gallery", main);
            Assert.DoesNotContain("data-sb-gallery", main);
            Assert.DoesNotContain("sb-gallery.js", html);
            Assert.DoesNotContain("sb-pdp__sku", main);
            Assert.DoesNotContain("Mã:", mainDecoded);
            Assert.DoesNotContain("sb-spec", main);
            Assert.DoesNotContain("sb-pdp__excerpt", main);
            AssertPageWideRules(html);
        });
    }

    [Fact]
    public async Task One_Photo_Renders_Single_Framed_Image_Without_Controls()
    {
        await WithPdpAsync(async (api, siteB, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Hub One");
            var archive = await c.ArchiveAsync(hub, "Pdp One Parent", 0);
            var post = await c.PostAsync(archive, "One");
            var photo = await media.UploadAsync();
            await UpdatePostAsync(post.Id, p => p.PrimaryImage = photo);

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            var gallery = Section(main, "<div class=\"sb-pdp__gallery\"", "</div>");

            Assert.Contains("sb-pdp__layout--gallery", main);
            var imgs = Imgs(main);
            Assert.Single(imgs);
            Assert.Contains(imgs[0], gallery);
            Assert.Equal($"{post.Title} – ảnh 1", Decode(AttrOf(imgs[0], "alt")!));
            Assert.DoesNotContain("data-sb-gallery", main);
            Assert.DoesNotContain("sb-pdp__thumb", main);
            Assert.DoesNotContain("aria-current", main);
            Assert.DoesNotContain("sb-gallery.js", html);
            AssertPageWideRules(html);
        });
    }

    [Fact]
    public async Task Many_Photos_Render_Main_Image_And_Thumbnail_Links_Skipping_Empty_Items()
    {
        await WithPdpAsync(async (api, siteB, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Hub Many");
            var archive = await c.ArchiveAsync(hub, "Pdp Many Parent", 0);
            var post = await c.PostAsync(archive, "Many");
            var primary = await media.UploadAsync();
            var second = await media.UploadAsync(altText: "Mặt trống nhìn nghiêng");
            var third = await media.UploadAsync();
            await UpdatePostAsync(post.Id, p =>
            {
                p.PrimaryImage = primary;
                p.Photos.Add(new ImageField { Id = second });
                p.Photos.Add(new ImageField());
                p.Photos.Add(new ImageField { Id = primary }); // duplicate of the primary image
                p.Photos.Add(new ImageField { Id = third });
                p.Photos.Add(new ImageField { Id = second }); // duplicate of an earlier photo
            });

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            var gallery = Section(main, "<div class=\"sb-pdp__gallery\" data-sb-gallery", "</ul>");

            var thumbs = Regex.Matches(gallery, "<a class=\"sb-pdp__thumb\"[^>]*>").Select(m => m.Value).ToList();
            Assert.Equal(3, thumbs.Count);
            for (var i = 0; i < thumbs.Count; i++)
            {
                Assert.Equal($"Xem ảnh {i + 1}", Decode(AttrOf(thumbs[i], "aria-label")!));
            }
            Assert.Equal("true", AttrOf(thumbs[0], "aria-current"));
            Assert.Null(AttrOf(thumbs[1], "aria-current"));
            Assert.Null(AttrOf(thumbs[2], "aria-current"));

            // Thumbnails are in order: primary, second, third (the empty item
            // and the duplicates of primary/second skipped).
            Assert.Contains(primary.ToString(), AttrOf(thumbs[0], "href")!);
            Assert.Contains(second.ToString(), AttrOf(thumbs[1], "href")!);
            Assert.Contains(third.ToString(), AttrOf(thumbs[2], "href")!);

            var mainImg = Regex.Match(gallery, "<img class=\"sb-pdp__image\"[^>]*>").Value;
            Assert.Matches(@"\sdata-sb-gallery-main[\s>=]", mainImg);
            Assert.Equal(AttrOf(thumbs[0], "href"), AttrOf(mainImg, "src"));
            Assert.Equal($"{post.Title} – ảnh 1", Decode(AttrOf(mainImg, "alt")!));

            var imgs = Imgs(main);
            Assert.Equal(4, imgs.Count); // main + 3 thumbnails
            var thumbAlts = imgs.Skip(1).Select(i => Decode(AttrOf(i, "alt")!)).ToList();
            var expectedAlts = new[] { $"{post.Title} – ảnh 1", "Mặt trống nhìn nghiêng", $"{post.Title} – ảnh 3" };
            Assert.Equal(expectedAlts, thumbAlts);

            // sb-gallery.js hooks on each thumbnail link.
            Assert.All(thumbs, t => Assert.Matches(@"\sdata-sb-gallery-thumb[\s>=]", t));
            Assert.Equal(expectedAlts, thumbs.Select(t => Decode(AttrOf(t, "data-alt")!)).ToArray());
            Assert.All(thumbs, t =>
            {
                Assert.False(string.IsNullOrWhiteSpace(AttrOf(t, "data-width")));
                Assert.False(string.IsNullOrWhiteSpace(AttrOf(t, "data-height")));
            });

            Assert.Contains("sb-pdp__layout--gallery", main);
            Assert.Contains("sb-gallery.js", html);
            Assert.DoesNotContain("<button", gallery);
            AssertPageWideRules(html);
        });
    }

    [Fact]
    public async Task Full_Data_Renders_Sku_Spec_Rows_And_Price_Encoded_In_Order()
    {
        await WithPdpAsync(async (api, siteB, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Hub Full");
            var archive = await c.ArchiveAsync(hub, "Pdp Full Parent", 0);
            var post = await c.PostAsync(archive, "Full", price: "từ 5 triệu", excerpt: "Full excerpt");
            var photo = await media.UploadAsync();
            await UpdatePostAsync(post.Id, p =>
            {
                p.PrimaryImage = photo;
                p.Sku = "TC-L2-160 <b>x</b>";
                p.Specs.Add(new ProductSpecRow { Label = "Đường kính", Value = "160 cm" });
                p.Specs.Add(new ProductSpecRow { Label = "Chất liệu <b>", Value = "Gỗ mít <script>x</script>" });
                p.Blocks.Add(new HtmlBlock { Body = "<p>Block body marker</p>" });
            });

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            var mainDecoded = Decode(main);

            Assert.Contains("<p class=\"sb-pdp__sku\">Mã: TC-L2-160 <b>x</b></p>", mainDecoded);
            Assert.Contains("Mã: TC-L2-160 &lt;b&gt;x&lt;/b&gt;</p>", main);
            Assert.Contains("<p class=\"sb-pdp__excerpt\">Full excerpt</p>", mainDecoded);
            Assert.Contains("<p class=\"sb-product__price\">từ 5 triệu</p>", mainDecoded);
            Assert.DoesNotContain("Liên hệ báo giá", mainDecoded);

            var spec = Section(main, "<dl class=\"sb-spec\">", "</dl>");
            Assert.Equal(2, Regex.Matches(spec, "class=\"sb-spec__row\"").Count);
            Assert.Contains("<dt class=\"sb-spec__label\">Đường kính</dt>", Decode(spec));
            Assert.Contains("<dd class=\"sb-spec__value\">160 cm</dd>", Decode(spec));
            // Values are HTML-encoded: the raw markup never contains the tags,
            // the decoded text does.
            Assert.Contains("&lt;b&gt;", spec);
            Assert.Contains("&lt;script&gt;x&lt;/script&gt;", spec);
            Assert.DoesNotContain("<b>", spec);
            Assert.DoesNotContain("<script>", spec);
            Assert.Contains("<dd class=\"sb-spec__value\">Gỗ mít <script>x</script></dd>", Decode(spec));

            // Order: gallery, eyebrow, title, SKU, excerpt, price, specs, blocks, form.
            var markers = new[]
            {
                "sb-pdp__gallery", "sb-pdp__eyebrow", "sb-pdp__title", "sb-pdp__sku", "sb-pdp__excerpt",
                "class=\"sb-product__price\"", "class=\"sb-spec\"", "Block body marker", "data-quote-request-form"
            };
            var positions = markers.Select(m => main.IndexOf(m, StringComparison.Ordinal)).ToList();
            Assert.All(positions, p => Assert.True(p >= 0));
            Assert.Equal(positions.OrderBy(p => p), positions);

            AssertPageWideRules(html);
        });
    }

    [Fact]
    public async Task Half_Empty_Spec_Row_Is_Omitted()
    {
        await WithPdpAsync(async (api, siteB, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Hub Half");
            var archive = await c.ArchiveAsync(hub, "Pdp Half Parent", 0);
            var post = await c.PostAsync(archive, "Half");
            await UpdatePostAsync(post.Id, p =>
            {
                p.Specs.Add(new ProductSpecRow { Label = "Đường kính", Value = "  " });
                p.Specs.Add(new ProductSpecRow { Label = "", Value = "Orphan value" });
                p.Specs.Add(new ProductSpecRow { Label = "Chiều cao", Value = "90 cm" });
                p.Sku = "   ";
            });

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteB));
            var main = Decode(Section(html, "<main", "</main>"));
            var spec = Section(main, "<dl class=\"sb-spec\">", "</dl>");

            Assert.Single(Regex.Matches(spec, "class=\"sb-spec__row\""));
            Assert.Contains("Chiều cao", spec);
            Assert.DoesNotContain("Đường kính", main);
            Assert.DoesNotContain("Orphan value", main);
            Assert.DoesNotContain("sb-pdp__sku", main);
            AssertPageWideRules(html);
        });
    }

    [Fact]
    public async Task Spec_Rows_All_Blank_Render_No_Spec_List()
    {
        await WithPdpAsync(async (api, siteB, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Hub Blank");
            var archive = await c.ArchiveAsync(hub, "Pdp Blank Parent", 0);
            var post = await c.PostAsync(archive, "Blank");
            await UpdatePostAsync(post.Id, p => p.Specs.Add(new ProductSpecRow { Label = "Nhãn", Value = "" }));

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteB));
            Assert.DoesNotContain("sb-spec", Section(html, "<main", "</main>"));
            AssertPageWideRules(html);
        });
    }

    [Fact]
    public async Task Image_Blocks_On_Pdp_Get_Alt_From_Product_Title_And_Linked_Post_Title()
    {
        await WithPdpAsync(async (api, siteB, c, media) =>
        {
            var hub = await c.HubAsync("Pdp Hub Blocks");
            var archive = await c.ArchiveAsync(hub, "Pdp Blocks Parent", 0);
            var linked = await c.PostAsync(archive, "Linked");
            var post = await c.PostAsync(archive, "Blocks");
            var noAlt = await media.UploadAsync(png: WidePng);
            var withAlt = await media.UploadAsync(altText: "Tang trống cận cảnh", png: WidePng);
            var linkedPhoto = await media.UploadAsync(png: WidePng);
            await UpdatePostAsync(linked.Id, p => p.PrimaryImage = linkedPhoto);
            var pagePhoto = await media.UploadAsync(png: WidePng);
            await UpdateHubAsync(hub.Id, p => p.PrimaryImage = pagePhoto);
            await UpdatePostAsync(post.Id, p =>
            {
                p.Blocks.Add(new ImageBlock { Body = noAlt });
                var gallery = new ImageGalleryBlock();
                gallery.Items.Add(new ImageBlock { Body = withAlt });
                gallery.Items.Add(new ImageBlock { Body = noAlt });
                p.Blocks.Add(gallery);
                p.Blocks.Add(new PostBlock { Body = new PostField { Id = linked.Id } });
                p.Blocks.Add(new PageBlock { Body = new PageField { Id = hub.Id } });
            });

            var html = await GetHtmlAsync(post.Permalink, HostnameOf(siteB));
            var main = Section(html, "<main", "</main>");
            var imgs = Imgs(main);

            // Blocks render in order: ImageBlock, 2 gallery items, PostBlock card, PageBlock card.
            Assert.Equal(5, imgs.Count);
            Assert.Contains(noAlt.ToString(), AttrOf(imgs[0], "src")!);
            Assert.Equal(post.Title, Decode(AttrOf(imgs[0], "alt")!));
            Assert.Equal("Tang trống cận cảnh", Decode(AttrOf(imgs[1], "alt")!));
            Assert.Equal(post.Title, Decode(AttrOf(imgs[2], "alt")!));
            Assert.Contains(linkedPhoto.ToString(), AttrOf(imgs[3], "src")!);
            Assert.Equal(linked.Title, Decode(AttrOf(imgs[3], "alt")!));
            Assert.Contains(pagePhoto.ToString(), AttrOf(imgs[4], "src")!);
            Assert.Equal(hub.Title, Decode(AttrOf(imgs[4], "alt")!));
            AssertPageWideRules(html);
        });
    }

    // --- helpers ---

    /// <summary>
    /// AC: no "Đặt mua", no <c>&lt;form action</c>, no cart link, and every
    /// <c>&lt;img&gt;</c> on the page carries a non-empty alt.
    /// </summary>
    private static void AssertPageWideRules(string html)
    {
        var decoded = Decode(html);
        Assert.DoesNotContain("Đặt mua", decoded);
        Assert.DoesNotMatch(new Regex(@"<form\b[^>]*\baction=", RegexOptions.IgnoreCase), html);
        Assert.DoesNotMatch(new Regex("href=\"[^\"]*(cart|gio-hang|checkout)", RegexOptions.IgnoreCase), html);
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

    /// <summary>
    /// Edits a post in its own DI scope. Saving a post with blocks through
    /// the test's long-lived scope leaves stale tracked block rows in that
    /// DbContext, which Piranha's post delete then flushes (0 rows affected,
    /// DbUpdateConcurrencyException) during cleanup.
    /// </summary>
    private async Task UpdatePostAsync(Guid id, Action<ProductPost> change)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var post = (await api.Posts.GetByIdAsync<ProductPost>(id))!;
        change(post);
        await api.Posts.SaveAsync(post);
    }

    /// <summary>Edits a hub page in its own DI scope (see <see cref="UpdatePostAsync"/>).</summary>
    private async Task UpdateHubAsync(Guid id, Action<ProductHubPage> change)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var page = (await api.Pages.GetByIdAsync<ProductHubPage>(id))!;
        change(page);
        await api.Pages.SaveAsync(page);
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

        public async Task<Guid> UploadAsync(string? altText = null, byte[]? png = null)
        {
            using var stream = new MemoryStream(png ?? TinyPng);
            var content = new StreamMediaContent
            {
                Filename = $"pdp-test-{Guid.NewGuid():N}.png",
                Data = stream
            };
            await _api.Media.SaveAsync(content);
            var id = content.Id!.Value;
            _ids.Add(id);

            if (altText != null)
            {
                var media = (await _api.Media.GetByIdAsync(id))!;
                media.AltText = altText;
                await _api.Media.SaveAsync(media);
            }
            return id;
        }

        /// <summary>
        /// Deletes in a fresh scope: the page render may have added resized
        /// versions (e.g. ImageBlock's 1110px) through another DbContext, and
        /// this scope's context still tracks the uploaded Media without them,
        /// which makes Piranha's delete fail with a concurrency exception.
        /// </summary>
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

    private async Task WithPdpAsync(Func<IApi, Site, CatalogBuilder, MediaBuilder, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var catalog = new CatalogBuilder(api, siteB);
        var media = new MediaBuilder(api, _factory.Services);

        try
        {
            await body(api, siteB, catalog, media);
        }
        finally
        {
            // Posts reference the media, so they go first - but the media is
            // deleted even if catalog cleanup throws.
            try
            {
                await catalog.CleanupAsync();
            }
            finally
            {
                await media.CleanupAsync();
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
