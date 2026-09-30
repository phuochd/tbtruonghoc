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
/// The hub and its categories are created only when no page with slug
/// <see cref="HubSlug"/> exists on Site A. Titles, slugs and products are
/// placeholders, not real content.
///
/// Story 6.2: on every run (like the contacts), blank excerpts / "Nhóm lọc"
/// / "Chứng nhận" of the sample categories and an empty homepage trust band
/// get sample values - so an existing dev DB shows the aggregate
/// search/chips and the band too. Filled values are kept. Certifications
/// and stats (trust claims) are marked "(mẫu)"; excerpts and groups are
/// neutral sample copy.
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

    /// <summary>
    /// Story 6.2: sample excerpt, filter groups and certifications per
    /// category slug (dev only; certifications are marked "(mẫu)").
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, (string Excerpt, string Groups, string Certifications)> CategoryDetails =
        new Dictionary<string, (string, string, string)>
        {
            ["du-che-nang-san-truong"] = ("Dù che, mái che di động cho sân chơi và khu vực tập thể dục ngoài trời.", "Ngoài trời", ""),
            ["noi-that-mam-non"] = ("Bàn ghế, tủ kệ, giường ngủ trưa dành riêng cho lớp học mầm non.", "Mầm non, Nội thất", "CARB P2 (mẫu), ASTM (mẫu)"),
            ["quan-ao-nghi-thuc-co-doi"] = ("Trang phục nghi thức, cờ đội, phụ kiện phục vụ lễ chào cờ.", "Nghi thức & sự kiện", ""),
            ["thiet-bi-am-thanh-may-chieu"] = ("Loa, micro, máy chiếu phục vụ giảng dạy và hội trường.", "Thiết bị công nghệ, Nghi thức & sự kiện", ""),
            ["bang-tuong-tac"] = ("Bảng thông minh tương tác cho lớp học ứng dụng công nghệ.", "Thiết bị công nghệ", ""),
            ["man-hinh-led-hien-thi"] = ("Màn hình LED trong nhà và ngoài trời cho sự kiện, thông báo.", "Thiết bị công nghệ, Nghi thức & sự kiện", ""),
            ["thiet-bi-van-phong"] = ("Bàn ghế, tủ hồ sơ, thiết bị văn phòng cho khối hành chính nhà trường.", "Nội thất", ""),
            ["phong-thi-nghiem-ly-hoa-sinh"] = ("Dụng cụ, mô hình thí nghiệm Vật lý – Hóa học – Sinh học.", "Phòng học bộ môn", ""),
            ["ban-thi-nghiem"] = ("Bàn thí nghiệm chuyên dụng, mặt chịu hóa chất và chịu nhiệt.", "Phòng học bộ môn, Nội thất", ""),
            ["thiet-bi-do-dung-day-hoc"] = ("Đồ dùng trực quan, học cụ hỗ trợ giảng dạy các môn học.", "Phòng học bộ môn", ""),
            ["thiet-bi-mam-non-ngoai-troi"] = ("Đồ chơi vận động, thiết bị sân chơi ngoài trời cho trẻ mầm non.", "Mầm non, Ngoài trời", "ASTM (mẫu)"),
        };

    /// <summary>Story 6.2: sample homepage trust stats (dev only, marked "(mẫu)").</summary>
    internal static readonly IReadOnlyList<(string Number, string Label)> SampleStats = new[]
    {
        ("500+", "Trường đã lắp đặt (mẫu)"),
        ("20 năm", "Kinh nghiệm ngành (mẫu)"),
        ("100%", "Bảo hành chính hãng (mẫu)"),
        ("24/7", "Hỗ trợ kỹ thuật (mẫu)"),
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

        if (await api.Pages.GetBySlugAsync<PageInfo>(HubSlug, siteId) == null)
        {
            await CreateHubAsync(api, siteId);
        }

        await FillBlankCategoryDetailsAsync(api, siteId);
        await FillEmptyTrustStatsAsync(api, siteId);
    }

    private static async Task CreateHubAsync(IApi api, Guid siteId)
    {
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

    /// <summary>Story 6.2: sample category details, only into blank fields.</summary>
    private static async Task FillBlankCategoryDetailsAsync(IApi api, Guid siteId)
    {
        foreach (var (slug, details) in CategoryDetails)
        {
            var archive = await api.Pages.GetBySlugAsync<ProductArchive>($"{HubSlug}/{slug}", siteId);
            if (archive == null)
            {
                continue;
            }

            var changed = false;
            if (string.IsNullOrWhiteSpace(archive.Excerpt))
            {
                archive.Excerpt = details.Excerpt;
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(archive.FilterGroups?.Value) && details.Groups.Length > 0)
            {
                archive.FilterGroups = details.Groups;
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(archive.Certifications?.Value) && details.Certifications.Length > 0)
            {
                archive.Certifications = details.Certifications;
                changed = true;
            }

            if (changed)
            {
                await api.Pages.SaveAsync(archive);
            }
        }
    }

    /// <summary>Story 6.2: sample stats on the start page, only when its band is empty.</summary>
    private static async Task FillEmptyTrustStatsAsync(IApi api, Guid siteId)
    {
        var start = await api.Pages.GetStartpageAsync<PageInfo>(siteId);
        if (start == null || start.TypeId != nameof(SiteAHomePage))
        {
            return;
        }

        var home = await api.Pages.GetByIdAsync<SiteAHomePage>(start.Id);
        if (home == null || home.TrustStats.Count > 0)
        {
            return;
        }

        foreach (var (number, label) in SampleStats)
        {
            home.TrustStats.Add(new TrustStat { Number = number, Label = label });
        }
        await api.Pages.SaveAsync(home);
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
