#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 1.8 (AD-1): the app-local <c>Areas/Manager/Pages/PageList.cshtml</c>
/// override of Piranha.Manager's own page list. Tests never log into
/// Manager, so the tab behavior itself is verified manually (see the
/// spec's Verification section); these tests prove the override is wired
/// correctly - it, and only it, serves <c>~/manager/pages</c>, and the
/// Piranha auth gate still applies.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class ManagerPageTabsTests
{
    private const string PageListPath = "/Areas/Manager/Pages/PageList.cshtml";

    private readonly PiranhaWebApplicationFactory _factory;

    public ManagerPageTabsTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// A second page claiming the same route would throw
    /// AmbiguousMatchException (500) here; the override keeping Piranha's
    /// <c>[Authorize(Policy = "PiranhaPages")]</c> model yields a login
    /// redirect instead.
    /// </summary>
    [Fact]
    public async Task Anonymous_Request_To_Manager_Pages_Redirects_To_Login()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/manager/pages");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("login", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Manager_Pages_Route_Has_Exactly_One_Endpoint_Served_By_The_App_Override()
    {
        // Force host startup so endpoint data sources are populated.
        _factory.CreateClient();

        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => string.Equals(e.RoutePattern.RawText?.Trim('/'), "manager/pages", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var endpoint = Assert.Single(endpoints);

        var descriptor = endpoint.Metadata.GetMetadata<PageActionDescriptor>();
        Assert.NotNull(descriptor);
        Assert.Equal(PageListPath, descriptor!.RelativePath);


        // Build-time: both the app and the Piranha.Manager RCL ship a
        // compiled view for this path; MVC takes the first one by path
        // (application parts list the app's own assembly first), so the
        // first match must be the app's.
        var partManager = _factory.Services.GetRequiredService<ApplicationPartManager>();
        var viewsFeature = new ViewsFeature();
        partManager.PopulateFeature(viewsFeature);
        var winningView = viewsFeature.ViewDescriptors
            .First(v => string.Equals(v.RelativePath, PageListPath, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(typeof(Program).Assembly, winningView.Type!.Assembly);

        // Runtime: the page actually loaded for the endpoint. Program.cs
        // enables Piranha's AddRazorRuntimeCompilation, so in Development the
        // page is recompiled from the app's .cshtml on disk into a dynamic
        // assembly; either way it must never be Piranha.Manager's own page.
        var compiled = descriptor as CompiledPageActionDescriptor;
        if (compiled?.PageTypeInfo == null)
        {
            var loader = _factory.Services.GetRequiredService<PageLoader>();
            compiled = await loader.LoadAsync(descriptor, endpoint.Metadata);
        }

        Assert.NotEqual(typeof(Piranha.Manager.Module).Assembly, compiled.PageTypeInfo.Assembly);
        Assert.Equal(typeof(Piranha.Manager.Models.PageListViewModel), compiled.ModelTypeInfo?.AsType());

        // The override must keep Piranha's page-level auth gate. The
        // pre-compilation endpoint's Metadata does not carry the PageModel's
        // [Authorize] yet; the endpoint the request is actually dispatched to
        // (PageLoaderMatcherPolicy swaps it in) carries the compiled
        // descriptor's EndpointMetadata, so check that.
        Assert.Contains(compiled.EndpointMetadata.OfType<AuthorizeAttribute>(), a => a.Policy == "PiranhaPages");
    }

    [Fact]
    public void PageList_Override_Keeps_Script_Order_And_VShow_Wiring()
    {
        var cshtml = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "TbTruongHoc.Web", "Areas", "Manager", "Pages", "PageList.cshtml"));

        // manager-page-tabs.js must load before piranha.pagelist compiles the template.
        var tabsScript = cshtml.IndexOf("manager-page-tabs.js", StringComparison.Ordinal);
        var pagelistScript = cshtml.IndexOf("piranha.pagelist.min.js", StringComparison.Ordinal);
        Assert.True(tabsScript >= 0, "manager-page-tabs.js script tag is missing from PageList.cshtml.");
        Assert.True(pagelistScript >= 0, "piranha.pagelist.min.js script tag is missing from PageList.cshtml.");
        Assert.True(tabsScript < pagelistScript, "manager-page-tabs.js must be included before piranha.pagelist.min.js.");

        // The per-site panel <li> must hide inactive sites with v-show, never
        // v-if: bind() attaches drag-drop only to containers already in the DOM.
        // (The tab strip's <li v-for="site in sites"> is the other match; the
        // panel is the one with role="tabpanel".)
        var panel = Regex.Matches(cshtml, @"<li\s+v-for=""site in sites""[^>]*>", RegexOptions.Singleline)
            .Select(m => m.Value)
            .SingleOrDefault(tag => tag.Contains(@"role=""tabpanel""", StringComparison.Ordinal));
        Assert.True(panel != null, "Site panel <li v-for=\"site in sites\" role=\"tabpanel\"> not found in PageList.cshtml.");
        Assert.Contains(@"v-show=""managerPageTabs.isActive(site.id, sites)""", panel);
        Assert.DoesNotContain("v-if", panel);
    }

    [Fact]
    public void Piranha_Manager_Is_Still_12_0_0()
    {
        var version = typeof(Piranha.Manager.Module).Assembly.GetName().Version!;

        Assert.True(version.Major == 12 && version.Minor == 0 && version.Build == 0,
            $"Piranha.Manager is now {version}. Areas/Manager/Pages/PageList.cshtml is a copy of Piranha 12.0.0's page list: " +
            "re-sync the override against the new Piranha PageList page (markup, scripts, partials) before updating this check.");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TbTruongHoc.sln")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new InvalidOperationException(
                $"Could not locate repo root (TbTruongHoc.sln) starting from {AppContext.BaseDirectory}");
        }

        return dir.FullName;
    }
}
