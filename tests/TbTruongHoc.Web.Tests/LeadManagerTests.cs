#nullable enable

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Controllers;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Covers Story 1.5's (AD-3) I/O &amp; Edge-Case Matrix for the Manager
/// "Danh sách khách để lại thông tin" screen.
///
/// Per this story's Code Map, the authenticated-success rows resolve
/// <see cref="LeadApiController"/>'s own dependencies from
/// <c>factory.Services.CreateScope()</c> and construct/call the controller
/// directly, bypassing <c>[Authorize]</c> - which needs a real Manager login
/// the test has no way to perform (admin credentials are randomly generated
/// at first boot; Boundaries &amp; Constraints explicitly forbid attempting
/// an HTTP "log in as Manager admin" step in tests). This exercises exactly
/// the same query/mapping logic the real, authenticated HTTP request would
/// run - only the ASP.NET Core authentication/authorization pipeline itself
/// is skipped.
///
/// Separately, the reject-when-unauthenticated rows go through the real
/// HTTP pipeline with no Manager auth cookie, so the actual
/// <c>[Authorize(Policy = Permission.Admin)]</c> gate on both the Razor Page
/// and the API controller is what is under test there. ASP.NET Core's
/// default cookie authentication challenge
/// (<c>CookieAuthenticationEvents.OnRedirectToLogin</c>/<c>OnRedirectToAccessDenied</c>)
/// only returns 401/403 instead of a 302 redirect when the request is
/// recognized as an AJAX/XHR call - which it detects solely via an
/// <c>X-Requested-With: XMLHttpRequest</c> header, not the request path -
/// so the API-rejection test sets that header explicitly to exercise that
/// branch, exactly like a real browser XHR call would.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class LeadManagerTests
{
    private readonly PiranhaWebApplicationFactory _factory;

    public LeadManagerTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_Returns_Rows_Across_Both_Sites_With_Resolved_SiteName_Ordered_By_CreatedAt_Desc()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var leadDb = scope.ServiceProvider.GetRequiredService<LeadDbContext>();
        var controller = new LeadApiController(api, leadDb);

        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);

        var suffix = Guid.NewGuid().ToString("N");
        var olderId = await SeedSubmissionAsync(leadDb, siteA.Id, "general", $"Older {suffix}", DateTimeOffset.UtcNow.AddMinutes(-10));
        var newerId = await SeedSubmissionAsync(leadDb, siteB.Id, "general", $"Newer {suffix}", DateTimeOffset.UtcNow);

        var items = await GetOkListAsync(controller, siteId: null);

        var olderItem = items.SingleOrDefault(i => i.Id == olderId);
        var newerItem = items.SingleOrDefault(i => i.Id == newerId);
        Assert.NotNull(olderItem);
        Assert.NotNull(newerItem);
        // Site names resolved server-side, never left blank/guessed.
        Assert.Equal(siteA.Title, olderItem!.SiteName);
        Assert.Equal(siteB.Title, newerItem!.SiteName);

        var itemList = items.ToList();
        var newerIndex = itemList.IndexOf(newerItem);
        var olderIndex = itemList.IndexOf(olderItem);
        Assert.True(newerIndex < olderIndex, "Expected the more recently created row to be listed before the older one (CreatedAt desc, unconditionally).");
    }

    [Fact]
    public async Task List_Filtered_By_SiteId_Returns_Only_That_Sites_Rows()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var leadDb = scope.ServiceProvider.GetRequiredService<LeadDbContext>();
        var controller = new LeadApiController(api, leadDb);

        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);

        var suffix = Guid.NewGuid().ToString("N");
        var siteAId = await SeedSubmissionAsync(leadDb, siteA.Id, "general", $"Filter A {suffix}", DateTimeOffset.UtcNow);
        var siteBId = await SeedSubmissionAsync(leadDb, siteB.Id, "general", $"Filter B {suffix}", DateTimeOffset.UtcNow);

        var items = await GetOkListAsync(controller, siteA.Id);

        Assert.Contains(items, i => i.Id == siteAId);
        Assert.DoesNotContain(items, i => i.Id == siteBId);
        Assert.All(items, i => Assert.Equal(siteA.Id, i.SiteId));
    }

    [Fact]
    public async Task Detail_Of_General_Submission_Returns_Full_Record_With_LocationFields_Null_Not_Omitted()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var leadDb = scope.ServiceProvider.GetRequiredService<LeadDbContext>();
        var controller = new LeadApiController(api, leadDb);

        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var suffix = Guid.NewGuid().ToString("N");
        var id = await SeedSubmissionAsync(leadDb, site.Id, "general", $"General Detail {suffix}", DateTimeOffset.UtcNow);

        var result = await controller.Get(id);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var detail = Assert.IsType<LeadDetailModel>(okResult.Value);

        Assert.Equal(id, detail.Id);
        Assert.Equal(site.Id, detail.SiteId);
        Assert.Equal(site.Title, detail.SiteName);
        Assert.Equal("general", detail.FormType);
        Assert.Null(detail.LocationAddress);
        Assert.Null(detail.IsOutsideServiceArea);

        // The I/O matrix requires these two fields present as explicit null,
        // not omitted from the response - serialize with the app's own
        // registered Microsoft.AspNetCore.Mvc.JsonOptions (not a fresh
        // System.Text.Json.JsonSerializerOptions default), so a future
        // JsonIgnoreCondition.WhenWritingNull tweak anywhere in the app's
        // real JSON configuration would actually fail this test instead of
        // silently breaking only the live endpoint.
        var jsonOptions = _factory.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>().Value;
        var serializerOptions = jsonOptions.JsonSerializerOptions;
        var json = System.Text.Json.JsonSerializer.Serialize(detail, serializerOptions);
        using var doc = System.Text.Json.JsonDocument.Parse(json);

        // The app's real options may apply a naming policy (e.g. ASP.NET
        // Core's camelCase default) - convert the DTO's property names the
        // same way rather than assuming PascalCase, so this checks the
        // actual wire property names.
        var namingPolicy = serializerOptions.PropertyNamingPolicy;
        var locationAddressName = namingPolicy?.ConvertName(nameof(LeadDetailModel.LocationAddress)) ?? nameof(LeadDetailModel.LocationAddress);
        var isOutsideServiceAreaName = namingPolicy?.ConvertName(nameof(LeadDetailModel.IsOutsideServiceArea)) ?? nameof(LeadDetailModel.IsOutsideServiceArea);

        Assert.True(doc.RootElement.TryGetProperty(locationAddressName, out var locationProp));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, locationProp.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty(isOutsideServiceAreaName, out var outsideProp));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, outsideProp.ValueKind);
    }

    [Fact]
    public async Task Detail_Of_Survey_Submission_Returns_LocationAddress_And_IsOutsideServiceArea()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var leadDb = scope.ServiceProvider.GetRequiredService<LeadDbContext>();
        var controller = new LeadApiController(api, leadDb);

        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var suffix = Guid.NewGuid().ToString("N");
        var id = await SeedSubmissionAsync(
            leadDb,
            site.Id,
            "survey",
            $"Survey Detail {suffix}",
            DateTimeOffset.UtcNow,
            locationAddress: $"123 Outside Town Rd, {suffix}",
            isOutsideServiceArea: true);

        var result = await controller.Get(id);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var detail = Assert.IsType<LeadDetailModel>(okResult.Value);

        Assert.Equal("survey", detail.FormType);
        Assert.Equal($"123 Outside Town Rd, {suffix}", detail.LocationAddress);
        Assert.True(detail.IsOutsideServiceArea);
    }

    [Fact]
    public async Task Detail_Of_NonExistent_Id_Returns_404_With_No_Exception()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var leadDb = scope.ServiceProvider.GetRequiredService<LeadDbContext>();
        var controller = new LeadApiController(api, leadDb);

        var result = await controller.Get(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Anonymous_Request_To_Manager_Leads_Page_Redirects_To_Login_Never_Data()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/manager/leads");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("login", location, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The spec's frozen I/O &amp; Edge-Case Matrix describes this row's
    /// expected status as "401/403 for the API". Empirically (verified both
    /// against this test and by curling the already-running dev container's
    /// equivalent, pre-existing <c>manager/api/alias/list</c> endpoint) that
    /// is not what this app actually returns: Piranha's own
    /// <c>SecurityMiddleware</c> (registered globally by this app's
    /// <c>options.UseCms()</c> call in Program.cs - see
    /// core/Piranha.AspNetCore/Http/SecurityMiddleware.cs, and
    /// core/Piranha.AspNetCore/Hosting/PiranhaStartupFilter.cs which wraps it
    /// around the *entire* pipeline) rewrites <em>any</em> 401 response
    /// anywhere in this app - Manager API included - into a 302 redirect to
    /// its configured login URL, before the response ever reaches the
    /// client. So <c>[Authorize(Policy = Permission.Admin)]</c>'s own 401
    /// challenge never survives to the wire here; every unauthenticated
    /// Manager request, page or API, surfaces as a 302. This is pre-existing
    /// platform behavior from Story 1.1's scaffold, not something this
    /// story's code controls or should change. What this story's Boundaries
    /// actually require - and what this test asserts - is that the request
    /// is rejected and lead data is never returned, regardless of the exact
    /// status code.
    /// </summary>
    [Fact]
    public async Task Anonymous_Request_To_Lead_Api_List_Never_Returns_Data()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/manager/api/lead/list");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("login", location, StringComparison.OrdinalIgnoreCase);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("FormSubmission", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Same rejection guarantee as <see cref="Anonymous_Request_To_Lead_Api_List_Never_Returns_Data"/>,
    /// but against the single-record detail endpoint with a real seeded row -
    /// the list-level test above proves no row is ever returned, but doesn't
    /// prove a specific lead's own PII (name/phone) can't leak through
    /// <c>GET manager/api/lead/{id}</c> to an anonymous caller who already
    /// knows/guesses the id.
    /// </summary>
    [Fact]
    public async Task Anonymous_Request_To_Lead_Api_Detail_Never_Leaks_Seeded_PII()
    {
        string name;
        string phone;
        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            var leadDb = scope.ServiceProvider.GetRequiredService<LeadDbContext>();
            var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);

            var suffix = Guid.NewGuid().ToString("N");
            name = $"PII Leak Guard {suffix}";
            phone = $"09{suffix.Substring(0, 8)}";
            id = await SeedSubmissionAsync(leadDb, site.Id, "general", name, DateTimeOffset.UtcNow, phone: phone);
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var request = new HttpRequestMessage(HttpMethod.Get, $"/manager/api/lead/{id}");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("login", location, StringComparison.OrdinalIgnoreCase);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(name, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(phone, body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Leads_Menu_Item_Is_Registered_Under_Content_With_Admin_Policy()
    {
        // WebApplicationFactory is lazy - the host (and hence Program.cs's
        // app.UsePiranha startup callback that registers this menu item)
        // only actually starts on first access to .Services/.CreateClient().
        // Merely holding a reference to _factory does not trigger that, so
        // this must touch .Services itself before asserting on Menu.Items -
        // otherwise this test is order-dependent on some other test in the
        // shared collection happening to run first.
        using var scope = _factory.Services.CreateScope();

        var contentGroup = Piranha.Manager.Menu.Items["Content"];
        Assert.NotNull(contentGroup);

        var leadsItem = contentGroup!.Items["Leads"];
        Assert.NotNull(leadsItem);
        Assert.Equal("~/manager/leads", leadsItem!.Route);
        Assert.Equal(Piranha.Manager.Permission.Admin, leadsItem.Policy);
    }

    private static async Task<System.Collections.Generic.IReadOnlyList<LeadListItemModel>> GetOkListAsync(LeadApiController controller, Guid? siteId)
    {
        var result = await controller.List(siteId);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<LeadListItemModel>>(okResult.Value);
    }

    private static async Task<Guid> SeedSubmissionAsync(
        LeadDbContext leadDb,
        Guid siteId,
        string formType,
        string name,
        DateTimeOffset createdAt,
        string? locationAddress = null,
        bool? isOutsideServiceArea = null,
        string phone = "0900000000")
    {
        var submission = new FormSubmission
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            FormType = formType,
            Name = name,
            Phone = phone,
            CreatedAt = createdAt,
            LocationAddress = locationAddress,
            IsOutsideServiceArea = isOutsideServiceArea
        };

        leadDb.FormSubmissions.Add(submission);
        await leadDb.SaveChangesAsync();

        return submission.Id;
    }

    private static async Task<Site> GetSiteAsync(IApi api, string internalId)
    {
        var site = await api.Sites.GetByInternalIdAsync(internalId);
        Assert.NotNull(site);
        return site!;
    }
}
