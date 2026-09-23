using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using TbTruongHoc.Web.Data;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Covers the spec's I/O &amp; Edge-Case Matrix row 4: re-running the startup
/// seed when the two Site records already exist must be a no-op - no
/// duplicate Site records created. This calls the exact same
/// <see cref="SiteSeed.EnsureSeededAsync"/> method Program.cs invokes at
/// startup, against the real MariaDB-backed <see cref="IApi"/> the app uses
/// (not an in-memory provider), because the seed had to specifically work
/// around Piranha's own EF Core bootstrap behavior (which auto-creates a
/// placeholder "Default" Site the first time the database is touched) -
/// that interaction is provider-specific and only observable against the
/// real database.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class SiteSeedIdempotencyTests
{
    private readonly PiranhaWebApplicationFactory _factory;

    public SiteSeedIdempotencyTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reseeding_Twice_Produces_No_Duplicate_Sites()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // The app has already started once (via this same WebApplicationFactory),
        // which already ran SiteSeed.EnsureSeededAsync as part of its own
        // startup - so the two Site records already exist. Confirm that
        // baseline first.
        var beforeSites = (await api.Sites.GetAllAsync()).ToList();
        Assert.Equal(2, beforeSites.Count);

        // Re-run the exact startup seed step a first additional time.
        await SiteSeed.EnsureSeededAsync(api, configuration);

        var afterFirstRerun = (await api.Sites.GetAllAsync())
            .OrderBy(s => s.InternalId)
            .ToList();
        Assert.Equal(2, afterFirstRerun.Count);

        var idsAfterFirstRerun = afterFirstRerun.Select(s => s.Id).ToList();
        var internalIdsAfterFirstRerun = afterFirstRerun.Select(s => s.InternalId).ToList();

        // Simulate a Manager-made edit to an existing site's Hostnames
        // between restarts. SiteSeed.cs documents that re-seeding must never
        // clobber such an edit on an already-seeded site - assert that here
        // rather than just Id/InternalId/count, so a regression that made
        // the seed overwrite Hostnames on every restart would be caught.
        const string managerEditedHostnames = "manager-edited.example";
        var siteToMutate = afterFirstRerun.Single(s => s.InternalId == SiteSeed.TbTruongHocInternalId);
        var originalHostnames = siteToMutate.Hostnames;
        try
        {
            siteToMutate.Hostnames = managerEditedHostnames;
            await api.Sites.SaveAsync(siteToMutate);

            // Re-run it again - same result, same IDs, no duplicates.
            await SiteSeed.EnsureSeededAsync(api, configuration);

            var afterSecondRerun = (await api.Sites.GetAllAsync())
                .OrderBy(s => s.InternalId)
                .ToList();

            Assert.Equal(2, afterSecondRerun.Count);
            Assert.Equal(internalIdsAfterFirstRerun, afterSecondRerun.Select(s => s.InternalId).ToList());
            Assert.Equal(idsAfterFirstRerun, afterSecondRerun.Select(s => s.Id).ToList());

            // The Manager-style edit must have survived the re-seed untouched -
            // not been reverted back to the configured hostnames.
            var mutatedSiteAfterReseed = afterSecondRerun.Single(s => s.InternalId == SiteSeed.TbTruongHocInternalId);
            Assert.Equal(managerEditedHostnames, mutatedSiteAfterReseed.Hostnames);

            // And the expected two sites are exactly the ones from Story 1.1 -
            // no third/unexpected Site record snuck in.
            Assert.Contains(afterSecondRerun, s => s.InternalId == SiteSeed.TbTruongHocInternalId && s.IsDefault);
            Assert.Contains(afterSecondRerun, s => s.InternalId == SiteSeed.TrongDoiTamInternalId && !s.IsDefault);
        }
        finally
        {
            // This test shares one real, persistent database with every
            // other test class in PiranhaAppCollection (see its own doc
            // comment) - restore the hostname this test intentionally
            // mutated so sibling tests (e.g. hostname resolution) that run
            // later in the same process still see the seeded value.
            siteToMutate.Hostnames = originalHostnames;
            await api.Sites.SaveAsync(siteToMutate);
        }
    }
}
