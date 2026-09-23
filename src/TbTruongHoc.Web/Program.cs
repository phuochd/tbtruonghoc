using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Piranha;
using Piranha.AttributeBuilder;
using Piranha.AspNetCore.Identity.MySQL;
using Piranha.Cache;
using Piranha.Data.EF.MySql;
using Piranha.Manager.Editor;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddPiranha(options =>
{
    /**
     * This will enable automatic reload of .cshtml
     * without restarting the application. However since
     * this adds a slight overhead it should not be
     * enabled in production.
     */
    options.AddRazorRuntimeCompilation = true;

    options.UseCms();
    options.UseManager();

    options.UseFileStorage(naming: Piranha.Local.FileStorageNaming.UniqueFolderNames);
    options.UseImageSharp();
    options.UseTinyMCE();
    options.UseMemoryCache();

    var connectionString = builder.Configuration.GetConnectionString("piranha");
    var serverVersion = new MariaDbServerVersion(new Version(10, 11, 0));

    options.UseEF<MySqlDb>(db => db.UseMySql(connectionString, serverVersion));
    options.UseIdentityWithSeed<IdentityMySQLDb>(db => db.UseMySql(connectionString, serverVersion));

    // Story 1.4 (FR-3): a second, standalone DbContext for the FormSubmission
    // table - isolated from Piranha's own MySqlDb/migrations, same
    // connection string/server version. Registered here (rather than via
    // builder.Services directly) so it shares this same connectionString
    // resolution with Piranha's own contexts above.
    builder.Services.AddDbContext<LeadDbContext>(db => db.UseMySql(connectionString, serverVersion));

    /**
     * Here you can configure the different permissions
     * that you want to use for securing content in the
     * application.
    options.UseSecurity(o =>
    {
        o.UsePermission("WebUser", "Web User");
    });
     */

    /**
     * Here you can specify the login url for the front end
     * application. This does not affect the login url of
     * the manager interface.
    options.LoginUrl = "login";
     */
});

var app = builder.Build();

// Resolved once at startup for the OnBeforeSave hook below - Piranha's own
// get-mutate-save pattern (used by both Manager and any IApi caller) writes
// the new field values into the *cached* content instance before Save is
// even called, so a hook that only throws (without also evicting the cache
// entry) still leaves the rejected/unsafe value sitting in memory for any
// other reader until the next successful save. May be null if UseMemoryCache()
// were ever removed - the hook below guards for that.
var siteContentCache = app.Services.GetService<ICache>();

