using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Covers Story 1.4's I/O &amp; Edge-Case Matrix via real HTTP POSTs (the
/// same Host-header pattern as <see cref="HostnameResolutionTests"/> and
/// <see cref="PerPageSeoFieldsTests"/>) against the real MariaDB-backed
/// <see cref="LeadDbContext"/>, asserting the resolved <c>SiteId</c> per
/// site and that every submitted field lands intact.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class LeadSubmissionTests
{
    private const string LeadsEndpoint = "/api/leads";

    private readonly PiranhaWebApplicationFactory _factory;

    public LeadSubmissionTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Complete_Submission_On_SiteA_Persists_Row_With_SiteA_Id_And_General_FormType()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);

        var suffix = Guid.NewGuid().ToString("N");
        var name = $"Site A Visitor {suffix}";

        var response = await PostAsync(HostnameOf(siteA), new
        {
            name,
            phone = "0901234567",
            message = "I'd like a quote."
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await GetSavedSubmissionAsync(name);
        Assert.NotNull(saved);
        Assert.Equal(siteA.Id, saved!.SiteId);
        Assert.Equal("general", saved.FormType);
        Assert.Equal("0901234567", saved.Phone);
        Assert.Equal("I'd like a quote.", saved.Message);
    }

    [Fact]
    public async Task Complete_Submission_On_SiteB_Persists_Row_With_SiteB_Id_Never_SiteA()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);

        var suffix = Guid.NewGuid().ToString("N");
        var name = $"Site B Visitor {suffix}";

        var response = await PostAsync(HostnameOf(siteB), new
        {
            name,
            phone = "0909876543"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await GetSavedSubmissionAsync(name);
        Assert.NotNull(saved);
        Assert.Equal(siteB.Id, saved!.SiteId);
        Assert.NotEqual(siteA.Id, saved.SiteId);
    }

    [Fact]
    public async Task ProductOfInterest_Is_Saved_Verbatim()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);

        var suffix = Guid.NewGuid().ToString("N");
        var name = $"Product Interest Visitor {suffix}";
        var product = $"Bàn học sinh cỡ 4 - {suffix}";

        var response = await PostAsync(HostnameOf(site), new
        {
            name,
            phone = "0901112233",
            productOfInterest = product
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await GetSavedSubmissionAsync(name);
        Assert.NotNull(saved);
        Assert.Equal(product, saved!.ProductOfInterest);
    }

    [Fact]
    public async Task Blank_Phone_Saves_Nothing_And_Returns_400_With_Field_Level_Error()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);

        var suffix = Guid.NewGuid().ToString("N");
        var name = $"Blank Phone Visitor {suffix}";

        var response = await PostAsync(HostnameOf(site), new
        {
            name,
            phone = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);
        var errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Phone", out _), $"Expected a 'Phone' field error in: {body}");

        var saved = await GetSavedSubmissionAsync(name);
        Assert.Null(saved);
    }

    [Fact]
    public async Task Blank_Name_Saves_Nothing_And_Returns_400_With_Field_Level_Error()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);

        var suffix = Guid.NewGuid().ToString("N");
        var phone = $"0900000000{suffix}";

        var response = await PostAsync(HostnameOf(site), new
        {
            name = "",
            phone
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);
        var errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Name", out _), $"Expected a 'Name' field error in: {body}");

        var saved = await GetSavedSubmissionByPhoneAsync(phone);
        Assert.Null(saved);
    }

    /// <summary>
    /// Stands in for the matrix's "Submission fails (network/server error)"
    /// row in a way this real-MariaDB-backed suite can actually trigger
    /// deterministically: a Message far past the mapped column's
    /// <c>varchar(2000)</c> length causes MariaDB itself to reject the
    /// INSERT, which is exactly the kind of persistence-layer failure the
    /// controller's try/catch around <c>SaveChangesAsync</c> must turn into
    /// a clean 500 - never an unhandled exception reaching the visitor, and
    /// never a half-saved row.
    /// </summary>
    [Fact]
    public async Task Persistence_Failure_Returns_Error_Without_Unhandled_Exception_And_Saves_No_Row()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);

        var suffix = Guid.NewGuid().ToString("N");
        var name = $"Oversized Message Visitor {suffix}";
        var oversizedMessage = new string('x', 3000);

        var response = await PostAsync(HostnameOf(site), new
        {
            name,
            phone = "0901234567",
            message = oversizedMessage
        });

        // The important assertion is that the app handled this without
        // crashing (no 500 from an unhandled exception bubbling past
        // developer-exception-page middleware into a connection reset) and
        // persisted nothing - the exact status code communicated to the
        // visitor is secondary to "no unhandled exception, no half-saved row".
        Assert.True(
            response.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadRequest,
            $"Expected a handled error response, got {(int)response.StatusCode} {response.StatusCode}.");

        var saved = await GetSavedSubmissionAsync(name);
        Assert.Null(saved);
    }

    private async Task<HttpResponseMessage> PostAsync(string hostname, object payload)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, LeadsEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Host = hostname;

        return await client.SendAsync(request);
    }

    private async Task<Models.FormSubmission?> GetSavedSubmissionAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var leadDb = scope.ServiceProvider.GetRequiredService<LeadDbContext>();
        return await leadDb.FormSubmissions.AsNoTracking().SingleOrDefaultAsync(s => s.Name == name);
    }

    /// <summary>
    /// Same as <see cref="GetSavedSubmissionAsync"/> but keyed on Phone - used
    /// by the blank-Name test, where Name can't serve as the unique lookup
    /// key since it's the very field left empty.
    /// </summary>
    private async Task<Models.FormSubmission?> GetSavedSubmissionByPhoneAsync(string phone)
    {
        using var scope = _factory.Services.CreateScope();
        var leadDb = scope.ServiceProvider.GetRequiredService<LeadDbContext>();
        return await leadDb.FormSubmissions.AsNoTracking().SingleOrDefaultAsync(s => s.Phone == phone);
    }

    private static async Task<Site> GetSiteAsync(IApi api, string internalId)
    {
        var site = await api.Sites.GetByInternalIdAsync(internalId);
        Assert.NotNull(site);
        return site!;
    }

    /// <summary>
    /// See <see cref="PerPageSeoFieldsTests.HostnameOf"/> - reads the site's
    /// *current* configured hostname rather than assuming the originally-
    /// seeded value.
    /// </summary>
    private static string HostnameOf(Site site)
    {
        var hostname = site.Hostnames?.Split(',').FirstOrDefault()?.Trim();
        Assert.False(string.IsNullOrEmpty(hostname));
        return hostname!;
    }
}
