#nullable enable

using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// Story 6.3 (Q1): idempotent startup seed for Site A's catalog - a
/// published top-level "Sản phẩm" <see cref="ProductHubPage"/> (slug
/// <c>san-pham</c>, appended after the existing top-level pages) plus the 11
/// categories as DRAFT <see cref="ProductArchive"/> children
/// (<c>Published = null</c>), title + slug <c>san-pham/{slug}</c> only - the
/// client publishes each one once it has products and prose.
///
/// Runs only when Site A has none of the 12 seed slugs (hub + categories).
/// Once any of them exists the whole seed is skipped, so editor renames and
/// deletions always stick; existing pages are never modified.
/// </summary>
public static class SiteACatalogSeed
{
    internal const string HubSlug = "san-pham";
    internal const string HubTitle = "Sản phẩm";

    /// <summary>The 11 Site A categories (epic-6-context); slugs are the part after <see cref="HubSlug"/>.</summary>
    internal static readonly IReadOnlyList<(string Title, string Slug)> Categories = new[]
    {
        ("Dù che nắng sân trường", "du-che-nang-san-truong"),
        ("Nội thất mầm non", "noi-that-mam-non"),
        ("Quần áo nghi thức & cờ đội", "quan-ao-nghi-thuc-co-doi"),
        ("Thiết bị âm thanh – máy chiếu", "thiet-bi-am-thanh-may-chieu"),
        ("Bảng tương tác", "bang-tuong-tac"),
        ("Màn hình LED hiển thị", "man-hinh-led-hien-thi"),
        ("Thiết bị văn phòng", "thiet-bi-van-phong"),
        ("Phòng thí nghiệm Lý – Hóa – Sinh", "phong-thi-nghiem-ly-hoa-sinh"),
        ("Bàn thí nghiệm", "ban-thi-nghiem"),
        ("Thiết bị – đồ dùng dạy học", "thiet-bi-do-dung-day-hoc"),
        ("Thiết bị mầm non ngoài trời", "thiet-bi-mam-non-ngoai-troi"),
    };

    /// <summary>The full page slug of a category: <c>san-pham/{slug}</c>.</summary>
    internal static string CategorySlug(string slug) => $"{HubSlug}/{slug}";

    public static async Task EnsureSeededAsync(IApi api)
    {
        var site = await api.Sites.GetByInternalIdAsync(SiteSeed.TbTruongHocInternalId);
        if (site == null)
        {
            return;
        }

        await EnsureSeededAsync(api, site.Id);
    }

    /// <summary>
    /// Seeds the given site. Skipped when any of the 12 seed slugs already
    /// exists there.
    /// </summary>
    internal static async Task EnsureSeededAsync(IApi api, Guid siteId)
    {
        foreach (var slug in new[] { HubSlug }.Concat(Categories.Select(c => CategorySlug(c.Slug))))
        {
            if (await api.Pages.GetBySlugAsync<PageInfo>(slug, siteId) != null)
            {
                return;
            }
        }

        // Append after the existing top-level pages so "Trang chủ" stays the start page.
        var sitemap = await api.Sites.GetSitemapAsync(siteId, onlyPublished: false);

        var hub = await api.Pages.CreateAsync<ProductHubPage>();
        hub.SiteId = siteId;
        hub.ParentId = null;
        hub.SortOrder = sitemap.Count;
        hub.Title = HubTitle;
        hub.Slug = HubSlug;
        hub.Published = DateTime.Now;
        await api.Pages.SaveAsync(hub);

        for (var i = 0; i < Categories.Count; i++)
        {
            var (title, slug) = Categories[i];

            var archive = await api.Pages.CreateAsync<ProductArchive>();
            archive.SiteId = siteId;
            archive.ParentId = hub.Id;
            archive.SortOrder = i;
            archive.Title = title;
            archive.Slug = CategorySlug(slug);
            archive.Published = null;
            await api.Pages.SaveAsync(archive);
        }
    }
}
