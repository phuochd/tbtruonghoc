using System.Reflection;
using Piranha.AttributeBuilder;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 2.2 (AD-2): a standalone hub page (e.g. "Trống") whose category-tile
/// grid is built at render time from its <see cref="ProductArchive"/>
/// children in the sitemap - never a hand-curated link list.
/// </summary>
[PageType(Title = "Trang tổng danh mục")]
[ContentTypeRoute(Title = "Default", Route = "/producthub")]
public class ProductHubPage : Page<ProductHubPage>
{
    /// <summary>
    /// The non-empty, visible subcategories, in sitemap order.
    /// </summary>
    public IReadOnlyList<CategoryTileModel> Tiles { get; set; } = Array.Empty<CategoryTileModel>();

    /// <summary>
    /// Story 6.1: this type's [PageType] title. Piranha's
    /// <see cref="SitemapItem.PageTypeName"/> carries the page type's
    /// <em>title</em> (not its id), so the nav can spot a hub from the
    /// sitemap alone, without loading each page.
    /// </summary>
    public static readonly string PageTypeTitle =
        typeof(ProductHubPage).GetCustomAttribute<PageTypeAttribute>()!.Title;

    /// <summary>True when the sitemap item is a <see cref="ProductHubPage"/>.</summary>
    public static bool IsHub(SitemapItem item) => item?.PageTypeName == PageTypeTitle;

    /// <summary>
    /// Story 6.2: the aggregate page's filter chips - every tile's groups,
    /// case-insensitively distinct, first spelling and first-seen order.
    /// </summary>
    public static IReadOnlyList<string> DistinctGroups(IEnumerable<CategoryTileModel> tiles)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return tiles.SelectMany(t => t.Groups).Where(g => seen.Add(GroupKey(g))).ToList();
    }

    /// <summary>
    /// Story 6.2: the key a filter chip and a tile's groups match on (and
    /// the one dedupe key): trimmed, NFC, lower-case invariant.
    /// </summary>
    public static string GroupKey(string group) =>
        group.Trim().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();
}

/// <summary>
/// One category-tile on a <see cref="ProductHubPage"/>. Story 6.2 adds the
/// category's id, primary image, filter groups and certifications (used by
/// Site A's homepage and aggregate page; Site B ignores them).
/// </summary>
public sealed record CategoryTileModel(string Title, string Excerpt, string Permalink)
{
    /// <summary>The category (archive) page's id.</summary>
    public Guid Id { get; init; }

    /// <summary>The category's primary image, or null when none is set.</summary>
    public ImageField Image { get; init; }

    /// <summary>Filter-chip groups (<see cref="ProductArchive.FilterGroupList"/>).</summary>
    public IReadOnlyList<string> Groups { get; init; } = Array.Empty<string>();

    /// <summary>Certification pills (<see cref="ProductArchive.CertificationList"/>).</summary>
    public IReadOnlyList<string> Certifications { get; init; } = Array.Empty<string>();

    /// <summary>True when <see cref="Image"/> is set and its media exists.</summary>
    public bool HasImage => Image != null && Image.HasValue && Image.Media != null;
}

/// <summary>
/// Story 2.2: input for <c>Views/Shared/_CategoryTile.cshtml</c> - the tile
/// plus its eyebrow (the hub's title).
/// </summary>
public sealed record CategoryTileViewModel(CategoryTileModel Tile, string Eyebrow);

/// <summary>
/// Story 6.2: input for <c>Views/Shared/_SiteACategoryTile.cshtml</c> - the
/// tile, its title's heading level, and whether certification pills show
/// (aggregate page only).
/// </summary>
public sealed record SiteACategoryTileViewModel(CategoryTileModel Tile, int HeadingLevel, bool ShowCertifications);
