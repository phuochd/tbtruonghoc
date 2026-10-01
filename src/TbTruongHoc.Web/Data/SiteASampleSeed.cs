#nullable enable

using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// DEV-ONLY sample data for Site A, so the Story 6.1+ pages can be looked at
/// locally. Story 6.3: the "Sản phẩm" hub and the 11 categories come from the
/// production <see cref="SiteACatalogSeed"/> (same titles/slugs; drafts);
/// this seed publishes every sample category that is still a draft with no
/// posts and adds 1-2 published placeholder <see cref="ProductPost"/>s to
/// it - except the last category, published but left empty so the
/// empty-category hide rule is visible (dropdown shows 10 rows, "Xem tất cả
/// 10 nhóm sản phẩm →"). An existing dev DB (categories already published
/// with products) is left as is. Blank SiteSettings contact fields get
/// sample values; filled ones are kept.
///
/// Opt-in only: runs when the environment is Development AND the config key
/// <see cref="ConfigKey"/> is true (set by the launchSettings profile, so
/// `dotnet run` gets it but the test host and production never do).
/// Category titles/slugs are the production ones (<see cref="SiteACatalogSeed"/>);
/// only the products (and sample details/contacts) are placeholders.
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

    internal const string HubSlug = SiteACatalogSeed.HubSlug;
    internal const string SampleCategory = "Mẫu";

    /// <summary>
    /// Placeholder products per category slug (<see cref="SiteACatalogSeed.Categories"/>);
    /// the last category has none on purpose.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string[]> Products = new Dictionary<string, string[]>
    {
        ["du-che-nang-san-truong"] = new[] { "Dù lệch tâm 3m (mẫu)", "Dù tâm vuông 4m (mẫu)" },
        ["noi-that-mam-non"] = new[] { "Bàn ghế mầm non (mẫu)" },
        ["quan-ao-nghi-thuc-co-doi"] = new[] { "Bộ đồng phục nghi thức Đội (mẫu)", "Cờ Đội (mẫu)" },
        ["thiet-bi-am-thanh-may-chieu"] = new[] { "Loa hội trường (mẫu)" },
        ["bang-tuong-tac"] = new[] { "Bảng tương tác 86 inch (mẫu)" },
        ["man-hinh-led-hien-thi"] = new[] { "Màn hình LED P3 trong nhà (mẫu)" },
        ["thiet-bi-van-phong"] = new[] { "Máy in văn phòng (mẫu)" },
        ["phong-thi-nghiem-ly-hoa-sinh"] = new[] { "Phòng thí nghiệm Hóa (mẫu)" },
        ["ban-thi-nghiem"] = new[] { "Bàn thí nghiệm trung tâm (mẫu)" },
        ["thiet-bi-do-dung-day-hoc"] = new[] { "Bộ đồ dùng dạy Toán lớp 1 (mẫu)" },
        ["thiet-bi-mam-non-ngoai-troi"] = Array.Empty<string>(),
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

        // Story 6.3: the hub + draft categories are the production seed's
        // (a no-op once any of its slugs exists).
        await SiteACatalogSeed.EnsureSeededAsync(api, siteId);
        if (await api.Pages.GetBySlugAsync<PageInfo>(HubSlug, siteId) != null)
        {
            await PublishDraftSampleCategoriesAsync(api, siteId);
        }

        await FillBlankCategoryDetailsAsync(api, siteId);
        await FillEmptyTrustStatsAsync(api, siteId);
    }

    /// <summary>
    /// Story 6.3: publishes each sample category that is still a draft with
    /// no posts and adds its sample products. Published or post-bearing
    /// categories (an existing dev DB, editor work) are left untouched.
    /// </summary>
    private static async Task PublishDraftSampleCategoriesAsync(IApi api, Guid siteId)
    {
        var published = DateTime.Now.AddMinutes(-1);

        for (var i = 0; i < SiteACatalogSeed.Categories.Count; i++)
        {
            var slug = SiteACatalogSeed.Categories[i].Slug;
            var archive = await api.Pages.GetBySlugAsync<ProductArchive>(SiteACatalogSeed.CategorySlug(slug), siteId);
            if (archive == null || archive.Published.HasValue || (await api.Posts.GetAllAsync<PostInfo>(archive.Id)).Any())
            {
                continue;
            }

            archive.Published = published;
            await api.Pages.SaveAsync(archive);

            var products = Products.TryGetValue(slug, out var list) ? list : Array.Empty<string>();
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
