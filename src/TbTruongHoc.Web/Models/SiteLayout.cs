#nullable enable

using Piranha;
using TbTruongHoc.Web.Data;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 2.1: picks the Razor layout for the current site, used by
/// <c>Views/_ViewStart.cshtml</c>. Site B (trongdoitam.net) gets its own
/// Mộc Trầm shell; Story 6.1 gives Site A (tbtruonghoc.com) its own shell too.
/// Any other/no site keeps the shared <c>_Layout</c> untouched.
/// Lives in C# rather than inline in _ViewStart because
/// <see cref="SiteSeed.TrongDoiTamInternalId"/> is internal and Razor runtime
/// compilation (enabled in Program.cs) compiles edited views into a separate
/// assembly that could not see it.
/// </summary>
public static class SiteLayout
{
    public const string Default = "_Layout";
    public const string TrongDoiTam = "_LayoutTrongDoiTam";

    /// <summary>Story 6.1: Site A's "Xanh Lục Bảo Rạng Rỡ" shell.</summary>
    public const string TbTruongHoc = "_LayoutTbTruongHoc";

    public static string ForInternalId(string? internalId) => internalId switch
    {
        SiteSeed.TrongDoiTamInternalId => TrongDoiTam,
        SiteSeed.TbTruongHocInternalId => TbTruongHoc,
        _ => Default,
    };

    /// <summary>
    /// Resolves the site through <see cref="IApi.Sites"/> (Piranha-cached),
    /// falling back to the shared layout when there is no current site.
    /// </summary>
    public static async Task<string> ForSiteAsync(IApi api, Guid siteId)
    {
        if (siteId == Guid.Empty)
        {
            return Default;
        }

        var site = await api.Sites.GetByIdAsync(siteId);
        return ForInternalId(site?.InternalId);
    }
}
