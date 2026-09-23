#nullable enable

using Microsoft.Extensions.Configuration;
using Piranha;
using Piranha.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// Idempotent startup seed that guarantees the two Piranha <see cref="Site"/>
/// records required by Story 1.1 exist: "tbtruonghoc" (default) and
/// "trongdoitam.net" (non-default). Never creates a third site and never
/// re-derives/updates a site record that already exists, so re-running
/// startup produces no duplicates and does not clobber Manager-edited
/// hostnames on an existing site.
/// </summary>
public static class SiteSeed
{
    internal const string TbTruongHocInternalId = "tbtruonghoc";
    internal const string TrongDoiTamInternalId = "trongdoitam-net";

    /// <summary>
    /// The InternalId Piranha's own EF Core bootstrap (see
    /// Piranha.Data.EF's internal Db&lt;T&gt;.Seed()) gives the single
    /// placeholder Site it auto-creates the very first time the database
    /// is touched on a brand new install.
    /// </summary>
    private const string FrameworkBootstrapInternalId = "Default";

    public static async Task EnsureSeededAsync(IApi api, IConfiguration configuration)
    {
        var tbTruongHocHostnames = configuration["Sites:TbTruongHoc:Hostnames"];
        var trongDoiTamHostnames = configuration["Sites:TrongDoiTam:Hostnames"];

        var tbTruongHoc = await api.Sites.GetByInternalIdAsync(TbTruongHocInternalId);
        if (tbTruongHoc == null)
        {
            // On a genuinely empty database, Piranha's own framework
            // bootstrap already inserted one placeholder Site ("Default")
            // before this seed step ever runs. Repurpose that row into
            // "tbtruonghoc" instead of leaving it behind as an unwanted
            // third Site record.
            var bootstrapSite = await api.Sites.GetByInternalIdAsync(FrameworkBootstrapInternalId);
            if (bootstrapSite != null)
            {
                bootstrapSite.InternalId = TbTruongHocInternalId;
                bootstrapSite.Title = "TB Truong Hoc";
                bootstrapSite.Hostnames = tbTruongHocHostnames;
                bootstrapSite.IsDefault = true;

                await api.Sites.SaveAsync(bootstrapSite);
            }
            else
            {
                await CreateSiteAsync(api, TbTruongHocInternalId, "TB Truong Hoc", tbTruongHocHostnames, isDefault: true);
            }
        }

        await EnsureSiteAsync(api, TrongDoiTamInternalId, "Trong Doi Tam", trongDoiTamHostnames, isDefault: false);
    }

    private static async Task EnsureSiteAsync(IApi api, string internalId, string title, string? hostnames, bool isDefault)
    {
        var existing = await api.Sites.GetByInternalIdAsync(internalId);
        if (existing != null)
        {
            // Site already seeded on a previous run - never re-derive it.
            return;
        }

        await CreateSiteAsync(api, internalId, title, hostnames, isDefault);
    }

    private static async Task CreateSiteAsync(IApi api, string internalId, string title, string? hostnames, bool isDefault)
    {
        var site = new Site
        {
            Id = Guid.NewGuid(),
            InternalId = internalId,
            Title = title,
            Hostnames = hostnames,
            IsDefault = isDefault
        };

        await api.Sites.SaveAsync(site);
    }
}
