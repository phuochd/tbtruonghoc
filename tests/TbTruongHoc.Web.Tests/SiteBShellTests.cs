using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 2.1: Site B (trongdoitam.net) gets its own Mộc Trầm layout - nav
/// from the CMS sitemap, footer and a sticky contact bar fed by that site's
/// <see cref="SiteSettings"/> - while Site A keeps rendering the shared
/// <c>_Layout</c>. Real HTTP render against the real MariaDB-backed app,
/// following <see cref="SiteSettingsTests"/>'s snapshot/restore pattern.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class SiteBShellTests
{
    private const int NonStartPageSortOrder = 1;

    private readonly PiranhaWebApplicationFactory _factory;

    public SiteBShellTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task All_Contact_Values_Set_Render_Three_Segments_And_Hotline()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var phone = $"090 333 0000 {suffix[..4]}";
        var zalo = $"https://zalo.me/sb-{suffix}";
        var maps = $"https://maps.google.com/?q=sb-{suffix}";

        await WithSiteBPageAsync(s =>
        {
            s.Phone = phone;
            s.ZaloUrl = zalo;
            s.MapsUrl = maps;
        }, html =>
        {
            var bar = Section(html, "<nav class=\"sb-contact-bar\"", "</nav>");
            Assert.Contains("aria-label=\"Liên hệ nhanh\"", bar);

            Assert.Contains("sb-contact-bar__seg--call\" href=\"tel:", bar);
            Assert.Contains("Gọi ngay", bar);

            Assert.Contains($"sb-contact-bar__seg--zalo\" href=\"{zalo}\" target=\"_blank\" rel=\"noopener noreferrer\"", bar);
            Assert.Contains("Chat Zalo", bar);

            Assert.Contains($"sb-contact-bar__seg--maps\" href=\"{maps}\" target=\"_blank\" rel=\"noopener noreferrer\"", bar);
            Assert.Contains("Bản đồ", bar);

            // Nav hotline shows the Phone, as a tel: link.
            var nav = Section(html, "<header class=\"sb-nav\"", "</header>");
            Assert.Contains("class=\"sb-nav__hotline\" href=\"tel:", nav);
            Assert.Contains(phone, nav);

            Assert.Contains("sb-has-bar", BodyTag(html));
        });
    }

    [Fact]
    public async Task Only_Phone_Set_Renders_Call_Segment_Only()
    {
        var suffix = Guid.NewGuid().ToString("N");

        await WithSiteBPageAsync(s =>
        {
            s.Phone = $"090 444 0000 {suffix[..4]}";
            s.ZaloUrl = string.Empty;
            s.MapsUrl = string.Empty;
        }, html =>
        {
            Assert.Contains("sb-contact-bar__seg--call", html);
            Assert.DoesNotContain("sb-contact-bar__seg--zalo", html);
            Assert.DoesNotContain("sb-contact-bar__seg--maps", html);
            Assert.DoesNotContain("zalo.me", html);
            Assert.Contains("sb-has-bar", BodyTag(html));
        });
    }

    [Fact]
    public async Task Only_Zalo_Set_Renders_Bar_Without_Hotline_Or_Footer_Phone()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var zalo = $"https://zalo.me/zalo-only-{suffix}";

        await WithSiteBPageAsync(s =>
        {
            s.Phone = string.Empty;
            s.ZaloUrl = zalo;
            s.MapsUrl = string.Empty;
            s.Address = string.Empty;
        }, html =>
        {
            var bar = Section(html, "<nav class=\"sb-contact-bar\"", "</nav>");
            Assert.Contains($"sb-contact-bar__seg--zalo\" href=\"{zalo}\"", bar);
            Assert.DoesNotContain("sb-contact-bar__seg--call", bar);
            Assert.DoesNotContain("sb-contact-bar__seg--maps", bar);
            Assert.Contains("sb-has-bar", BodyTag(html));

            Assert.DoesNotContain("sb-nav__hotline", html);
            var footer = Section(html, "<footer class=\"sb-footer\"", "</footer>");
            Assert.DoesNotContain("href=\"tel:", footer);
            Assert.DoesNotContain("sb-footer__phone", footer);
        });
    }

    [Fact]
    public async Task Only_Address_Set_Shows_Footer_Address_Without_Bar()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var address = $"Address Only Street {suffix}";

        await WithSiteBPageAsync(s =>
        {
            s.Phone = string.Empty;
            s.ZaloUrl = string.Empty;
            s.MapsUrl = string.Empty;
            s.Address = address;
        }, html =>
        {
            var footer = Section(html, "<footer class=\"sb-footer\"", "</footer>");
            Assert.Contains($"<p class=\"sb-footer__address\">{address}</p>", footer);

            Assert.DoesNotContain("<nav class=\"sb-contact-bar\"", html);
            Assert.DoesNotContain("sb-has-bar", BodyTag(html));
        });
    }

    [Fact]
    public async Task Nothing_Set_Omits_Bar_Hotline_And_Bottom_Padding()
    {
        await WithSiteBPageAsync(s =>
        {
            s.Phone = string.Empty;
            s.ZaloUrl = string.Empty;
            s.MapsUrl = string.Empty;
            s.Address = string.Empty;
        }, html =>
        {
            Assert.DoesNotContain("<nav class=\"sb-contact-bar\"", html);
            Assert.DoesNotContain("sb-nav__hotline", html);
            Assert.DoesNotContain("href=\"tel:", html);
            Assert.DoesNotContain("sb-has-bar", BodyTag(html));
            // The rest of the shell still renders.
            Assert.Contains("<header class=\"sb-nav\"", html);
            Assert.Contains("<footer class=\"sb-footer\"", html);
        });
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("/relative/path")]
    [InlineData("not a url")]
    [InlineData("")]
    [InlineData(null)]
    public void Unsafe_Or_Unset_Url_Is_Treated_As_Unset(string? url)
    {
        // Unsafe schemes can't be saved through either save path (Story 1.9
        // hook), so this row is covered at the shared helper both the bar
        // and _ContactBlock render through.
        Assert.Null(ContactLinks.SafeUrl(url));
    }

    [Fact]
    public void Safe_Url_Is_Returned_Unchanged()
    {
        const string zalo = "https://zalo.me/0901234567";
        Assert.Equal(zalo, ContactLinks.SafeUrl(zalo));
    }

    [Theory]
    [InlineData("+84 (090) 123-4567", "+840901234567")]
    [InlineData("090 123 4567", "0901234567")]
    [InlineData("  +84 90 123 4567", "+84901234567")]
    [InlineData("1234567", "1234567")]
    [InlineData("+1234567", "+1234567")]
    public void TelHref_Keeps_Digits_And_Leading_Plus(string phone, string expected)
    {
        Assert.Equal(expected, ContactLinks.TelHref(phone));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("N/A")]
    [InlineData("123456")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TelHref_Below_Seven_Digits_Is_Unset(string? phone)
    {
        Assert.Null(ContactLinks.TelHref(phone));
    }

    [Theory]
    [InlineData(SiteSeed.TrongDoiTamInternalId, SiteLayout.TrongDoiTam)]
    [InlineData(SiteSeed.TbTruongHocInternalId, SiteLayout.Default)]
    [InlineData(null, SiteLayout.Default)]
    [InlineData("some-future-site", SiteLayout.Default)]
    public void Layout_Is_Selected_Per_Site(string? internalId, string expected)
    {
        Assert.Equal(expected, SiteLayout.ForInternalId(internalId));
    }

    [Fact]
    public async Task Sitemap_Parent_With_Children_Renders_Disclosure_And_Submenu_Skipping_Hidden()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        StandardPage? parent = null;
        StandardPage? childOne = null;
        StandardPage? childTwo = null;
        StandardPage? hiddenChild = null;

        try
        {
            parent = await CreatePublishedPageAsync(api, siteB, $"Nav Parent {suffix}", $"nav-parent-{suffix}");
            childOne = await CreatePublishedPageAsync(api, siteB, $"Nav Child One {suffix}", $"nav-child-one-{suffix}", parentId: parent.Id, sortOrder: 0);
            childTwo = await CreatePublishedPageAsync(api, siteB, $"Nav Child Two {suffix}", $"nav-child-two-{suffix}", parentId: parent.Id, sortOrder: 1);
            hiddenChild = await CreatePublishedPageAsync(api, siteB, $"Nav Hidden Child {suffix}", $"nav-hidden-child-{suffix}", parentId: parent.Id, sortOrder: 2, isHidden: true);

            var html = await GetHtmlAsync(parent.Permalink, HostnameOf(siteB));
            var nav = Section(html, "<header class=\"sb-nav\"", "</header>");

            var submenuId = "sb-sub-" + parent.Id.ToString("N");
            Assert.Contains($"Nav Parent {suffix}</a>", nav);
            Assert.Contains($"aria-expanded=\"false\" aria-controls=\"{submenuId}\" data-sb-submenu-toggle", nav);

            var submenu = Section(nav, $"<ul class=\"sb-nav__submenu\" id=\"{submenuId}\"", "</ul>");
            Assert.Contains($"Nav Child One {suffix}", submenu);
            Assert.Contains($"Nav Child Two {suffix}", submenu);
            Assert.DoesNotContain($"Nav Hidden Child {suffix}", nav);

            // The current page is marked for assistive tech.
            Assert.Contains("aria-current=\"page\">Nav Parent " + suffix, nav);

            // On a child page: parent item is active, child sublink is current.
            var childHtml = await GetHtmlAsync(childOne.Permalink, HostnameOf(siteB));
            var childNav = Section(childHtml, "<header class=\"sb-nav\"", "</header>");
            var parentLink = $"href=\"{parent.Permalink}\"";
            var parentLinkAt = childNav.IndexOf(parentLink, StringComparison.Ordinal);
            Assert.True(parentLinkAt >= 0, "Parent link missing on child page.");
            var parentLiAt = childNav.LastIndexOf("<li class=\"sb-nav__item", parentLinkAt, StringComparison.Ordinal);
            Assert.True(parentLiAt >= 0);
            var parentLiTag = childNav[parentLiAt..childNav.IndexOf('>', parentLiAt)];
            Assert.Contains("is-active", parentLiTag);
            Assert.DoesNotContain("aria-current=\"page\">Nav Parent " + suffix, childNav);
            Assert.Contains("class=\"sb-nav__sublink\" href=\"" + childOne.Permalink + "\" aria-current=\"page\">Nav Child One " + suffix, childNav);
            Assert.DoesNotContain("aria-current=\"page\">Nav Child Two " + suffix, childNav);
        }
        finally
        {
            foreach (var page in new[] { hiddenChild, childTwo, childOne, parent })
            {
                if (page != null)
                {
                    await api.Pages.DeleteAsync(page.Id);
                }
            }
        }
    }

    [Fact]
    public async Task Site_B_Uses_Moc_Tram_Shell_Only()
    {
        var suffix = Guid.NewGuid().ToString("N");

        await WithSiteBPageAsync(s =>
        {
            s.Phone = $"090 555 0000 {suffix[..4]}";
            s.ZaloUrl = $"https://zalo.me/sb-{suffix}";
            s.MapsUrl = $"https://maps.google.com/?q=sb-{suffix}";
            s.Address = $"12 Đọi Tam {suffix}";
        }, (html, pageTitle) =>
        {
            Assert.Contains($"<title>{pageTitle}</title>", html);
            Assert.Contains("sb-has-nav", BodyTag(html));

            Assert.Contains("<html lang=\"vi\">", html);
            Assert.Contains("family=Be+Vietnam+Pro", html);
            Assert.Contains("display=swap", html);
            Assert.Contains("/assets/css/site-b.css", html);
            Assert.Contains("/assets/js/site-b-nav.js", html);

            Assert.DoesNotContain("Lato", html);
            Assert.DoesNotContain("Raleway", html);
            Assert.DoesNotContain("style.min.css", html);
            Assert.DoesNotContain("contact-block", html);
            Assert.DoesNotContain("navbar", html);

            // No account/login UI in the site chrome.
            var chrome = Section(html, "<header class=\"sb-nav\"", "</header>")
                + Section(html, "<footer class=\"sb-footer\"", "</footer>");
            foreach (var banned in new[] { "login", "account", "đăng nhập", "tài khoản" })
            {
                Assert.DoesNotContain(banned, chrome, StringComparison.OrdinalIgnoreCase);
            }

            // Wordmark with the accented second word.
            Assert.Contains("Trống <span class=\"sb-nav__brand-accent\">Đọi Tam</span>", html);

            // Footer keeps the per-site address and phone.
            var footer = Section(html, "<footer class=\"sb-footer\"", "</footer>");
            Assert.Contains($"12 Đọi Tam {suffix}", System.Net.WebUtility.HtmlDecode(footer));
            Assert.Contains("href=\"tel:", footer);

            // DOM (= keyboard) order: skip link -> nav -> main -> footer -> sticky bar.
            var skip = html.IndexOf("class=\"sb-skip-link\" href=\"#main-content\"", StringComparison.Ordinal);
            var nav = html.IndexOf("<header class=\"sb-nav\"", StringComparison.Ordinal);
            var main = html.IndexOf("id=\"main-content\"", StringComparison.Ordinal);
            var mainTag = html.IndexOf("<main", StringComparison.Ordinal);
            var footerAt = html.IndexOf("<footer class=\"sb-footer\"", StringComparison.Ordinal);
            var bar = html.IndexOf("<nav class=\"sb-contact-bar\"", StringComparison.Ordinal);
            Assert.True(skip >= 0 && skip < nav, "Skip link must come first.");
            Assert.True(nav < main && main < mainTag, "Nav must precede main content.");
            Assert.True(mainTag < footerAt, "Main must precede footer.");
            Assert.True(footerAt < bar, "Sticky bar must come after the footer.");
        });
    }

    [Fact]
    public async Task Site_A_Keeps_The_Shared_Layout()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, siteA.Id);

        var suffix = Guid.NewGuid().ToString("N");
        StandardPage? page = null;

        try
        {
            await SaveSettingsAsync(api, siteA.Id, s =>
            {
                s.Phone = $"090 666 0000 {suffix[..4]}";
                s.ZaloUrl = $"https://zalo.me/sa-{suffix}";
                s.MapsUrl = $"https://maps.google.com/?q=sa-{suffix}";
                s.Address = $"Site A Street {suffix}";
            });

            page = await CreatePublishedPageAsync(api, siteA, $"Shell A {suffix}", $"shell-a-{suffix}");
            var html = await GetHtmlAsync(page.Permalink, HostnameOf(siteA));

            Assert.Contains("<html lang=\"en\">", html);
            Assert.Contains("style.min.css", html);
            Assert.Contains("family=Lato", html);
            Assert.Contains("navbar navbar-expand-lg", html);
            Assert.Contains("class=\"contact-block\"", html);
            Assert.Contains("contact-block__phone", html);
            Assert.Contains("contact-block__zalo", html);
            Assert.Contains("contact-block__maps", html);

            Assert.DoesNotContain("site-b.css", html);
            Assert.DoesNotContain("site-b-nav.js", html);
            Assert.DoesNotContain("sb-contact-bar", html);
            Assert.DoesNotContain("sb-nav", html);
            Assert.DoesNotContain("Be+Vietnam+Pro", html);
        }
        finally
        {
            await RestoreAsync(api, siteA.Id, original);
            if (page != null)
            {
                await api.Pages.DeleteAsync(page.Id);
            }
        }
    }

    [Fact]
    public async Task Hide_Site_Chrome_Drops_Nav_Footer_And_Bar_But_Keeps_Tokens_Analytics_And_Consent()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var original = await SnapshotAsync(api, siteB.Id);

        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var ga4Id = $"G-HIDE{suffix}";
        var verification = $"hide-chrome-verify-{suffix}";

        try
        {
            // Everything set, so each omission below is caused by the flag,
            // not by missing data.
            await SaveSettingsAsync(api, siteB.Id, s =>
            {
                s.Phone = $"090 777 0000 {suffix[..4]}";
                s.ZaloUrl = $"https://zalo.me/hide-{suffix}";
                s.MapsUrl = $"https://maps.google.com/?q=hide-{suffix}";
                s.Address = $"Hide Chrome Street {suffix}";
                s.Ga4MeasurementId = ga4Id;
                s.SearchConsoleVerification = verification;
            });

            // Production so the GA4-gated consent banner renders at all (same
            // shared-env switch AnalyticsSearchConsoleTests uses; the
            // collection runs serially).
            var env = _factory.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            var previousEnv = env.EnvironmentName;
            env.EnvironmentName = Microsoft.Extensions.Hosting.Environments.Production;
            string html;
            try
            {
                html = await GetHtmlAsync(HideChromeProbeController.Path, HostnameOf(siteB));
            }
            finally
            {
                env.EnvironmentName = previousEnv;
            }

            // Rendered through Site B's layout...
            Assert.Contains("<html lang=\"vi\">", html);
            Assert.Contains("/assets/css/site-b.css", html);
            Assert.Contains("family=Be+Vietnam+Pro", html);
            Assert.Contains("<h1>Hide chrome probe</h1>", html);

            // ...with no site chrome.
            Assert.DoesNotContain("sb-nav", html);
            Assert.DoesNotContain("sb-footer", html);
            Assert.DoesNotContain("sb-contact-bar", html);
            Assert.DoesNotContain("site-b-nav.js", html);
            var body = BodyTag(html);
            Assert.DoesNotContain("sb-has-bar", body);
            Assert.DoesNotContain("sb-has-nav", body);

            // _Analytics and _CookieConsent stay.
            Assert.Contains($"<meta name=\"google-site-verification\" content=\"{verification}\">", html);
            Assert.Contains($"data-ga4-id=\"{ga4Id}\"", html);
            Assert.Contains("data-consent-banner", html);
        }
        finally
        {
            await RestoreAsync(api, siteB.Id, original);
        }
    }

    // --- helpers ---

    private Task WithSiteBPageAsync(Action<SiteSettings> mutate, Action<string> assert) =>
        WithSiteBPageAsync(mutate, (html, _) => assert(html));

    /// <summary>Passes the rendered html and the throwaway page's title.</summary>
    private async Task WithSiteBPageAsync(Action<SiteSettings> mutate, Action<string, string> assert)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var original = await SnapshotAsync(api, siteB.Id);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        StandardPage? page = null;

        try
        {
            await SaveSettingsAsync(api, siteB.Id, mutate);
            var title = $"Shell B {suffix}";
            page = await CreatePublishedPageAsync(api, siteB, title, $"shell-b-{suffix}");
            var html = await GetHtmlAsync(page.Permalink, HostnameOf(siteB));
            assert(html, title);
        }
        finally
        {
            await RestoreAsync(api, siteB.Id, original);
            if (page != null)
            {
                await api.Pages.DeleteAsync(page.Id);
            }
        }
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

    private static async Task<Site> GetSiteAsync(IApi api, string internalId)
    {
        var site = await api.Sites.GetByInternalIdAsync(internalId);
        Assert.NotNull(site);
        return site!;
    }

    private static async Task<StandardPage> CreatePublishedPageAsync(
        IApi api, Site site, string title, string slug,
        Guid? parentId = null, int sortOrder = NonStartPageSortOrder, bool isHidden = false)
    {
        var page = await api.Pages.CreateAsync<StandardPage>();
        page.SiteId = site.Id;
        page.ParentId = parentId;
        page.SortOrder = sortOrder;
        page.Title = title;
        page.Slug = slug;
        page.IsHidden = isHidden;
        page.Published = DateTime.Now;
        await api.Pages.SaveAsync(page);

        var saved = await api.Pages.GetByIdAsync<StandardPage>(page.Id);
        Assert.NotNull(saved);
        return saved!;
    }

    private static async Task SaveSettingsAsync(IApi api, Guid siteId, Action<SiteSettings> mutate)
    {
        var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(siteId)
            ?? await api.Sites.CreateContentAsync<SiteSettings>();
        mutate(settings);
        await api.Sites.SaveContentAsync(siteId, settings);
    }

    private static async Task<Snapshot> SnapshotAsync(IApi api, Guid siteId)
    {
        var s = await api.Sites.GetContentByIdAsync<SiteSettings>(siteId);
        return new Snapshot(
            s?.Phone?.Value,
            s?.ZaloUrl?.Value,
            s?.Address?.Value,
            s?.MapsUrl?.Value,
            s?.Ga4MeasurementId?.Value,
            s?.SearchConsoleVerification?.Value,
            s?.NotificationEmails?.Value);
    }

    private static Task RestoreAsync(IApi api, Guid siteId, Snapshot o) =>
        SaveSettingsAsync(api, siteId, s =>
        {
            s.Phone = o.Phone;
            s.ZaloUrl = o.ZaloUrl;
            s.Address = o.Address;
            s.MapsUrl = o.MapsUrl;
            s.Ga4MeasurementId = o.Ga4MeasurementId;
            s.SearchConsoleVerification = o.SearchConsoleVerification;
            s.NotificationEmails = o.NotificationEmails;
        });

    private sealed record Snapshot(
        string? Phone,
        string? ZaloUrl,
        string? Address,
        string? MapsUrl,
        string? Ga4MeasurementId,
        string? SearchConsoleVerification,
        string? NotificationEmails);

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
