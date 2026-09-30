#nullable enable

using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// DEV-ONLY sample data for Site A, so the Story 6.1 shell can be looked at
/// locally: a published top-level "Sản phẩm" <see cref="ProductHubPage"/>
/// with the 11 Site A categories as published <see cref="ProductArchive"/>
/// children, each with 1-2 published <see cref="ProductPost"/>s - except the
/// last category, left empty so the empty-category hide rule is visible
/// (dropdown shows 10 rows, "Xem tất cả 10 nhóm sản phẩm →"). Blank
/// SiteSettings contact fields get sample values; filled ones are kept.
///
/// Opt-in only: runs when the environment is Development AND the config key
/// <see cref="ConfigKey"/> is true (set by the launchSettings profile, so
/// `dotnet run` gets it but the test host and production never do).
/// Skipped entirely once a page with slug <see cref="HubSlug"/> exists on
/// Site A. Titles, slugs and products are placeholders, not real content.
/// </summary>
public static class SiteASampleSeed
{
    public const string ConfigKey = "DevSamples:SiteA";

    internal const string HubSlug = "san-pham";
    internal const string HubTitle = "Sản phẩm";
    internal const string SampleCategory = "Mẫu";

    /// <summary>
    /// The 11 categories (epic-6-context) with placeholder products; the last
    /// one has none on purpose.
    /// </summary>
    internal static readonly IReadOnlyList<(string Title, string Slug, string[] Products)> Categories = new[]
    {
        ("Dù che nắng sân trường", "du-che-nang-san-truong", new[] { "Dù lệch tâm 3m (mẫu)", "Dù tâm vuông 4m (mẫu)" }),
        ("Nội thất mầm non", "noi-that-mam-non", new[] { "Bàn ghế mầm non (mẫu)" }),
        ("Quần áo nghi thức & cờ đội", "quan-ao-nghi-thuc-co-doi", new[] { "Bộ đồng phục nghi thức Đội (mẫu)", "Cờ Đội (mẫu)" }),
        ("Thiết bị âm thanh – máy chiếu", "thiet-bi-am-thanh-may-chieu", new[] { "Loa hội trường (mẫu)" }),
        ("Bảng tương tác", "bang-tuong-tac", new[] { "Bảng tương tác 86 inch (mẫu)" }),
        ("Màn hình LED hiển thị", "man-hinh-led-hien-thi", new[] { "Màn hình LED P3 trong nhà (mẫu)" }),
        ("Thiết bị văn phòng", "thiet-bi-van-phong", new[] { "Máy in văn phòng (mẫu)" }),
        ("Phòng thí nghiệm Lý – Hóa – Sinh", "phong-thi-nghiem-ly-hoa-sinh", new[] { "Phòng thí nghiệm Hóa (mẫu)" }),
        ("Bàn thí nghiệm", "ban-thi-nghiem", new[] { "Bàn thí nghiệm trung tâm (mẫu)" }),
        ("Thiết bị – đồ dùng dạy học", "thiet-bi-do-dung-day-hoc", new[] { "Bộ đồ dùng dạy Toán lớp 1 (mẫu)" }),
        ("Thiết bị mầm non ngoài trời", "thiet-bi-mam-non-ngoai-troi", Array.Empty<string>()),
    };

    public static async Task EnsureSeededAsync(IApi api)
    {
        var site = await api.Sites.GetByInternalIdAsync(SiteSeed.TbTruongHocInternalId);
        if (site == null)
        {
            return;
        }

        await EnsureSeededAsync(api, site.Id);
    }

    internal static async Task EnsureSeededAsync(IApi api, Guid siteId)
    {
        await FillBlankContactsAsync(api, siteId);

        if (await api.Pages.GetBySlugAsync<PageInfo>(HubSlug, siteId) != null)
        {
            return;
        }

        // Append after the existing top-level pages so "Trang chủ" stays the start page.
        var sitemap = await api.Sites.GetSitemapAsync(siteId, onlyPublished: false);
        var published = DateTime.Now.AddMinutes(-1);

        var hub = await api.Pages.CreateAsync<ProductHubPage>();
        hub.SiteId = siteId;
        hub.ParentId = null;
        hub.SortOrder = sitemap.Count;
        hub.Title = HubTitle;
        hub.Slug = HubSlug;
        hub.Published = published;
        await api.Pages.SaveAsync(hub);

        for (var i = 0; i < Categories.Count; i++)
        {
            var (title, slug, products) = Categories[i];

            var archive = await api.Pages.CreateAsync<ProductArchive>();
            archive.SiteId = siteId;
            archive.ParentId = hub.Id;
            archive.SortOrder = i;
            archive.Title = title;
            archive.Slug = $"{HubSlug}/{slug}";
            archive.Published = published;
            await api.Pages.SaveAsync(archive);

            for (var p = 0; p < products.Length; p++)
            {
                var post = await api.Posts.CreateAsync<ProductPost>();
                post.BlogId = archive.Id;
                post.Category = SampleCategory;
                post.Title = products[p];
                post.Sku.Value = $"MAU-{i + 1:00}{p + 1}";
                // One priced product per pair, the rest "Liên hệ báo giá".
                post.Price.Value = p == 1 ? "từ 2.500.000đ" : null;
                post.Published = published;
                await api.Posts.SaveAsync(post);
            }
        }
    }

    /// <summary>Sample chip/footer contacts, only into fields that are blank.</summary>
    private static async Task FillBlankContactsAsync(IApi api, Guid siteId)
    {
        var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(siteId);
        if (settings == null)
        {
            return;
        }

        var changed = false;
        void Fill(Piranha.Extend.Fields.StringField? field, string value)
        {
            if (field != null && string.IsNullOrWhiteSpace(field.Value))
            {
                field.Value = value;
                changed = true;
            }
        }

        Fill(settings.Phone, "0900 000 000");
        Fill(settings.ZaloUrl, "https://zalo.me/0900000000");
        Fill(settings.Email, "lienhe@example.vn");
        Fill(settings.Address, "123 Đường Mẫu, Quận 1, TP. Hồ Chí Minh");
        Fill(settings.MapsUrl, "https://maps.google.com/?q=Quan+1+Ho+Chi+Minh");

        if (changed)
        {
            await api.Sites.SaveContentAsync(siteId, settings);
        }
    }
}
