using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
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
/// Covers Story 1.3 (per-site contact info, FR-2): confirms the shared
/// <see cref="SiteSettings"/> SiteType flows correctly to a real
/// HTTP-rendered page on each site - each site's own Phone/ZaloUrl/Address/
/// MapsUrl values, never the other site's and never hardcoded - against the
/// real MariaDB-backed <see cref="IApi"/> and the real HTTP pipeline,
/// mirroring <see cref="PerPageSeoFieldsTests"/>'s pattern. Also covers the
/// spec's I/O &amp; Edge-Case Matrix: tel:-link digit stripping, direct Zalo/
/// Maps links, omission of unset fields, and HTML-encoding of special
/// characters.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class SiteSettingsTests
{
    /// <summary>
    /// See <see cref="PerPageSeoFieldsTests.NonStartPageSortOrder"/> - keeps
    /// every throwaway page this suite creates off the "home page" slot.
    /// </summary>
    private const int NonStartPageSortOrder = 1;

    private readonly PiranhaWebApplicationFactory _factory;

    public SiteSettingsTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Each_Site_Renders_Only_Its_Own_Contact_Values()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();

        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);

        var suffix = Guid.NewGuid().ToString("N");

        var (originalA, originalB) = (
            await SnapshotAsync(api, siteA.Id),
            await SnapshotAsync(api, siteB.Id));

        // Fixed digit prefix (10 digits) guarantees the >= 7-digit tel:
        // threshold regardless of how many of the hex suffix's characters
        // happen to be digits (0-9 vs a-f).
        var phoneA = $"090 111 0000 {suffix[..4]}";
        var phoneB = $"090 222 0000 {suffix[..4]}";

        StandardPage? pageA = null;
        StandardPage? pageB = null;

        try
        {
            await SaveSettingsAsync(api, siteA.Id, s =>
            {
                s.Phone = phoneA;
                s.ZaloUrl = $"https://zalo.me/siteA-{suffix}";
                s.Address = $"123 Site A Street {suffix}";
                s.MapsUrl = $"https://maps.google.com/?q=siteA-{suffix}";
            });
            await SaveSettingsAsync(api, siteB.Id, s =>
            {
                s.Phone = phoneB;
                s.ZaloUrl = $"https://zalo.me/siteB-{suffix}";
                s.Address = $"456 Site B Street {suffix}";
                s.MapsUrl = $"https://maps.google.com/?q=siteB-{suffix}";
            });

            pageA = await CreatePublishedPageAsync(api, siteA, $"Contact Test A {suffix}", $"contact-test-a-{suffix}");
            pageB = await CreatePublishedPageAsync(api, siteB, $"Contact Test B {suffix}", $"contact-test-b-{suffix}");

            var htmlA = await GetHtmlAsync(pageA.Permalink, HostnameOf(siteA));
            var htmlB = await GetHtmlAsync(pageB.Permalink, HostnameOf(siteB));

            // Site A's page shows Site A's own values...
            Assert.Contains(phoneA, htmlA);
            Assert.Contains($"https://zalo.me/siteA-{suffix}", htmlA);
            Assert.Contains($"123 Site A Street {suffix}", htmlA);
            Assert.Contains($"https://maps.google.com/?q=siteA-{suffix}", htmlA);

            // ...and never Site B's.
            Assert.DoesNotContain(phoneB, htmlA);
            Assert.DoesNotContain($"https://zalo.me/siteB-{suffix}", htmlA);
            Assert.DoesNotContain($"456 Site B Street {suffix}", htmlA);
            Assert.DoesNotContain($"https://maps.google.com/?q=siteB-{suffix}", htmlA);

            // Site B's page shows Site B's own values...
            Assert.Contains(phoneB, htmlB);
            Assert.Contains($"https://zalo.me/siteB-{suffix}", htmlB);
            Assert.Contains($"456 Site B Street {suffix}", htmlB);
            Assert.Contains($"https://maps.google.com/?q=siteB-{suffix}", htmlB);

            // ...and never Site A's.
            Assert.DoesNotContain(phoneA, htmlB);
            Assert.DoesNotContain($"https://zalo.me/siteA-{suffix}", htmlB);
            Assert.DoesNotContain($"123 Site A Street {suffix}", htmlB);
            Assert.DoesNotContain($"https://maps.google.com/?q=siteA-{suffix}", htmlB);
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
    public async Task Phone_Renders_As_Real_Tel_Link_With_Digits_Only_Href_And_Unchanged_Display_Text()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var suffix = Guid.NewGuid().ToString("N")[..6];
        // Deliberately includes a leading "+", spaces and parens - none of
        // these must survive into the tel: href, but the visible text must
        // keep them exactly as entered (once decoded back from whatever
        // Razor's @-expression HTML-encoding did to them - "+" for instance
        // renders as the numeric entity "&#x2B;", same as _MetaTags.cshtml's
        // convention would do to any other value with encodable characters).
        var displayPhone = $"+84 (090) 123-{suffix}";

        StandardPage? page = null;

        try
        {
            await SaveSettingsAsync(api, site.Id, s => s.Phone = displayPhone);

            page = await CreatePublishedPageAsync(api, site, $"Phone Test {suffix}", $"phone-test-{suffix}");
            var html = await GetHtmlAsync(page.Permalink, HostnameOf(site));

            // A leading "+" (internationally-formatted number) must survive
            // into the href alongside the digits - only punctuation/spacing
            // other than that leading "+" is stripped. The href attribute
            // value goes through the same Razor @-expression HTML-encoding
            // as the display text, so a literal "+" in the raw markup is
            // "&#x2B;" - harmless (HTML parsers decode it back to "+" when
            // reading the attribute), but the raw-source match below has to
            // expect the encoded form too.
            var digitsOnly = new string(Array.FindAll(displayPhone.ToCharArray(), char.IsDigit));
            var expectedTelHref = (displayPhone.TrimStart().StartsWith("+") ? "+" : "") + digitsOnly;
            var encodedTelHref = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(expectedTelHref);
            Assert.Contains($"href=\"tel:{encodedTelHref}\"", html);
            // Razor's @-expression encoder (System.Text.Encodings.Web.HtmlEncoder.Default)
            // is stricter than System.Net.WebUtility.HtmlEncode used elsewhere in
            // this suite - it also encodes "+" (as "&#x2B;"), so match with the
            // exact encoder the partial actually goes through.
            var encodedDisplayPhone = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(displayPhone);
            Assert.Contains($">{encodedDisplayPhone}<", html);
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
    public async Task Phone_With_Only_A_Stray_Digit_In_Garbage_Text_Is_Treated_As_Unset()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var suffix = Guid.NewGuid().ToString("N")[..6];

        StandardPage? page = null;

        try
        {
            // "javascript:alert(1)" strips down to a single digit ("1" from
            // "(1)") - not a real phone number. Must be treated the same as
            // an empty phone (omitted), not rendered as href="tel:1".
            await SaveSettingsAsync(api, site.Id, s =>
            {
                s.Phone = "javascript:alert(1)";
                s.ZaloUrl = string.Empty;
                s.Address = string.Empty;
                s.MapsUrl = string.Empty;
                s.Email = string.Empty;
            });

            page = await CreatePublishedPageAsync(api, site, $"Garbage Phone Test {suffix}", $"garbage-phone-test-{suffix}");
            var html = await GetHtmlAsync(page.Permalink, HostnameOf(site));

            // Story 6.1: Site A's contact surfaces are the nav chip and the
            // footer strip - neither may show a garbage phone.
            Assert.DoesNotContain("class=\"sa-chip\"", html);
            Assert.DoesNotContain("sa-footer__contact", html);
            Assert.DoesNotContain("href=\"tel:", html);
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
    public async Task Save_Rejects_Unsafe_Zalo_Url_Scheme_And_Does_Not_Persist()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var safeZalo = "https://zalo.me/still-safe-after-rejected-save";

        try
        {
            await SaveSettingsAsync(api, site.Id, s => s.ZaloUrl = safeZalo);

            await Assert.ThrowsAsync<ValidationException>(() =>
                SaveSettingsAsync(api, site.Id, s => s.ZaloUrl = "javascript:alert(1)"));

            // The rejected save must not have persisted - the last safe
            // value stays in place, mirroring the defense-in-depth intent:
            // block at save time, don't just filter at render time.
            var afterRejectedSave = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.Equal(safeZalo, afterRejectedSave!.ZaloUrl?.Value);
        }
        finally
        {
            await RestoreAsync(api, site.Id, original);
        }
    }

    [Fact]
    public async Task Save_Rejects_Unsafe_Maps_Url_Scheme_And_Does_Not_Persist()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var safeMaps = "https://maps.google.com/?q=still-safe-after-rejected-save";

        try
        {
            await SaveSettingsAsync(api, site.Id, s => s.MapsUrl = safeMaps);

            await Assert.ThrowsAsync<ValidationException>(() =>
                SaveSettingsAsync(api, site.Id, s => s.MapsUrl = "data:text/html,<script>alert(1)</script>"));

            var afterRejectedSave = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.Equal(safeMaps, afterRejectedSave!.MapsUrl?.Value);
        }
        finally
        {
            await RestoreAsync(api, site.Id, original);
        }
    }

    // --- Story 1.9 (security fix): Piranha Manager's built-in UI never saves
    // SiteSettings as the strongly-typed POCO above - it always round-trips
    // a DynamicSiteContent built from the same stored regions (see
    // App.Hooks.SiteContent.RegisterOnBeforeSave in Program.cs). The facts
    // above only ever exercise the app's own typed SaveSettingsAsync path,
    // so they passed even while the real Manager save path stayed wide
    // open. These facts save through a real DynamicSiteContent - loaded and
    // saved the same way Manager's own save path does - to prove the fix
    // and guard against the bypass reopening.

    [Fact]
    public async Task Manager_Save_Path_Rejects_Unsafe_Zalo_Url_Via_DynamicSiteContent()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var safeZalo = "https://zalo.me/still-safe-after-rejected-manager-save";

        try
        {
            // Known-safe baseline saved through the typed path, so the
            // rejected Manager-shaped save below can be proven not to have
            // overwritten it.
            await SaveSettingsAsync(api, site.Id, s => s.ZaloUrl = safeZalo);

            var dyn = await LoadDynamicContentAsync(api, site.Id);
            SetRegionValue(dyn, nameof(SiteSettings.ZaloUrl), "javascript:alert(1)");

            var rejection = await Assert.ThrowsAsync<ValidationException>(() => api.Sites.SaveContentAsync(site.Id, dyn));
            // Same message the typed path produces - the two paths must not
            // drift into reporting the same fault differently.
            Assert.Equal("Zalo URL must be a valid http:// or https:// link.", rejection.Message);

            var afterRejectedSave = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.Equal(safeZalo, afterRejectedSave!.ZaloUrl?.Value);
        }
        finally
        {
            await RestoreAsync(api, site.Id, original);
        }
    }

    [Fact]
    public async Task Manager_Save_Path_Rejects_Unsafe_Maps_Url_Via_DynamicSiteContent()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var safeMaps = "https://maps.google.com/?q=still-safe-after-rejected-manager-save";

        try
        {
            await SaveSettingsAsync(api, site.Id, s => s.MapsUrl = safeMaps);

            var dyn = await LoadDynamicContentAsync(api, site.Id);
            SetRegionValue(dyn, nameof(SiteSettings.MapsUrl), "data:text/html,<script>alert(1)</script>");

            var rejection = await Assert.ThrowsAsync<ValidationException>(() => api.Sites.SaveContentAsync(site.Id, dyn));
            Assert.Equal("Maps URL must be a valid http:// or https:// link.", rejection.Message);

            var afterRejectedSave = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.Equal(safeMaps, afterRejectedSave!.MapsUrl?.Value);
        }
        finally
        {
            await RestoreAsync(api, site.Id, original);
        }
    }

    [Fact]
    public async Task Manager_Save_Path_Rejects_Malformed_Ga4_Id_Via_DynamicSiteContent()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        const string safeGa4Id = "G-STILLSAFE1";

        try
        {
            await SaveSettingsAsync(api, site.Id, s => s.Ga4MeasurementId = safeGa4Id);

            var dyn = await LoadDynamicContentAsync(api, site.Id);
            // Below the "G-" + 4-char minimum length floor - a truncated/
            // mistyped paste, same shape as AnalyticsSearchConsoleTests's
            // typed-path regression fact.
            SetRegionValue(dyn, nameof(SiteSettings.Ga4MeasurementId), "G-X");

            var rejection = await Assert.ThrowsAsync<ValidationException>(() => api.Sites.SaveContentAsync(site.Id, dyn));
            Assert.Equal("GA4 Measurement ID must look like G-XXXXXXXXXX (letters/digits only).", rejection.Message);

            var afterRejectedSave = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.Equal(safeGa4Id, afterRejectedSave!.Ga4MeasurementId?.Value);
        }
        finally
        {
            await RestoreAsync(api, site.Id, original);
        }
    }

    [Fact]
    public async Task Manager_Save_Path_Rejects_Invalid_Search_Console_Verification_Via_DynamicSiteContent()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        const string safeVerification = "still-safe-after-rejected-manager-save";

        try
        {
            await SaveSettingsAsync(api, site.Id, s => s.SearchConsoleVerification = safeVerification);

            var dyn = await LoadDynamicContentAsync(api, site.Id);
            SetRegionValue(dyn, nameof(SiteSettings.SearchConsoleVerification), "\"><script>alert(1)</script>");

            var rejection = await Assert.ThrowsAsync<ValidationException>(() => api.Sites.SaveContentAsync(site.Id, dyn));
            Assert.Equal("Search Console Verification must contain only letters, digits, '.', '-', '_', '=' or '+'.", rejection.Message);

            var afterRejectedSave = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.Equal(safeVerification, afterRejectedSave!.SearchConsoleVerification?.Value);
        }
        finally
        {
            await RestoreAsync(api, site.Id, original);
        }
    }

    [Fact]
    public async Task Manager_Save_Path_Accepts_Valid_Values_Via_DynamicSiteContent()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var validZalo = $"https://zalo.me/manager-valid-{suffix}";
        var validMaps = $"https://maps.google.com/?q=manager-valid-{suffix}";
        var validGa4 = $"G-{suffix}AAAA";
        var validVerification = $"manager-valid-verify-{suffix}";

        try
        {
            var dyn = await LoadDynamicContentAsync(api, site.Id);
            SetRegionValue(dyn, nameof(SiteSettings.ZaloUrl), validZalo);
            SetRegionValue(dyn, nameof(SiteSettings.MapsUrl), validMaps);
            SetRegionValue(dyn, nameof(SiteSettings.Ga4MeasurementId), validGa4);
            SetRegionValue(dyn, nameof(SiteSettings.SearchConsoleVerification), validVerification);

            // No exception - a real Manager save of valid values must
            // succeed exactly as the typed path already does.
            await api.Sites.SaveContentAsync(site.Id, dyn);

            var afterSave = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.Equal(validZalo, afterSave!.ZaloUrl?.Value);
            Assert.Equal(validMaps, afterSave.MapsUrl?.Value);
            Assert.Equal(validGa4, afterSave.Ga4MeasurementId?.Value);
            Assert.Equal(validVerification, afterSave.SearchConsoleVerification?.Value);
        }
        finally
        {
            await RestoreAsync(api, site.Id, original);
        }
    }

    [Fact]
    public async Task Manager_Save_Path_Treats_Empty_Or_Missing_Fields_As_Unset_Via_DynamicSiteContent()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        try
        {
            // Seed known-valid values first, so an empty/whitespace/absent
            // value below can only be explained by the "always accepted"
            // rule, not by there being nothing to validate either way.
            await SaveSettingsAsync(api, site.Id, s =>
            {
                s.ZaloUrl = "https://zalo.me/before-clearing";
                s.MapsUrl = "https://maps.google.com/?q=before-clearing";
                s.Ga4MeasurementId = "G-BEFORECLEAR1";
            });

            var dyn = await LoadDynamicContentAsync(api, site.Id);
            // Empty string, per the matrix's "field empty" case.
            SetRegionValue(dyn, nameof(SiteSettings.ZaloUrl), string.Empty);
            // Whitespace-only, the other half of the "empty/whitespace is
            // always accepted" rule - IsNullOrWhiteSpace is what implements
            // both, so neither should reach a validator.
            SetRegionValue(dyn, nameof(SiteSettings.MapsUrl), "   ");
            // Region key entirely absent from Regions, per the matrix's
            // "region key missing from Regions" case - RemoveRegion actually
            // removes the dictionary entry, not just blanking its value.
            RemoveRegion(dyn, nameof(SiteSettings.Ga4MeasurementId));

            // No exception - all three must be treated as unset, not validated.
            await api.Sites.SaveContentAsync(site.Id, dyn);

            var afterSave = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.True(string.IsNullOrEmpty(afterSave!.ZaloUrl?.Value));
            Assert.True(string.IsNullOrWhiteSpace(afterSave.MapsUrl?.Value));
            // An absent region is not part of the save payload at all, so its
            // stored value is left exactly as it was. What matters here is
            // that the hook accepted the save rather than validating a field
            // that was never submitted.
            Assert.Equal("G-BEFORECLEAR1", afterSave.Ga4MeasurementId?.Value);
        }
        finally
        {
            await RestoreAsync(api, site.Id, original);
        }
    }

    [Fact]
    public async Task ZaloUrl_Renders_As_A_Direct_Link_With_No_Rewriting()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var zaloUrl = $"https://zalo.me/{suffix}";

        StandardPage? page = null;

        try
        {
            await SaveSettingsAsync(api, site.Id, s => s.ZaloUrl = zaloUrl);

            page = await CreatePublishedPageAsync(api, site, $"Zalo Test {suffix}", $"zalo-test-{suffix}");
            var html = await GetHtmlAsync(page.Permalink, HostnameOf(site));

            Assert.Contains($"href=\"{zaloUrl}\"", html);
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
    public async Task MapsUrl_Renders_As_A_Link_To_The_Sites_Own_Location()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var mapsUrl = $"https://maps.google.com/?q={suffix}";

        StandardPage? page = null;

        try
        {
            await SaveSettingsAsync(api, site.Id, s => s.MapsUrl = mapsUrl);

            page = await CreatePublishedPageAsync(api, site, $"Maps Test {suffix}", $"maps-test-{suffix}");
            var html = await GetHtmlAsync(page.Permalink, HostnameOf(site));

            Assert.Contains($"href=\"{mapsUrl}\"", html);
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
    public async Task Unset_Fields_Are_Omitted_Not_Rendered_As_Broken_Links()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var suffix = Guid.NewGuid().ToString("N")[..6];

        StandardPage? page = null;

        try
        {
            // All contact fields explicitly cleared - the chip, the footer's
            // contact line, and each individual element, must be omitted
            // rather than rendered with an empty/broken href.
            await SaveSettingsAsync(api, site.Id, s =>
            {
                s.Phone = string.Empty;
                s.ZaloUrl = string.Empty;
                s.Address = string.Empty;
                s.MapsUrl = string.Empty;
                s.Email = string.Empty;
            });

            page = await CreatePublishedPageAsync(api, site, $"Empty Contact Test {suffix}", $"empty-contact-test-{suffix}");
            var html = await GetHtmlAsync(page.Permalink, HostnameOf(site));

            // Story 6.1: with every contact field empty, neither the nav chip
            // nor the footer strip's contact line renders at all.
            Assert.DoesNotContain("class=\"sa-chip\"", html);
            Assert.DoesNotContain("sa-footer__contact", html);
            Assert.DoesNotContain("href=\"tel:", html);
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
    public async Task Mixed_State_Renders_Only_The_Fields_That_Are_Set()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var suffix = Guid.NewGuid().ToString("N")[..6];
        // Fixed digit prefix guarantees >= 7 digits regardless of how many
        // of the hex suffix's characters happen to be digits (0-9 vs a-f).
        var phone = $"090 555 1234 {suffix}";
        var address = $"789 Mixed State Ave {suffix}";

        StandardPage? page = null;

        try
        {
            // A realistic partially-filled state: Phone and Address set,
            // ZaloUrl/MapsUrl left blank - proves each @if block in Site A's
            // chip and footer is gated independently rather than all four
            // rising or falling together.
            await SaveSettingsAsync(api, site.Id, s =>
            {
                s.Phone = phone;
                s.Address = address;
                s.ZaloUrl = string.Empty;
                s.MapsUrl = string.Empty;
            });

            page = await CreatePublishedPageAsync(api, site, $"Mixed State Test {suffix}", $"mixed-state-test-{suffix}");
            var html = await GetHtmlAsync(page.Permalink, HostnameOf(site));

            // Story 6.1: Site A renders these through the nav chip and the
            // footer strip.
            Assert.Contains("class=\"sa-chip\"", html);
            Assert.Contains("sa-chip__link--call\" href=\"tel:", html);
            Assert.Contains("sa-footer__phone\" href=\"tel:", html);
            Assert.Contains(address, html);
            Assert.DoesNotContain("sa-chip__link--zalo", html);
            Assert.DoesNotContain("sa-footer__zalo", html);
            Assert.DoesNotContain("sa-footer__maps", html);
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
    public async Task Address_With_Html_Special_Characters_Is_Encoded_Without_Breaking_Markup()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        var suffix = Guid.NewGuid().ToString("N")[..6];
        // "&", "<", ">" and """ must all be encoded - an unescaped """ here
        // would break out of an attribute and an unescaped "<" would break
        // the surrounding markup, neither of which a plain substring match
        // below would catch.
        var address = $"123 \"Main\" St <District 1> & Ward {suffix}";

        StandardPage? page = null;

        try
        {
            await SaveSettingsAsync(api, site.Id, s => s.Address = address);

            page = await CreatePublishedPageAsync(api, site, $"Address Escaping Test {suffix}", $"address-escaping-test-{suffix}");
            var html = await GetHtmlAsync(page.Permalink, HostnameOf(site));

            // Use the same encoder Razor's @-expressions actually go
            // through (System.Text.Encodings.Web.HtmlEncoder.Default) -
            // consistent with the Phone test above, and future-proof
            // against values (like Phone's "+") where it diverges from
            // System.Net.WebUtility.HtmlEncode.
            Assert.Contains(System.Text.Encodings.Web.HtmlEncoder.Default.Encode(address), html);
            Assert.DoesNotContain($">{address}<", html);
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
    public async Task Reseeding_Twice_Produces_No_Duplicate_Or_Overwritten_SiteSettings()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await SnapshotAsync(api, site.Id);

        const string managerEditedPhone = "090-manager-edited";

        try
        {
            // The app has already started once (via this same
            // WebApplicationFactory), which already ran
            // SiteSettingsSeed.EnsureSeededAsync as part of startup - so an
            // empty SiteSettings instance already exists for every site.
            var beforeReseed = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.NotNull(beforeReseed);

            // Simulate a Manager-made edit between restarts.
            await SaveSettingsAsync(api, site.Id, s => s.Phone = managerEditedPhone);

            // Re-run the exact startup seed step again.
            await SiteSettingsSeed.EnsureSeededAsync(api);

            var afterReseed = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            Assert.NotNull(afterReseed);
            Assert.Equal(managerEditedPhone, afterReseed!.Phone?.Value);
        }
        finally
        {
            await RestoreAsync(api, site.Id, original);
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

    /// <summary>
    /// Loads the site's current settings the same shape Piranha Manager's
    /// own save path always round-trips - a generic <see cref="DynamicSiteContent"/>
    /// built from the stored regions (Piranha.Services.ContentFactory), never
    /// the app's own strongly-typed <see cref="SiteSettings"/> POCO. This is
    /// the real construction the Story 1.9 fix in Program.cs's
    /// <c>RegisterOnBeforeSave</c> hook must run its checks against.
    /// </summary>
    private static async Task<DynamicSiteContent> LoadDynamicContentAsync(IApi api, Guid siteId)
    {
        var dyn = await api.Sites.GetContentByIdAsync(siteId);
        Assert.NotNull(dyn);
        return dyn!;
    }

    /// <summary>
    /// A single-field region's value in <see cref="DynamicSiteContent.Regions"/>
    /// (an ExpandoObject) is the raw <see cref="StringField"/> instance
    /// itself, not further wrapped - so setting it means mutating that
    /// field's <c>Value</c> in place (or inserting a new one, if the region
    /// somehow isn't present yet).
    /// </summary>
    private static void SetRegionValue(DynamicSiteContent dyn, string regionKey, string? value)
    {
        var regions = (IDictionary<string, object>)dyn.Regions;
        if (regions.TryGetValue(regionKey, out var existing) && existing is StringField field)
        {
            field.Value = value;
        }
        else
        {
            regions[regionKey] = new StringField { Value = value };
        }
    }

    /// <summary>
    /// Simulates the region key being entirely absent from <see cref="DynamicSiteContent.Regions"/>
    /// (the I/O matrix's "region key missing from Regions" case) - distinct
    /// from <see cref="SetRegionValue"/> with an empty string, which leaves
    /// the key present with a blank value. Asserts the key really was there
    /// to remove, so the "missing key" case can't quietly degrade into
    /// "key that was never present anyway".
    /// </summary>
    private static void RemoveRegion(DynamicSiteContent dyn, string regionKey) =>
        Assert.True(
            ((IDictionary<string, object>)dyn.Regions).Remove(regionKey),
            $"Expected region '{regionKey}' to be present before removing it.");

    /// <summary>
    /// Captures every field value a test could mutate, so they can be
    /// restored afterward - this suite shares one real, persistent database
    /// with every other test class in <see cref="PiranhaAppCollection"/> (see
    /// its own doc comment). Every field is captured (including Story 1.7's
    /// NotificationEmails), not just the four
    /// the contact-block tests touch, so a single restore call is always
    /// enough.
    /// </summary>
    private static async Task<SiteSettingsSnapshot> SnapshotAsync(IApi api, Guid siteId)
    {
        var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(siteId);
        return new SiteSettingsSnapshot(
            settings?.Phone?.Value,
            settings?.ZaloUrl?.Value,
            settings?.Address?.Value,
            settings?.MapsUrl?.Value,
            settings?.Ga4MeasurementId?.Value,
            settings?.SearchConsoleVerification?.Value,
            settings?.NotificationEmails?.Value,
            settings?.Email?.Value);
    }

    private static async Task RestoreAsync(IApi api, Guid siteId, SiteSettingsSnapshot original)
    {
        await SaveSettingsAsync(api, siteId, s =>
        {
            s.Phone = original.Phone;
            s.ZaloUrl = original.ZaloUrl;
            s.Address = original.Address;
            s.MapsUrl = original.MapsUrl;
            s.Ga4MeasurementId = original.Ga4MeasurementId;
            s.SearchConsoleVerification = original.SearchConsoleVerification;
            s.NotificationEmails = original.NotificationEmails;
            s.Email = original.Email;
        });
    }

    private sealed record SiteSettingsSnapshot(
        string? Phone,
        string? ZaloUrl,
        string? Address,
        string? MapsUrl,
        string? Ga4MeasurementId,
        string? SearchConsoleVerification,
        string? NotificationEmails,
        string? Email);

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
