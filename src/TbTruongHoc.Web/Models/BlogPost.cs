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
}