// Resolved once at startup for the Manager menu registration's defensive
// null-check below (same resolve-once-before-UsePiranha pattern as
// siteContentCache above).
var startupLogger = app.Services.GetService<ILogger<Program>>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UsePiranha(options =>
{
    // Initialize Piranha
    App.Init(options.Api);

    // Story 1.5 (AD-3): register the "Danh sách khách để lại thông tin" entry
    // into Piranha's own Manager menu - mirrors this file's existing inline-
    // registration style for hooks/seeding below rather than introducing a
    // new IModule class (Code Map).
    //
    // Piranha.Manager.Menu.Items (see Piranha.Manager.Menu's own source) is a
    // strictly two-level structure: top-level entries are headers only
    // (_Menu.cshtml renders a group's own Route as nothing - only its
    // Css/Name - and then loops group.Items for the actual clickable links),
    // so the new leaf MenuItem must be added to an existing group's Items,
    // not appended to Menu.Items directly. "Content" is the built-in group
    // this naturally belongs under, alongside "Pages"/"Media"/"Comments".
    // Guarded so re-running this callback (e.g. a second WebApplicationFactory
    // in the same process) never adds a duplicate entry.
    var contentMenuGroup = Piranha.Manager.Menu.Items["Content"];
    if (contentMenuGroup != null)
    {
        if (contentMenuGroup.Items["Leads"] == null)
        {
            contentMenuGroup.Items.Add(new Piranha.Manager.MenuItem
            {
                InternalId = "Leads",
                Name = "Danh sách khách để lại thông tin",
                Route = "~/manager/leads",
                Policy = Piranha.Manager.Permission.Admin,
                Css = "fas fa-address-book"
            });
        }
    }
    else
    {
        // Should never happen with piranha.manager 12.0.0's built-in menu -
        // this only fires if a future Piranha upgrade renames/removes the
        // "Content" group. Logged rather than silently skipped, since the
        // failure mode is otherwise invisible: the Leads screen would still
        // work at ~/manager/leads, just with no menu link to reach it.
        startupLogger?.LogWarning(
            "Could not register the Manager menu entry for the Leads screen: Piranha.Manager.Menu.Items has no 'Content' group. The screen is still reachable directly at ~/manager/leads.");
    }

    // Defense in depth for Story 1.3's Zalo/Maps fields: _ContactBlock.cshtml
    // already skips rendering an unsafe (non-http/https) scheme, but reject
    // it at save time too so a future view that renders SiteSettings without
    // going through that partial can't reintroduce the same risk. Registered
    // on the SiteContentBase hook (the static type SiteService.SaveContentAsync
    // actually invokes the hook with) so it fires for SiteSettings saves.
    App.Hooks.SiteContent.RegisterOnBeforeSave(model =>
    {
        if (model is not SiteSettings settings)
        {
            return;
        }

        var zaloUnsafe = !string.IsNullOrWhiteSpace(settings.ZaloUrl?.Value) && !SiteSettingsValidation.IsSafeAbsoluteUrl(settings.ZaloUrl.Value);
        var mapsUnsafe = !string.IsNullOrWhiteSpace(settings.MapsUrl?.Value) && !SiteSettingsValidation.IsSafeAbsoluteUrl(settings.MapsUrl.Value);

        // Story 1.6 (FR-4): same defense-in-depth for the GA4/verification
        // fields _Analytics.cshtml embeds directly into an inline <script>
        // and an attribute - a value crafted to break out of either must
        // never persist.
        var ga4Unsafe = !string.IsNullOrWhiteSpace(settings.Ga4MeasurementId?.Value) && !SiteSettingsValidation.IsValidGa4MeasurementId(settings.Ga4MeasurementId.Value);
        var verificationUnsafe = !string.IsNullOrWhiteSpace(settings.SearchConsoleVerification?.Value) && !SiteSettingsValidation.IsValidSearchConsoleVerification(settings.SearchConsoleVerification.Value);

        if (!zaloUnsafe && !mapsUnsafe && !ga4Unsafe && !verificationUnsafe)
        {
            return;
        }

        // Evict the poisoned in-memory copy (see siteContentCache's comment
        // above) so the next read - Manager's own or _ContactBlock.cshtml's/
        // _Analytics.cshtml's - falls back to the last valid, still-
        // unmodified DB row instead of this in-place-mutated object.
        siteContentCache?.RemoveAsync($"SiteContent_{model.Id}").GetAwaiter().GetResult();

        throw new ValidationException(zaloUnsafe
            ? "Zalo URL must be a valid http:// or https:// link."
            : mapsUnsafe
                ? "Maps URL must be a valid http:// or https:// link."
                : ga4Unsafe
                    ? "GA4 Measurement ID must look like G-XXXXXXXXXX (letters/digits only)."
                    : "Search Console Verification must contain only letters, digits, '-' or '_'.");
    });

    // Build content types
    new ContentTypeBuilder(options.Api)
        .AddAssembly(typeof(Program).Assembly)
        .Build()
        .DeleteOrphans();

    // Configure Tiny MCE
    EditorConfig.FromFile("editorconfig.json");

    options.UseManager();
    options.UseTinyMCE();
    options.UseIdentity();

    // Story 1.4 (FR-3): apply LeadDbContext's own EF Core Migrations at
    // startup - mirrors how Piranha's own tables ship pre-built migrations
    // that apply on first startup (options.UseEF<MySqlDb> above does this
    // internally for Piranha's schema; LeadDbContext needs it done
    // explicitly since it isn't a Piranha-managed context). Safe to run on
    // every startup - a no-op once the schema is current.
    using (var migrationScope = app.Services.CreateScope())
    {
        var leadDb = migrationScope.ServiceProvider.GetRequiredService<LeadDbContext>();
        leadDb.Database.MigrateAsync().GetAwaiter().GetResult();
    }

    // Idempotently seed the "tbtruonghoc" (default) and "trongdoitam.net"
    // Site records. Safe to run on every startup - a no-op once seeded.
    SiteSeed.EnsureSeededAsync(options.Api, app.Configuration).GetAwaiter().GetResult();

    // Idempotently seed an empty SiteSettings content instance (Story 1.3's
    // Phone/ZaloUrl/Address/MapsUrl fields) for each site seeded above. Must
    // run after SiteSeed so both Site records already exist. Safe to run on
    // every startup - a no-op once seeded.
    SiteSettingsSeed.EnsureSeededAsync(options.Api).GetAwaiter().GetResult();
});

app.Run();

/// <summary>
/// Exposes the implicit top-level Program class publicly so that
/// <c>Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory&lt;Program&gt;</c>
/// in the test project can boot this app in-process. Test-only concern;
/// does not change any runtime behavior.
/// </summary>
public partial class Program { }