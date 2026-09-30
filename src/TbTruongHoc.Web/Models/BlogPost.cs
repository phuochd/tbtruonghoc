using Piranha.AttributeBuilder;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 5.1 (FR-14): one blog article, a child of a <see cref="BlogArchive"/>.
/// The body is the post's blocks; title, excerpt, primary image and the SEO
/// fields are Piranha's built-ins. No extra regions.
/// </summary>
[PostType(Title = "Bài viết")]
[ContentTypeRoute(Title = "Default", Route = "/blogpost")]
public class BlogPost : Post<BlogPost>
{
    /// <summary>Story 5.2: most related posts shown at the end of an article.</summary>
    public const int MaxRelated = 3;

    /// <summary>
    /// Story 5.2: the parent archive (title + permalink) for the breadcrumb
    /// and the back-link. Loaded by the controller, not persisted.
    /// </summary>
    public PageInfo ParentArchive { get; set; }

    /// <summary>
    /// Story 5.2: published posts from the same archive sharing at least one
    /// tag with this one, newest first, at most <see cref="MaxRelated"/>.
    /// Loaded by the controller, not persisted.
    /// </summary>
    public IReadOnlyList<BlogPost> Related { get; set; } = Array.Empty<BlogPost>();
}
