using Piranha;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// Idempotent startup seed that guarantees every Piranha <see cref="Piranha.Models.Site"/>
/// (both "tbtruonghoc" and "trongdoitam-net", seeded by <see cref="SiteSeed"/>
/// immediately before this runs) has its own empty <see cref="SiteSettings"/>
/// content instance, so Piranha Manager's built-in per-site settings edit UI
/// has something to open on first use. Mirrors <see cref="SiteSeed.EnsureSeededAsync"/>'s
/// get-or-create pattern: never overwrites an already-saved instance, so a
/// Manager-edited Phone/ZaloUrl/Address/MapsUrl/Ga4MeasurementId/
/// SearchConsoleVerification value is never clobbered by a later restart.
/// </summary>
public static class SiteSettingsSeed
{
    /// <summary>
    /// The <see cref="Piranha.Models.SiteType"/> id Piranha.AttributeBuilder.ContentTypeBuilder
    /// registers for <see cref="SiteSettings"/> (confirmed by decompiling
    /// Piranha.AttributeBuilder.ContentTypeBuilder.GetSiteType: since the
    /// <c>[SiteType]</c> attribute on <see cref="SiteSettings"/> sets no
    /// explicit <c>Id</c>, Piranha defaults it to the CLR type's own
    /// <see cref="Type.Name"/>).
    ///
    /// A <see cref="Piranha.Models.Site"/> record's own <c>SiteTypeId</c>
    /// must be set to this value before <c>ISiteService.SaveContentAsync</c>
    /// will accept content for it - confirmed by decompiling
    /// Piranha.Repositories.SiteRepository.SaveContent&lt;T&gt;, which
    /// throws <see cref="MissingFieldException"/>("Can't save content for a
    /// site that doesn't have a Site Type Id.") when <c>Site.SiteTypeId</c>
    /// is empty. <see cref="SiteSeed"/> creates its two <c>Site</c> records
    /// with no <c>SiteTypeId</c> at all (Story 1.1 predates this model), so
    /// this seed must assign it the first time it runs for a given site.
    /// </summary>
    private static readonly string SiteSettingsTypeId = typeof(SiteSettings).Name;

    public static async Task EnsureSeededAsync(IApi api)
    {
        var sites = await api.Sites.GetAllAsync();

        foreach (var site in sites)
        {
            // ISiteService.GetContentByIdAsync<T> (confirmed by decompiling
            // Piranha.Repositories.SiteRepository.GetContentById<T>) returns
            // null both when no content has ever been saved for this site
            // AND when Site.SiteTypeId isn't set yet - so this null-check
            // alone is the correct "not yet seeded" signal on a fresh site,
            // and stays the correct "already seeded / already Manager-edited,
            // never overwrite" signal on every later run once SiteTypeId has
            // been assigned below.
            var existing = await api.Sites.GetContentByIdAsync<SiteSettings>(site.Id);
            if (existing != null)
            {
                continue;
            }

            if (site.SiteTypeId != SiteSettingsTypeId)
            {
                site.SiteTypeId = SiteSettingsTypeId;
                await api.Sites.SaveAsync(site);
            }

            var settings = await api.Sites.CreateContentAsync<SiteSettings>();
            await api.Sites.SaveContentAsync(site.Id, settings);
        }
    }
}
