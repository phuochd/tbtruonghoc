using Piranha.AttributeBuilder;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 5.1 (FR-14): the blog listing - an archive page whose items are
/// <see cref="BlogPost"/>s, rendered as a paginated blog-card grid.
/// Site-agnostic: Site B's "Blog" page is seeded by <c>BlogSeed</c>;
/// Epic 7 reuses the same type on Site A.
/// </summary>
[PageType(Title = "Blog", IsArchive = true)]
[ContentTypeRoute(Title = "Default", Route = "/blogarchive")]
[PageTypeArchiveItem(typeof(BlogPost))]
public class BlogArchive : Page<BlogArchive>
{
    /// <summary>Cards per archive page.</summary>
    public const int PageSize = 12;

    /// <summary>
    /// The currently loaded page of posts.
    /// </summary>
    public PostArchive<BlogPost> Archive { get; set; }
}
