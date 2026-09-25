using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Covers Story 1.6 (per-site GA4/Search Console, FR-4): confirms the
/// shared <see cref="SiteSettings"/> SiteType's new GA4 Measurement ID and
/// Search Console Verification fields flow correctly to a real
/// HTTP-rendered page on each site - each site's own values, never the
/// other site's - and that Piranha's built-in sitemap.xml middleware
/// already scopes to the resolved current site, satisfying FR-4's "own
/// scoped sitemap.xml" requirement with no new sitemap code. Mirrors
/// <see cref="SiteSettingsTests"/>'s pattern against the real
/// MariaDB-backed <see cref="IApi"/> and the real HTTP pipeline.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class AnalyticsSearchConsoleTests
{
    private const int NonStartPageSortOrder = 1;

    private readonly PiranhaWebApplicationFactory _factory;

    public AnalyticsSearchConsoleTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Each_Site_Renders_Only_Its_Own_Ga4_And_Verification_Values_In_Production()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();

        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);

        var (originalA, originalB) = (
            await SnapshotAsync(api, siteA.Id),
            await SnapshotAsync(api, siteB.Id));

        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var ga4A = $"G-{suffix}AAAA";
        var ga4B = $"G-{suffix}BBBB";
        var verificationA = $"verify-{suffix}-a";
        var verificationB = $"verify-{suffix}-b";

        StandardPage? pageA = null;
        StandardPage? pageB = null;

        try
        {
            await SaveSettingsAsync(api, siteA.Id, s =>
            {
                s.Ga4MeasurementId = ga4A;
                s.SearchConsoleVerification = verificationA;
            });
            await SaveSettingsAsync(api, siteB.Id, s =>
            {
                s.Ga4MeasurementId = ga4B;
                s.SearchConsoleVerification = verificationB;
            });

            pageA = await CreatePublishedPageAsync(api, siteA, $"Analytics Test A {suffix}", $"analytics-test-a-{suffix}");
            pageB = await CreatePublishedPageAsync(api, siteB, $"Analytics Test B {suffix}", $"analytics-test-b-{suffix}");

            var (htmlA, htmlB) = await InProductionAsync(async () => (
                await GetHtmlAsync(pageA.Permalink, HostnameOf(siteA)),
                await GetHtmlAsync(pageB.Permalink, HostnameOf(siteB))));

            // Story 1.10: in Production the server emits only the consent
            // script carrying this site's ID - never gtag.js itself.
            AssertConsentMarkup(htmlA, ga4A);
            Assert.DoesNotContain("googletagmanager.com", htmlA);
            Assert.Contains($"content=\"{verificationA}\"", htmlA);
            Assert.DoesNotContain(ga4B, htmlA);
            Assert.DoesNotContain(verificationB, htmlA);

            AssertConsentMarkup(htmlB, ga4B);
            Assert.DoesNotContain("googletagmanager.com", htmlB);
            Assert.Contains($"content=\"{verificationB}\"", htmlB);
            Assert.DoesNotContain(ga4A, htmlB);
            Assert.DoesNotContain(verificationA, htmlB);
        }
        finally
        {
            // Settings restored before the page deletes: a delete failure must
            // not leave this test's mutated values behind for the next one.
            await RestoreAsync(api, siteA.Id, originalA);
            await RestoreAsync(api, siteB.Id, originalB);
            if (pageA != null)
            {
                await api.Pages.DeleteAsync(pageA.Id);
            }
            if (pageB != null)
            {
                await api.Pages.DeleteAsync(pageB.Id);
            }
        }
    }

    [Fact]
    public async Task Ga4_Consent_And_Verification_Meta_Are_Omitted_In_Production_When_Unset()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var suffix = Guid.NewGuid().ToString("N")[..6];

        StandardPage? page = null;

        try
        {
            await SaveSettingsAsync(api, site.Id, s =>
            {
                s.Ga4MeasurementId = string.Empty;
                s.SearchConsoleVerification = string.Empty;
            });

            page = await CreatePublishedPageAsync(api, site, $"Empty Analytics Test {suffix}", $"empty-analytics-test-{suffix}");
            var html = await InProductionAsync(() => GetHtmlAsync(page.Permalink, HostnameOf(site)));

            // Story 1.10: no tracking, so no consent banner/link either.
            Assert.DoesNotContain("googletagmanager.com", html);
            Assert.DoesNotContain("analytics-consent.js", html);
            Assert.DoesNotContain("data-consent-banner", html);
            Assert.DoesNotContain("data-consent-reopen", html);
            Assert.DoesNotContain("google-site-verification", html);
        }
        finally
        {
            // Settings restored before the page delete: a delete failure must
            // not leave this test's mutated values behind for the next one.
            await RestoreAsync(api, site.Id, original);
            if (page != null)
            {
                await api.Pages.DeleteAsync(page.Id);
            }
        }
    }

    [Fact]
    public async Task Outside_Production_A_Valid_Ga4_Id_Emits_No_Ga4_Or_Consent_Markup()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();

        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);

        var (originalA, originalB) = (
            await SnapshotAsync(api, siteA.Id),
            await SnapshotAsync(api, siteB.Id));

        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var ga4A = $"G-{suffix}AAAA";
        var ga4B = $"G-{suffix}BBBB";
        var verificationA = $"verify-{suffix}-a";

        StandardPage? pageA = null;
        StandardPage? pageB = null;

        try
        {
            await SaveSettingsAsync(api, siteA.Id, s =>
            {
                s.Ga4MeasurementId = ga4A;
                s.SearchConsoleVerification = verificationA;
            });
            await SaveSettingsAsync(api, siteB.Id, s => s.Ga4MeasurementId = ga4B);

            pageA = await CreatePublishedPageAsync(api, siteA, $"Dev Analytics Test A {suffix}", $"dev-analytics-test-a-{suffix}");
            pageB = await CreatePublishedPageAsync(api, siteB, $"Dev Analytics Test B {suffix}", $"dev-analytics-test-b-{suffix}");

            foreach (var environment in new[] { Environments.Development, Environments.Staging })
            {
                var (htmlA, htmlB) = await InEnvironmentAsync(environment, async () => (
                    await GetHtmlAsync(pageA.Permalink, HostnameOf(siteA)),
                    await GetHtmlAsync(pageB.Permalink, HostnameOf(siteB))));

                foreach (var html in new[] { htmlA, htmlB })
                {
                    Assert.DoesNotContain("googletagmanager.com", html);
                    Assert.DoesNotContain("gtag", html);
                    Assert.DoesNotContain(ga4A, html);
                    Assert.DoesNotContain(ga4B, html);
                    Assert.DoesNotContain("analytics-consent.js", html);
                    Assert.DoesNotContain("data-consent-banner", html);
                    Assert.DoesNotContain("data-consent-reopen", html);
                }

                // Search Console verification is not tracking - still rendered.
                Assert.Contains($"content=\"{verificationA}\"", htmlA);
            }
        }
        finally
        {
            // Settings restored before the page deletes: a delete failure must
            // not leave this test's mutated values behind for the next one.
            await RestoreAsync(api, siteA.Id, originalA);
            await RestoreAsync(api, siteB.Id, originalB);
            if (pageA != null)
            {
                await api.Pages.DeleteAsync(pageA.Id);
            }
            if (pageB != null)
            {
                await api.Pages.DeleteAsync(pageB.Id);
            }
        }
    }

    [Fact]
    public async Task Save_Rejects_Invalid_Ga4_Measurement_Id_And_Does_Not_Persist()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        const string safeGa4Id = "G-STILLSAFE1";

        try
        {
            await SaveSettingsAsync(api, site.Id, s => s.Ga4MeasurementId = safeGa4Id);

            await Assert.ThrowsAsync<ValidationException>(() =>
                SaveSettingsAsync(api, site.Id, s => s.Ga4MeasurementId = "G-ABC'; </script><script>alert(1)"));

            var afterRejectedSave = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.Equal(safeGa4Id, afterRejectedSave!.Ga4MeasurementId?.Value);
        }
        finally
        {
            await RestoreAsync(api, site.Id, original);
        }
    }

    [Fact]
    public async Task Save_Rejects_Invalid_Search_Console_Verification_And_Does_Not_Persist()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        const string safeVerification = "still-safe-after-rejected-save";

        try
        {
            await SaveSettingsAsync(api, site.Id, s => s.SearchConsoleVerification = safeVerification);

            await Assert.ThrowsAsync<ValidationException>(() =>
                SaveSettingsAsync(api, site.Id, s => s.SearchConsoleVerification = "\"><script>alert(1)</script>"));

            var afterRejectedSave = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.Equal(safeVerification, afterRejectedSave!.SearchConsoleVerification?.Value);
        }
        finally
        {
            await RestoreAsync(api, site.Id, original);
        }
    }

    [Fact]
    public async Task Sitemap_Is_Scoped_To_Each_Sites_Own_Published_Pages()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();

        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var slugA = $"sitemap-scope-a-{suffix}";
        var slugB = $"sitemap-scope-b-{suffix}";

        StandardPage? pageA = null;
        StandardPage? pageB = null;

        try
        {
            pageA = await CreatePublishedPageAsync(api, siteA, $"Sitemap Scope A {suffix}", slugA);
            pageB = await CreatePublishedPageAsync(api, siteB, $"Sitemap Scope B {suffix}", slugB);

            var sitemapA = await GetSitemapXmlAsync(HostnameOf(siteA));
            var sitemapB = await GetSitemapXmlAsync(HostnameOf(siteB));

            Assert.Contains(slugA, sitemapA);
            Assert.DoesNotContain(slugB, sitemapA);

            Assert.Contains(slugB, sitemapB);
            Assert.DoesNotContain(slugA, sitemapB);
        }
        finally
        {
            if (pageA != null)
            {
                await api.Pages.DeleteAsync(pageA.Id);
            }
            if (pageB != null)
            {
                await api.Pages.DeleteAsync(pageB.Id);
            }
        }
    }

    /// <summary>
    /// Story 1.10: the hooks analytics-consent.js looks up by selector, and the
    /// initial hidden state it relies on (it only ever reveals), must be in
    /// the rendered HTML - the Jint tests stub the DOM and cannot catch a
    /// renamed hook or a dropped attribute.
    /// </summary>
    private static void AssertConsentMarkup(string html, string ga4Id)
    {
        Assert.Matches($"<script[^>]*src=\"/assets/js/analytics-consent\\.js\\?v=[^\"]+\"[^>]*data-ga4-id=\"{ga4Id}\"[^>]*\\bdefer\\b", html);
        Assert.Matches("<div[^>]*role=\"region\"[^>]*aria-label=\"[^\"]+\"[^>]*data-consent-banner[^>]*\\bhidden\\b[^>]*>", html);
        Assert.Matches("<button type=\"button\"[^>]*data-consent-accept[^>]*>", html);
        Assert.Matches("<button type=\"button\"[^>]*data-consent-decline[^>]*>", html);
        Assert.Matches("<button type=\"button\"[^>]*data-consent-reopen[^>]*\\bhidden\\b[^>]*>", html);
    }

    private Task<T> InProductionAsync<T>(Func<Task<T>> render) =>
        InEnvironmentAsync(Environments.Production, render);

    /// <summary>
    /// Story 1.10: only one app host can exist per test process (see
    /// <see cref="PiranhaAppCollection"/>), so a Production-env factory is
    /// impossible. The analytics partials read the environment from the
    /// shared <see cref="IWebHostEnvironment"/> singleton on every render, so
    /// it is switched for the duration of the request(s) and always restored.
    /// The collection runs serially, so no other test observes the change.
    /// </summary>
    private async Task<T> InEnvironmentAsync<T>(string environmentName, Func<Task<T>> render)
    {
        var env = _factory.Services.GetRequiredService<IWebHostEnvironment>();
        var previous = env.EnvironmentName;
        env.EnvironmentName = environmentName;
        try
        {
            return await render();
        }
        finally
        {
            env.EnvironmentName = previous;
        }
    }

    private static async Task<Site> GetSiteAsync(IApi api, string internalId)
    {
        var site = await api.Sites.GetByInternalIdAsync(internalId);
        Assert.NotNull(site);
        return site!;
    }

    private static async Task<StandardPage> CreatePublishedPageAsync(IApi api, Site site, string title, string slug)
    {
        var page = await api.Pages.CreateAsync<StandardPage>();
        page.SiteId = site.Id;
        page.SortOrder = NonStartPageSortOrder;
        page.Title = title;
        page.Slug = slug;
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

    private static async Task<(string? Ga4MeasurementId, string? SearchConsoleVerification)> SnapshotAsync(IApi api, Guid siteId)
    {
        var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(siteId);
        return (settings?.Ga4MeasurementId?.Value, settings?.SearchConsoleVerification?.Value);
    }

    private static async Task RestoreAsync(IApi api, Guid siteId, (string? Ga4MeasurementId, string? SearchConsoleVerification) original)
    {
        await SaveSettingsAsync(api, siteId, s =>
        {
            s.Ga4MeasurementId = original.Ga4MeasurementId;
            s.SearchConsoleVerification = original.SearchConsoleVerification;
        });
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

    private async Task<string> GetSitemapXmlAsync(string hostname)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/sitemap.xml");
        request.Headers.Host = hostname;

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }
}
