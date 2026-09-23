using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using TbTruongHoc.Web.Data;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Covers the spec's I/O &amp; Edge-Case Matrix rows 1-3 (hostname -> Site
/// resolution). Piranha's own request pipeline
/// (Piranha.AspNetCore.Http.RoutingMiddleware / ApplicationService.InitAsync)
/// resolves the current site for every request by calling exactly
/// <c>api.Sites.GetByHostnameAsync(host)</c> and, if that returns null,
/// falling back to <c>api.Sites.GetDefaultAsync()</c>. There is no page
/// content seeded yet (Story 1.1 explicitly does not seed pages), so a
/// content-based assertion like comparing rendered HTML/sitemap output
/// between sites is not meaningful right now - both sites render an empty
/// &lt;urlset/&gt;. Instead these tests (a) send real HTTP requests with the
/// Host header set, through the real in-process app pipeline, to prove the
/// app handles each hostname without error, and (b) call the exact same
/// Piranha API method the pipeline uses, against the same real MariaDB-backed
/// IApi instance the app resolves from DI, to assert precisely which Site
/// each hostname resolves to.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class HostnameResolutionTests
{
    private readonly PiranhaWebApplicationFactory _factory;

    public HostnameResolutionTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SiteA_Hostname_Resolves_To_TbTruongHoc_Site()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();

        var site = await api.Sites.GetByHostnameAsync("tbtruonghoc.local");

        Assert.NotNull(site);
        Assert.Equal(SiteSeed.TbTruongHocInternalId, site!.InternalId);
        Assert.True(site.IsDefault);

        await AssertRequestSucceedsAsync("tbtruonghoc.local");
    }

    [Fact]
    public async Task SiteB_Hostname_Resolves_To_TrongDoiTam_Site()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();

        var site = await api.Sites.GetByHostnameAsync("trongdoitam.local");

        Assert.NotNull(site);
        Assert.Equal(SiteSeed.TrongDoiTamInternalId, site!.InternalId);
        Assert.False(site.IsDefault);

        await AssertRequestSucceedsAsync("trongdoitam.local");
    }

    [Fact]
    public async Task Unmapped_Hostname_Falls_Back_To_Default_Site()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();

        // No Site record has this hostname configured.
        var directMatch = await api.Sites.GetByHostnameAsync("unknown.example.test");
        Assert.Null(directMatch);

        // This is exactly the fallback Piranha's own RoutingMiddleware /
        // ApplicationService perform when GetByHostnameAsync returns null.
        var fallbackSite = await api.Sites.GetDefaultAsync();

        Assert.NotNull(fallbackSite);
        Assert.Equal(SiteSeed.TbTruongHocInternalId, fallbackSite!.InternalId);
        Assert.True(fallbackSite.IsDefault);

        await AssertRequestSucceedsAsync("unknown.example.test");
    }

    /// <summary>
    /// Sends a real request through the full in-process pipeline with the
    /// given Host header and asserts it is handled without a server error -
    /// i.e. Piranha's site-resolution middleware ran successfully end to end
    /// for that hostname (mapped or unmapped) rather than throwing.
    /// </summary>
    private async Task AssertRequestSucceedsAsync(string hostHeader)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/sitemap.xml");
        request.Headers.Host = hostHeader;

        var response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotModified,
            $"Expected a successful response for Host '{hostHeader}' but got {(int)response.StatusCode} {response.StatusCode}.");
    }
}
