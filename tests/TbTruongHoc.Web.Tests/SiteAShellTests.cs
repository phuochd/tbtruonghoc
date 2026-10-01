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
using Piranha.Extend.Fields;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 6.1: Site A (tbtruonghoc.com) gets its own "Xanh Lục Bảo Rạng Rỡ"
/// shell - sticky teal nav from the CMS sitemap with the "Sản phẩm"
/// dropdown fed by <c>ProductCatalog</c>, contact chip and footer strip from
/// Site A's <see cref="SiteSettings"/>, and the <see cref="SiteAHomePage"/>
/// hero (carousel / fallback). Real HTTP render against the real
/// MariaDB-backed app, following <see cref="SiteBShellTests"/>'s
/// snapshot/restore pattern.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class SiteAShellTests
{
    private const int NonStartPageSortOrder = 1;

    private readonly PiranhaWebApplicationFactory _factory;

    public SiteAShellTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Matrix: Hub with categories.
    [Fact]
    public async Task Hub_With_Categories_Renders_Dropdown_Of_NonEmpty_Categories_And_View_All_Count()
    {
        await WithSiteACatalogAsync(async (api, siteA, c) =>
        {
            var hub = await c.HubAsync("Sản phẩm");
            var one = await c.ArchiveAsync(hub, "Cat One", 0);
            var two = await c.ArchiveAsync(hub, "Cat Two", 1);
            var empty = await c.ArchiveAsync(hub, "Cat Empty", 2);
            var three = await c.ArchiveAsync(hub, "Cat Three", 3);
            await c.PostAsync(one, "P1");
            await c.PostAsync(two, "P2");
            await c.PostAsync(three, "P3");

            var html = await GetHtmlAsync(hub.Permalink, HostnameOf(siteA));
            var nav = Section(html, "<header class=\"sa-nav\"", "</header>");

            var panelId = "sa-dd-" + hub.Id.ToString("N");
            Assert.Contains($"aria-expanded=\"false\" aria-controls=\"{panelId}\" data-sa-dropdown-toggle", nav);
            Assert.Contains(hub.Title, Decode(Section(nav, "data-sa-dropdown-toggle>", "</button>")));

            var panel = Section(nav, $"<div class=\"sa-dropdown\" id=\"{panelId}\"", "</div>");
            var list = Section(panel, "<ul class=\"sa-dropdown__list\">", "</ul>");
            Assert.Equal(3, Count(list, "class=\"sa-dropdown__link\""));
            var at1 = list.IndexOf($"href=\"{one.Permalink}\"", StringComparison.Ordinal);
            var at2 = list.IndexOf($"href=\"{two.Permalink}\"", StringComparison.Ordinal);
            var at3 = list.IndexOf($"href=\"{three.Permalink}\"", StringComparison.Ordinal);
            Assert.True(at1 >= 0 && at1 < at2 && at2 < at3, "Rows must follow sitemap order.");
            Assert.DoesNotContain(empty.Permalink, panel);
            Assert.DoesNotContain(empty.Title, Decode(panel));

            // Divider, then "Xem tất cả {N} nhóm sản phẩm →" to the hub (N = tile count, not hardcoded).
            var dividerAt = panel.IndexOf("<hr class=\"sa-dropdown__divider\">", StringComparison.Ordinal);
            var allAt = panel.IndexOf($"class=\"sa-dropdown__all\" href=\"{hub.Permalink}\"", StringComparison.Ordinal);
            Assert.True(dividerAt > 0 && allAt > dividerAt, "View-all link must follow the divider.");
            Assert.Contains("Xem tất cả 3 nhóm sản phẩm →", Decode(panel));
            // The current page (the hub) is marked on the view-all row.
            Assert.Contains("aria-current=\"page\"", panel);
        });
    }

    // Matrix: Hub without categories.
    [Fact]
    public async Task Hub_Without_Categories_Is_A_Plain_Link()
    {
        await WithSiteACatalogAsync(async (api, siteA, c) =>
        {
            var hub = await c.HubAsync("Hub No Cats");
            var empty = await c.ArchiveAsync(hub, "Only Empty", 0);
            var draftOnly = await c.ArchiveAsync(hub, "Draft Only", 1);
            await c.PostAsync(draftOnly, "Draft", published: false);

            var html = await GetHtmlAsync(hub.Permalink, HostnameOf(siteA));
            var nav = Section(html, "<header class=\"sa-nav\"", "</header>");

            Assert.Contains($"class=\"sa-nav__link\" href=\"{hub.Permalink}\" aria-current=\"page\">", nav);
            // Scoped to this hub, not the whole nav: Site A may hold other
            // hubs (e.g. the dev sample seed). The plain-link assert above
            // already rules out the dropdown branch for this hub.
            Assert.DoesNotContain("sa-dd-" + hub.Id.ToString("N"), nav);
            Assert.DoesNotContain(empty.Permalink, nav);
        });
    }

    // Matrix: All contacts set.
    [Fact]
    public async Task All_Contacts_Set_Render_Chip_And_Full_Footer_Strip()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var phone = $"090 123 0000 {suffix[..4]}";
        var zalo = $"https://zalo.me/sa-{suffix}";
        var maps = $"https://maps.google.com/?q=sa-{suffix}";
        var email = $"lienhe-{suffix[..8]}@ngocanh.vn";
        var address = $"Số 1 Đường A {suffix}";

        await WithSiteAPageAsync(s =>
        {
            s.Phone = phone;
            s.ZaloUrl = zalo;
            s.MapsUrl = maps;
            s.Email = email;
            s.Address = address;
        }, html =>
        {
            var nav = Section(html, "<header class=\"sa-nav\"", "</header>");
            var chip = Section(nav, "<div class=\"sa-chip\">", "</div>");
            var telHref = ContactLinks.TelHref(phone);
            Assert.Contains($"class=\"sa-chip__link sa-chip__link--call\" href=\"tel:{telHref}\"", chip);
            Assert.Contains("aria-label=\"Gọi ngay tới Ngọc Anh", Decode(chip));
            Assert.Contains($"class=\"sa-chip__link sa-chip__link--zalo\" href=\"{zalo}\" target=\"_blank\" rel=\"noopener noreferrer\" aria-label=\"Chat Zalo với Ngọc Anh\"", Decode(chip));
            Assert.Contains(phone, Decode(chip));

            var footer = Section(html, "<footer class=\"sa-footer\"", "</footer>");
            Assert.Contains("<p class=\"sa-footer__brand\">NGỌC ANH</p>", Decode(footer));
            var zaloAt = footer.IndexOf($"class=\"sa-footer__zalo\" href=\"{zalo}\" target=\"_blank\"", StringComparison.Ordinal);
            var phoneAt = footer.IndexOf($"class=\"sa-footer__phone\" href=\"tel:{telHref}\"", StringComparison.Ordinal);
            var emailAt = footer.IndexOf($"class=\"sa-footer__email\" href=\"mailto:{email}\">{email}</a>", StringComparison.Ordinal);
            var addressAt = footer.IndexOf($"class=\"sa-footer__maps\" href=\"{maps}\" target=\"_blank\" rel=\"noopener noreferrer\">", StringComparison.Ordinal);
            Assert.True(zaloAt > 0 && zaloAt < phoneAt && phoneAt < emailAt && emailAt < addressAt,
                "Footer order must be Zalo · phone · email, then the address.");
            Assert.Contains($">{address}</a>", Decode(footer));
        });
    }

    // Matrix: Nothing set.
    [Fact]
    public async Task Nothing_Set_Omits_Chip_And_Footer_Shows_Brand_Only()
    {
        await WithSiteAPageAsync(s =>
        {
            s.Phone = string.Empty;
            s.ZaloUrl = string.Empty;
            s.MapsUrl = string.Empty;
            s.Email = string.Empty;
            s.Address = string.Empty;
        }, html =>
        {
            Assert.DoesNotContain("sa-chip", html);
            Assert.DoesNotContain("href=\"tel:", html);
            Assert.DoesNotContain("mailto:", html);

            var footer = Section(html, "<footer class=\"sa-footer\"", "</footer>");
            Assert.Contains("sa-footer__brand", footer);
            Assert.DoesNotContain("sa-footer__contact", footer);
            Assert.DoesNotContain("<a ", footer);

            // The rest of the shell still renders.
            Assert.Contains("<header class=\"sa-nav\"", html);
        });
    }

    // Matrix: Bad email.
    [Fact]
    public async Task Invalid_Email_Is_Treated_As_Unset()
    {
        await WithSiteAPageAsync(s =>
        {
            s.Phone = string.Empty;
            s.ZaloUrl = string.Empty;
            s.MapsUrl = string.Empty;
            s.Address = string.Empty;
            s.Email = "not-an-email";
        }, html =>
        {
            Assert.DoesNotContain("mailto:", html);
            Assert.DoesNotContain("not-an-email", html);
            Assert.DoesNotContain("sa-footer__contact", html);
        });
    }

    [Theory]
    [InlineData("lienhe@ngocanh.vn", "lienhe@ngocanh.vn")]
    [InlineData("  sales@example.com.vn ", "sales@example.com.vn")]
    [InlineData("not-an-email", null)]
    [InlineData("a@b", null)]
    [InlineData("Ngọc Anh <lienhe@ngocanh.vn>", null)]
    [InlineData("a@x.vn, b@x.vn", null)]
    [InlineData("a@x.vn;b@x.vn", null)]
    [InlineData("a@x.vn?subject=hi", null)]
    [InlineData("a%40evil.vn@x.vn", null)]
    [InlineData("a@x.vn#b@evil.vn", null)]
    [InlineData("a@x.vn\tb", null)]
    [InlineData("a\u0001b@x.vn", null)]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void MailtoAddress_Accepts_Only_One_Plain_Address(string? input, string? expected)
    {
        Assert.Equal(expected, ContactLinks.MailtoAddress(input));
    }

    [Theory]
    [InlineData("/lien-he", "/lien-he")]
    [InlineData(" https://tbtruonghoc.com/bao-gia ", "https://tbtruonghoc.com/bao-gia")]
    [InlineData("#bao-gia", "#bao-gia")]
    [InlineData("//evil.example", null)]
    [InlineData("/\\evil.example", null)]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("lien-he", null)]
    [InlineData("#", null)]
    [InlineData("/\t/evil.example", null)]
    [InlineData("/\r/evil.example", null)]
    [InlineData("/\n/evil.example", null)]
    [InlineData("/lien he", null)]
    [InlineData("#a\u0000b", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Hero_Cta_Link_Must_Be_Safe(string? input, string? expected)
    {
        Assert.Equal(expected, SiteAHomePage.SafeLink(input));
    }

    // Matrix: Hero with photos.
    [Fact]
    public async Task Hero_With_Photos_Renders_Manual_Carousel_In_Desktop_Only_Picture_Sources()
    {
        await WithHomePageAsync(async (api, siteA, h) =>
        {
            var altOne = $"Sân trường lắp dù {h.Suffix}";
            var photoOne = await h.UploadAsync(altOne);
            var photoTwo = await h.UploadAsync();
            var page = await h.CreateAsync(p =>
            {
                p.HeroEyebrow = "Thiết bị trường học";
                p.HeroHeading = $"Heading {h.Suffix}";
                p.HeroSubtext = "Mô tả ngắn";
                p.HeroCtaLabel = string.Empty;
                p.HeroCtaLink = "/lien-he";
                p.HeroPhotos.Add(new ImageField { Id = photoOne });
                p.HeroPhotos.Add(new ImageField { Id = photoTwo });
            });

            var html = await GetHtmlAsync(page.Permalink, HostnameOf(siteA));
            var hero = Section(html, "<section class=\"sa-hero", "</section>");

            Assert.StartsWith("<section class=\"sa-hero sa-hero--carousel\"", hero);
            Assert.Contains("data-sa-carousel", hero);
            Assert.Equal(2, Count(hero, "data-sa-slide"));
            Assert.Equal(2, Count(hero, "<source media=\"(min-width: 768px)\""));
            Assert.Equal(2, Count(hero, "data-sa-carousel-dot="));
            Assert.Contains("data-sa-carousel-prev", hero);
            Assert.Contains("data-sa-carousel-next", hero);

            // Phones never download hero photos: the <img> fallback is inline.
            foreach (Match img in Regex.Matches(hero, "<img [^>]*>"))
            {
                Assert.Contains("src=\"data:image/gif;base64,", img.Value);
            }

            // Walkthrough "A1+": no tint over the photos, all text in one box,
            // and no photo URL outside the desktop-only <source> (phones fetch
            // nothing).
            Assert.DoesNotContain("sa-hero__overlay", hero);
            Assert.DoesNotContain("background-image", hero);
            Assert.Equal(1, Count(hero, "class=\"sa-hero__box\""));
            // Text and CTAs come before the carousel (reading and tab order).
            Assert.True(hero.IndexOf("<h1", StringComparison.Ordinal) < hero.IndexOf("data-sa-carousel", StringComparison.Ordinal));
            Assert.True(hero.IndexOf("sa-hero__ctas", StringComparison.Ordinal) < hero.IndexOf("data-sa-carousel-prev", StringComparison.Ordinal));

            // Only the first slide shows up front; the second is hidden.
            var slides = Regex.Matches(hero, "<div class=\"sa-hero__slide\"[^>]*>").Select(m => m.Value).ToList();
            Assert.DoesNotContain("hidden", slides[0]);
            Assert.Contains("hidden=\"hidden\"", slides[1]);

            // Alt: media AltText, else the heading.
            var decoded = Decode(hero);
            Assert.Contains($"alt=\"{altOne}\"", decoded);
            Assert.Contains($"alt=\"Heading {h.Suffix}\"", decoded);

            // One heading/CTA set (the fallback below 768px is the same markup).
            Assert.Equal(1, Count(hero, "<h1"));
            Assert.Contains($">Heading {h.Suffix}</h1>", decoded);
            Assert.Contains("<p class=\"sa-hero__eyebrow\">Thiết bị trường học</p>", decoded);
            Assert.Contains("<p class=\"sa-hero__subtext\">Mô tả ngắn</p>", decoded);
            Assert.Contains("<a class=\"sa-hero__cta sa-hero__cta--quote\" href=\"/lien-he\">Nhận báo giá</a>", decoded);

            // Manual only: no auto-advance hooks in the markup.
            Assert.DoesNotContain("data-interval", hero);
            Assert.DoesNotContain("data-bs-ride", hero);
        });
    }

    // Matrix: Hero no photos (+ blank heading falls back to the title, blank link drops the primary CTA).
    [Fact]
    public async Task Hero_Without_Photos_Renders_Solid_Fallback_Without_Controls()
    {
        await WithHomePageAsync(async (api, siteA, h) =>
        {
            var page = await h.CreateAsync(p =>
            {
                p.HeroHeading = "   ";
                p.HeroCtaLink = "javascript:alert(1)";
            }, settings: s =>
            {
                s.Phone = "090 999 8888";
                s.ZaloUrl = "https://zalo.me/hero-test";
            });

            var html = await GetHtmlAsync(page.Permalink, HostnameOf(siteA));
            var hero = Section(html, "<section class=\"sa-hero", "</section>");

            Assert.StartsWith("<section class=\"sa-hero sa-hero--fallback\"", hero);
            Assert.DoesNotContain("data-sa-carousel", hero);
            Assert.DoesNotContain("<img", hero);
            Assert.DoesNotContain("<picture", hero);
            Assert.Contains($">{page.Title}</h1>", Decode(hero));

            // Unsafe link: no quote button; call and Zalo still render as two
            // distinct buttons.
            Assert.DoesNotContain("sa-hero__cta--quote", hero);
            Assert.DoesNotContain("javascript:", hero);
            var ctas = Decode(Section(hero, "<div class=\"sa-hero__ctas\">", "</div>"));
            Assert.Contains("<a class=\"sa-hero__cta sa-hero__cta--call\" href=\"tel:0909998888\" aria-label=\"Gọi ngay tới Ngọc Anh – 090 999 8888\">", ctas);
            Assert.Contains("Gọi ngay", ctas);
            Assert.Contains("<a class=\"sa-hero__cta sa-hero__cta--zalo\" href=\"https://zalo.me/hero-test\" target=\"_blank\" rel=\"noopener noreferrer\" aria-label=\"Chat Zalo với Ngọc Anh\">", ctas);
        });
    }

    [Fact]
    public async Task Hero_Pairs_Primary_Before_Secondary_Never_Inverted()
    {
        await WithHomePageAsync(async (api, siteA, h) =>
        {
            var page = await h.CreateAsync(p =>
            {
                p.HeroCtaLabel = "Yêu cầu tư vấn";
                p.HeroCtaLink = "https://tbtruonghoc.com/lien-he";
            }, settings: s =>
            {
                s.Phone = "090 999 7777";
                s.ZaloUrl = string.Empty;
            });

            var html = await GetHtmlAsync(page.Permalink, HostnameOf(siteA));
            var ctas = Decode(Section(html, "<div class=\"sa-hero__ctas\">", "</div>"));
            var quoteAt = ctas.IndexOf("<a class=\"sa-hero__cta sa-hero__cta--quote\" href=\"https://tbtruonghoc.com/lien-he\">Yêu cầu tư vấn</a>", StringComparison.Ordinal);
            var callAt = ctas.IndexOf("sa-hero__cta--call", StringComparison.Ordinal);
            Assert.True(quoteAt >= 0 && callAt > quoteAt, "Quote comes first, then call.");
            // Zalo unset: its button is omitted.
            Assert.DoesNotContain("sa-hero__cta--zalo", ctas);
        });
    }

    // Matrix: Chrome hidden.
    [Fact]
    public async Task Hide_Site_Chrome_Drops_Nav_Chip_And_Footer_But_Keeps_Tokens_Analytics_And_Consent()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SiteSettingsSnapshot.TakeAsync(api, siteA.Id);

        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var ga4Id = $"G-SAHIDE{suffix}";

        try
        {
            await SaveSettingsAsync(api, siteA.Id, s =>
            {
                s.Phone = $"090 777 0000 {suffix[..4]}";
                s.ZaloUrl = $"https://zalo.me/sa-hide-{suffix}";
                s.MapsUrl = $"https://maps.google.com/?q=sa-hide-{suffix}";
                s.Address = $"Hide Chrome Street {suffix}";
                s.Email = "hide@ngocanh.vn";
                s.Ga4MeasurementId = ga4Id;
            });

            var env = _factory.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            var previousEnv = env.EnvironmentName;
            env.EnvironmentName = Microsoft.Extensions.Hosting.Environments.Production;
            string html;
            try
            {
                html = await GetHtmlAsync(HideChromeProbeController.Path, HostnameOf(siteA));
            }
            finally
            {
                env.EnvironmentName = previousEnv;
            }

            Assert.Contains("<html lang=\"vi\">", html);
            Assert.Contains("/assets/css/site-a.css", html);
            Assert.Contains("family=Mulish", html);
            Assert.Contains("<h1>Hide chrome probe</h1>", html);

            Assert.DoesNotContain("sa-nav", html);
            Assert.DoesNotContain("sa-chip", html);
            Assert.DoesNotContain("sa-footer", html);
            Assert.DoesNotContain("sa-has-nav", BodyTag(html));

            Assert.Contains($"data-ga4-id=\"{ga4Id}\"", html);
            Assert.Contains("data-consent-banner", html);
        }
        finally
        {
            await original.RestoreAsync(api, siteA.Id);
        }
    }

    // Matrix: Site B unaffected.
    [Fact]
    public async Task Site_B_Output_Carries_No_Site_A_Shell()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        StandardPage? page = null;

        try
        {
            page = await CreatePublishedPageAsync(api, siteB, $"B Probe {suffix}", $"b-probe-{suffix}");
            var html = await GetHtmlAsync(page.Permalink, HostnameOf(siteB));

            Assert.Contains("/assets/css/site-b.css", html);
            Assert.Contains("<header class=\"sb-nav\"", html);
            Assert.DoesNotContain("site-a.css", html);
            Assert.DoesNotContain("site-a.js", html);
            Assert.DoesNotContain("Mulish", html);
            Assert.DoesNotContain("class=\"sa-", html);
            Assert.DoesNotContain("data-sa-", html);
        }
        finally
        {
            if (page != null)
            {
                await api.Pages.DeleteAsync(page.Id);
            }
        }
    }

    [Fact]
    public async Task Site_A_Shell_Uses_Only_Its_Own_Tokens_Font_And_Keyboard_Order()
    {
        var suffix = Guid.NewGuid().ToString("N");

        await WithSiteAPageAsync(s =>
        {
            s.Phone = $"090 555 0000 {suffix[..4]}";
            s.ZaloUrl = $"https://zalo.me/sa-{suffix}";
            s.Email = string.Empty;
        }, (html, title) =>
        {
            Assert.Contains($"<title>{title}</title>", html);
            Assert.Contains("<html lang=\"vi\">", html);
            Assert.Contains("family=Mulish:wght@400;600;700;800&display=swap", html);
            Assert.Contains("/assets/css/site-a.css", html);
            Assert.Contains("/assets/js/site-a.js", html);
            Assert.Contains("<body class=\"site-a sa-has-nav\">", html);

            // Never the sample theme, Site B's font or any Mộc Trầm class.
            Assert.DoesNotContain("Lato", html);
            Assert.DoesNotContain("Raleway", html);
            Assert.DoesNotContain("Be+Vietnam+Pro", html);
            Assert.DoesNotContain("Be Vietnam Pro", html);
            Assert.DoesNotContain("style.min.css", html);
            Assert.DoesNotContain("site-b", html);
            Assert.DoesNotContain("class=\"sb-", html);
            Assert.DoesNotContain(" sb-", html);
            Assert.DoesNotContain("data-sb-", html);
            Assert.DoesNotContain("navbar", html);
            Assert.DoesNotContain("contact-block", html);

            // No cart / checkout / account UI in the chrome.
            var chrome = Section(html, "<header class=\"sa-nav\"", "</header>")
                + Section(html, "<footer class=\"sa-footer\"", "</footer>");
            foreach (var banned in new[] { "login", "account", "cart", "checkout", "đăng nhập", "tài khoản", "giỏ hàng", "thanh toán" })
            {
                Assert.DoesNotContain(banned, Decode(chrome), StringComparison.OrdinalIgnoreCase);
            }

            // Wordmark.
            Assert.Contains("<a class=\"sa-nav__brand\" href=\"/\">NGỌC ANH</a>", Decode(html));

            // Keyboard order: skip link -> nav (incl. chip) -> main -> footer.
            var skip = html.IndexOf("class=\"sa-skip-link\" href=\"#main-content\"", StringComparison.Ordinal);
            var nav = html.IndexOf("<header class=\"sa-nav\"", StringComparison.Ordinal);
            var chip = html.IndexOf("<div class=\"sa-chip\">", StringComparison.Ordinal);
            var navEnd = html.IndexOf("</header>", nav, StringComparison.Ordinal);
            var main = html.IndexOf("id=\"main-content\"", StringComparison.Ordinal);
            var footer = html.IndexOf("<footer class=\"sa-footer\"", StringComparison.Ordinal);
            Assert.True(skip >= 0 && skip < nav, "Skip link must come first.");
            Assert.True(nav < chip && chip < navEnd, "The chip lives inside the nav.");
            Assert.True(navEnd < main, "Nav must precede main content.");
            Assert.True(main < footer, "Main must precede footer.");

            // Hamburger precedes the sheet it controls; the sheet has a close button.
            var toggleAt = html.IndexOf("data-sa-nav-toggle", StringComparison.Ordinal);
            var menuAt = html.IndexOf("id=\"sa-nav-menu\"", StringComparison.Ordinal);
            Assert.True(toggleAt > nav && toggleAt < menuAt);
            Assert.Contains("aria-controls=\"sa-nav-menu\"", html);
            Assert.Contains("data-sa-nav-close", html);
        });
    }

    [Fact]
    public async Task Seed_Creates_Home_Only_On_An_Empty_Site_And_Rerun_Changes_Nothing()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = $"sa-home-seed-{suffix}",
            Title = $"Site A Home Seed {suffix}",
            Hostnames = $"sa-home-seed-{suffix}.local",
            IsDefault = false
        };

        try
        {
            await api.Sites.SaveAsync(site);

            await SiteAHomeSeed.EnsureSeededAsync(api, site.Id);

            var sitemap = await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false);
            var item = Assert.Single(sitemap);
            var seeded = (await api.Pages.GetByIdAsync<SiteAHomePage>(item.Id))!;
            Assert.Equal(nameof(SiteAHomePage), seeded.TypeId);
            Assert.Equal("Trang chủ", seeded.Title);
            Assert.Null(seeded.ParentId);
            Assert.False(seeded.IsHidden);
            Assert.True(seeded.Published.HasValue && seeded.Published.Value <= DateTime.Now);
            Assert.Null(seeded.HeroEyebrowText);
            Assert.Null(seeded.HeroSubtextText);
            Assert.Null(seeded.HeroCtaHref);
            Assert.Empty(seeded.HeroPhotoItems);
            Assert.Equal("Trang chủ", seeded.HeroHeadingText);

            // An editor edits it; reruns create and change nothing.
            seeded.Title = $"Renamed {suffix}";
            await api.Pages.SaveAsync(seeded);
            await SiteAHomeSeed.EnsureSeededAsync(api, site.Id);
            await SiteAHomeSeed.EnsureSeededAsync(api, site.Id);

            sitemap = await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false);
            Assert.Equal(item.Id, Assert.Single(sitemap).Id);
            Assert.Equal($"Renamed {suffix}", (await api.Pages.GetByIdAsync<PageInfo>(item.Id))!.Title);

            // Any existing page (even a non-home one) blocks the seed.
            await api.Pages.DeleteAsync(item.Id);
            var other = await api.Pages.CreateAsync<StandardPage>();
            other.SiteId = site.Id;
            other.SortOrder = 0;
            other.Title = $"Other {suffix}";
            other.Slug = $"other-{suffix}";
            other.Published = DateTime.Now.AddMinutes(-5);
            await api.Pages.SaveAsync(other);

            await SiteAHomeSeed.EnsureSeededAsync(api, site.Id);
            Assert.Equal(other.Id, Assert.Single(await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false)).Id);
        }
        finally
        {
            foreach (var i in await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false))
            {
                await api.Pages.DeleteAsync(i.Id);
            }
            if (await api.Sites.GetByIdAsync(site.Id) != null)
            {
                await api.Sites.DeleteAsync(site.Id);
            }
        }
    }

    [Fact]
    public async Task Seed_Seam_Resolves_Site_By_Internal_Id_And_Unknown_Id_Is_A_No_Op()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = $"sa-home-seam-{suffix}",
            Title = $"Site A Home Seam {suffix}",
            Hostnames = $"sa-home-seam-{suffix}.local",
            IsDefault = false
        };

        try
        {
            await api.Sites.SaveAsync(site);

            // Unknown internal id: nothing anywhere, no throw.
            await SiteAHomeSeed.EnsureSeededForInternalIdAsync(api, $"no-such-site-{suffix}");
            Assert.Empty(await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false));

            await SiteAHomeSeed.EnsureSeededForInternalIdAsync(api, site.InternalId);
            await SiteAHomeSeed.EnsureSeededForInternalIdAsync(api, site.InternalId);

            var item = Assert.Single(await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false));
            var seeded = (await api.Pages.GetByIdAsync<SiteAHomePage>(item.Id))!;
            Assert.Equal(nameof(SiteAHomePage), seeded.TypeId);
            Assert.Equal("Trang chủ", seeded.Title);
            Assert.True(seeded.Published.HasValue && seeded.Published.Value <= DateTime.Now);
        }
        finally
        {
            foreach (var i in await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false))
            {
                await api.Pages.DeleteAsync(i.Id);
            }
            if (await api.Sites.GetByIdAsync(site.Id) != null)
            {
                await api.Sites.DeleteAsync(site.Id);
            }
        }
    }

    [Fact]
    public void Startup_Seed_Targets_Site_A()
    {
        Assert.Equal(SiteSeed.TbTruongHocInternalId, SiteAHomeSeed.TargetInternalId);
    }

    [Fact]
    public async Task Dev_Sample_Seed_Builds_A_Hub_Whose_Empty_Category_Is_Hidden_And_Rerun_Changes_Nothing()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var catalog = scope.ServiceProvider.GetRequiredService<TbTruongHoc.Web.Services.ProductCatalog>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = $"sa-sample-{suffix}",
            Title = $"Site A Sample {suffix}",
            Hostnames = $"sa-sample-{suffix}.local",
            IsDefault = false
        };

        try
        {
            await api.Sites.SaveAsync(site);

            await SiteASampleSeed.EnsureSeededAsync(api, site.Id);
            await SiteASampleSeed.EnsureSeededAsync(api, site.Id);

            var hub = Assert.Single(await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false));
            Assert.True(ProductHubPage.IsHub(hub));
            Assert.Equal(SiteACatalogSeed.Categories.Count, hub.Items.Count);

            var tiles = await catalog.GetHubTilesAsync(site.Id, hub.Id);
            Assert.Equal(SiteACatalogSeed.Categories.Count - 1, tiles.Count);
            Assert.DoesNotContain(tiles, t => t.Title == SiteACatalogSeed.Categories[^1].Title);
        }
        finally
        {
            foreach (var top in await api.Sites.GetSitemapAsync(site.Id, onlyPublished: false))
            {
                foreach (var child in top.Items)
                {
                    foreach (var post in await api.Posts.GetAllAsync<PostInfo>(child.Id))
                    {
                        await api.Posts.DeleteAsync(post.Id);
                    }
                    await api.Pages.DeleteAsync(child.Id);
                }
                await api.Pages.DeleteAsync(top.Id);
            }
            if (await api.Sites.GetByIdAsync(site.Id) != null)
            {
                await api.Sites.DeleteAsync(site.Id);
            }
        }
    }

    [Fact]
    public async Task Parent_Nav_Item_Is_Active_On_A_Child_Page()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        StandardPage? parent = null;
        StandardPage? child = null;

        try
        {
            parent = await CreatePublishedPageAsync(api, siteA, $"A Parent {suffix}", $"a-parent-{suffix}");
            child = await CreatePublishedPageAsync(api, siteA, $"A Child {suffix}", $"a-child-{suffix}", parentId: parent.Id, sortOrder: 0);

            var html = await GetHtmlAsync(child.Permalink, HostnameOf(siteA));
            var nav = Section(html, "<header class=\"sa-nav\"", "</header>");

            var linkAt = nav.IndexOf($"href=\"{parent.Permalink}\"", StringComparison.Ordinal);
            Assert.True(linkAt >= 0, "Parent link missing on child page.");
            var liAt = nav.LastIndexOf("<li class=\"sa-nav__item", linkAt, StringComparison.Ordinal);
            Assert.True(liAt >= 0);
            var liTag = nav[liAt..nav.IndexOf('>', liAt)];
            Assert.Contains("is-active", liTag);
            // Active, not current: the parent link carries no aria-current.
            Assert.DoesNotContain($"href=\"{parent.Permalink}\" aria-current=\"page\"", nav);
        }
        finally
        {
            foreach (var page in new[] { child, parent })
            {
                if (page != null)
                {
                    await api.Pages.DeleteAsync(page.Id);
                }
            }
        }
    }

    [Fact]
    public async Task Maps_Only_Footer_Links_Xem_Ban_Do()
    {
        var maps = $"https://maps.google.com/?q=sa-maps-only-{Guid.NewGuid():N}";

        await WithSiteAPageAsync(s =>
        {
            s.Phone = string.Empty;
            s.ZaloUrl = string.Empty;
            s.Email = string.Empty;
            s.Address = string.Empty;
            s.MapsUrl = maps;
        }, html =>
        {
            var footer = Decode(Section(html, "<footer class=\"sa-footer\"", "</footer>"));
            Assert.Contains($"<a class=\"sa-footer__maps\" href=\"{maps}\" target=\"_blank\" rel=\"noopener noreferrer\">Xem bản đồ</a>", footer);
            Assert.DoesNotContain("sa-footer__line", footer);
        });
    }

    // --- helpers ---

    private sealed class HomeBuilder
    {
        private readonly IApi _api;
        private readonly IServiceProvider _services;
        private readonly Site _site;
        private readonly List<Guid> _pages = new();
        private readonly List<Guid> _media = new();

        public HomeBuilder(IApi api, IServiceProvider services, Site site)
        {
            _api = api;
            _services = services;
            _site = site;
        }

        public string Suffix { get; } = Guid.NewGuid().ToString("N")[..8];

        public SiteSettingsSnapshot? Original { get; private set; }

        public async Task<SiteAHomePage> CreateAsync(Action<SiteAHomePage> mutate, Action<SiteSettings>? settings = null)
        {
            if (settings != null)
            {
                Original ??= await SiteSettingsSnapshot.TakeAsync(_api, _site.Id);
                await SaveSettingsAsync(_api, _site.Id, settings);
            }

            var page = await _api.Pages.CreateAsync<SiteAHomePage>();
            page.SiteId = _site.Id;
            page.SortOrder = NonStartPageSortOrder;
            page.Title = $"Home Test {Suffix}";
            page.Slug = $"sa-home-test-{Suffix}";
            page.Published = DateTime.Now.AddMinutes(-5);
            mutate(page);
            await _api.Pages.SaveAsync(page);
            _pages.Add(page.Id);
            return (await _api.Pages.GetByIdAsync<SiteAHomePage>(page.Id))!;
        }

        public async Task<Guid> UploadAsync(string? altText = null)
        {
            using var stream = new MemoryStream(WidePng);
            var content = new StreamMediaContent
            {
                Filename = $"sa-hero-test-{Guid.NewGuid():N}.png",
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
                if (Original != null)
                {
                    await Original.RestoreAsync(_api, _site.Id);
                }
                foreach (var id in _pages)
                {
                    await _api.Pages.DeleteAsync(id);
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

    // 220x90 solid PNG (same as BlogListingTests).
    private static readonly byte[] WidePng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAANwAAABaCAIAAABc0a8TAAAAwElEQVR42u3SQQ0AAAjEsPODIyxhmuCCR5MqWJbpglciAaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMaUKmBJMiSnBlJgSTIkpwZRgSkwJpsSUYEpMCabElGBKMCWmBFNiSjAlpgRTgikxJZgSU4IpMSWYElOCKcGUmBJMiSnBlJgSTAmmxJRgSkwJpsSUYEpMCaYEU2JKMCWmBFNiSjAlmBJTgikxJZgSU4IpMSWYEkyJKcGUmBJMiSnBlHBTLjqcMk/EWR93AAAAAElFTkSuQmCC");

    private async Task WithHomePageAsync(Func<IApi, Site, HomeBuilder, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var builder = new HomeBuilder(api, _factory.Services, siteA);

        try
        {
            await body(api, siteA, builder);
        }
        finally
        {
            await builder.CleanupAsync();
        }
    }

    private async Task WithSiteACatalogAsync(Func<IApi, Site, ProductCatalogTests.CatalogBuilder, Task> body)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var builder = new ProductCatalogTests.CatalogBuilder(api, siteA);

        try
        {
            await body(api, siteA, builder);
        }
        finally
        {
            await builder.CleanupAsync();
        }
    }

    private Task WithSiteAPageAsync(Action<SiteSettings> mutate, Action<string> assert) =>
        WithSiteAPageAsync(mutate, (html, _) => assert(html));

    /// <summary>Passes the rendered html and the throwaway page's title.</summary>
    private async Task WithSiteAPageAsync(Action<SiteSettings> mutate, Action<string, string> assert)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SiteSettingsSnapshot.TakeAsync(api, siteA.Id);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        StandardPage? page = null;

        try
        {
            await SaveSettingsAsync(api, siteA.Id, mutate);
            var title = $"Shell A {suffix}";
            page = await CreatePublishedPageAsync(api, siteA, title, $"shell-a-{suffix}");
            var html = await GetHtmlAsync(page.Permalink, HostnameOf(siteA));
            assert(html, title);
        }
        finally
        {
            await original.RestoreAsync(api, siteA.Id);
            if (page != null)
            {
                await api.Pages.DeleteAsync(page.Id);
            }
        }
    }

    private sealed record SiteSettingsSnapshot(
        string? Phone, string? ZaloUrl, string? Address, string? MapsUrl, string? Email,
        string? Ga4MeasurementId, string? SearchConsoleVerification, string? NotificationEmails)
    {
        public static async Task<SiteSettingsSnapshot> TakeAsync(IApi api, Guid siteId)
        {
            var s = await api.Sites.GetContentByIdAsync<SiteSettings>(siteId);
            return new SiteSettingsSnapshot(
                s?.Phone?.Value, s?.ZaloUrl?.Value, s?.Address?.Value, s?.MapsUrl?.Value, s?.Email?.Value,
                s?.Ga4MeasurementId?.Value, s?.SearchConsoleVerification?.Value, s?.NotificationEmails?.Value);
        }

        public Task RestoreAsync(IApi api, Guid siteId) =>
            SaveSettingsAsync(api, siteId, s =>
            {
                s.Phone = Phone;
                s.ZaloUrl = ZaloUrl;
                s.Address = Address;
                s.MapsUrl = MapsUrl;
                s.Email = Email;
                s.Ga4MeasurementId = Ga4MeasurementId;
                s.SearchConsoleVerification = SearchConsoleVerification;
                s.NotificationEmails = NotificationEmails;
            });
    }

    private static async Task SaveSettingsAsync(IApi api, Guid siteId, Action<SiteSettings> mutate)
    {
        var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(siteId)
            ?? await api.Sites.CreateContentAsync<SiteSettings>();
        mutate(settings);
        await api.Sites.SaveContentAsync(siteId, settings);
    }

    private static string Section(string html, string startMarker, string endMarker)
    {
        var start = html.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected to find '{startMarker}'.");
        var end = html.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(end > start, $"Expected '{endMarker}' after '{startMarker}'.");
        return html[start..(end + endMarker.Length)];
    }

    private static string BodyTag(string html) => Section(html, "<body", ">");

    private static string Decode(string html) => WebUtility.HtmlDecode(html);

    private static int Count(string haystack, string needle) =>
        Regex.Matches(haystack, Regex.Escape(needle)).Count;

    private static async Task<Site> GetSiteAsync(IApi api, string internalId)
    {
        var site = await api.Sites.GetByInternalIdAsync(internalId);
        Assert.NotNull(site);
        return site!;
    }

    private static async Task<StandardPage> CreatePublishedPageAsync(IApi api, Site site, string title, string slug,
        Guid? parentId = null, int sortOrder = NonStartPageSortOrder)
    {
        var page = await api.Pages.CreateAsync<StandardPage>();
        page.SiteId = site.Id;
        page.ParentId = parentId;
        page.SortOrder = sortOrder;
        page.Title = title;
        page.Slug = slug;
        page.Published = DateTime.Now;
        await api.Pages.SaveAsync(page);

        var saved = await api.Pages.GetByIdAsync<StandardPage>(page.Id);
        Assert.NotNull(saved);
        return saved!;
    }

    private static string HostnameOf(Site site)
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
