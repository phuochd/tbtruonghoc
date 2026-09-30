using Microsoft.AspNetCore.Mvc;
using Piranha;
using Piranha.AspNetCore.Services;
using Piranha.Models;
using TbTruongHoc.Web.Models;
using TbTruongHoc.Web.Services;

namespace TbTruongHoc.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class CmsController : Controller
{
    private readonly IApi _api;
    private readonly IModelLoader _loader;
    private readonly ProductCatalog _catalog;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="api">The current api</param>
    public CmsController(IApi api, IModelLoader loader, ProductCatalog catalog)
    {
        _api = api;
        _loader = loader;
        _catalog = catalog;
    }

    /// <summary>
    /// Gets the blog archive with the given id.
    /// </summary>
    /// <param name="id">The unique page id</param>
    /// <param name="year">The optional year</param>
    /// <param name="month">The optional month</param>
    /// <param name="page">The optional page</param>
    /// <param name="category">The optional category</param>
    /// <param name="tag">The optional tag</param>
    /// <param name="draft">If a draft is requested</param>
    [Route("archive")]
    public async Task<IActionResult> Archive(Guid id, int? year = null, int? month = null, int? page = null,
        Guid? category = null, Guid? tag = null, bool draft = false)
    {
        try
        {
            var model = await _loader.GetPageAsync<StandardArchive>(id, HttpContext.User, draft);
            model.Archive = await _api.Archives.GetByIdAsync<PostInfo>(id, page, category, tag, year, month);

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Gets the page with the given id.
    /// </summary>
    /// <param name="id">The unique page id</param>
    /// <param name="draft">If a draft is requested</param>
    [Route("page")]
    public async Task<IActionResult> Page(Guid id, bool draft = false)
    {
        try
        {
            var model = await _loader.GetPageAsync<StandardPage>(id, HttpContext.User, draft);

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Gets the post with the given id.
    /// </summary>
    /// <param name="id">The unique post id</param>
    /// <param name="draft">If a draft is requested</param>
    [Route("post")]
    public async Task<IActionResult> Post(Guid id, bool draft = false)
    {
        try
        {
            var model = await _loader.GetPostAsync<StandardPost>(id, HttpContext.User, draft);

            if (model.IsCommentsOpen)
            {
                model.Comments = await _api.Posts.GetAllCommentsAsync(model.Id, true);
            }
            return View(model);
        }
        catch
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Story 2.2: gets the product hub page (e.g. "Trống") with the given id,
    /// plus its category tiles built from its ProductArchive children.
    /// </summary>
    /// <param name="id">The unique page id</param>
    /// <param name="draft">If a draft is requested</param>
    [Route("producthub")]
    public async Task<IActionResult> ProductHub(Guid id, bool draft = false)
    {
        try
        {
            var model = await _loader.GetPageAsync<ProductHubPage>(id, HttpContext.User, draft);
            model.Tiles = await _catalog.GetHubTilesAsync(model.SiteId, model.Id);

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Story 2.2: gets the product archive (category) page with the given id
    /// and the requested page of its products.
    /// </summary>
    /// <param name="id">The unique page id</param>
    /// <param name="page">The optional archive page</param>
    /// <param name="draft">If a draft is requested</param>
    [Route("productarchive")]
    public async Task<IActionResult> ProductArchive(Guid id, int? page = null, bool draft = false)
    {
        try
        {
            var model = await _loader.GetPageAsync<ProductArchive>(id, HttpContext.User, draft);
            model.Archive = await _api.Archives.GetByIdAsync<ProductPost>(id, page, null, null, null, null,
                Models.ProductArchive.PageSize);

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Story 2.2: gets the product post with the given id.
    /// </summary>
    /// <param name="id">The unique post id</param>
    /// <param name="draft">If a draft is requested</param>
    [Route("productpost")]
    public async Task<IActionResult> ProductPost(Guid id, bool draft = false)
    {
        try
        {
            var model = await _loader.GetPostAsync<ProductPost>(id, HttpContext.User, draft);

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Story 5.1: gets the blog listing page with the given id and the
    /// requested page of its (published) posts.
    /// </summary>
    /// <param name="id">The unique page id</param>
    /// <param name="page">The optional archive page</param>
    /// <param name="draft">If a draft is requested</param>
    [Route("blogarchive")]
    public async Task<IActionResult> BlogArchive(Guid id, int? page = null, bool draft = false)
    {
        try
        {
            var model = await _loader.GetPageAsync<BlogArchive>(id, HttpContext.User, draft);
            if (model == null)
            {
                return NotFound();
            }
            model.Archive = await _api.Archives.GetByIdAsync<BlogPost>(id, page, null, null, null, null,
                Models.BlogArchive.PageSize);

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Story 5.1: gets the blog post with the given id.
    /// </summary>
    /// <param name="id">The unique post id</param>
    /// <param name="draft">If a draft is requested</param>
    [Route("blogpost")]
    public async Task<IActionResult> BlogPost(Guid id, bool draft = false)
    {
        try
        {
            var model = await _loader.GetPostAsync<BlogPost>(id, HttpContext.User, draft);
            // An unpublished post loads as null for visitors: 404, not a
            // null-model render error.
            if (model == null)
            {
                return NotFound();
            }

            // Story 5.2: parent archive for the breadcrumb / back-link, and
            // the related-posts strip.
            model.ParentArchive = await _api.Pages.GetByIdAsync<PageInfo>(model.BlogId);
            model.Related = await LoadRelatedPostsAsync(model);

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Story 5.2: posts from the same archive that share at least one tag
    /// with <paramref name="post"/>. Piranha's archive loader does the
    /// published filtering and newest-first ordering per tag; the per-tag
    /// results are merged, de-duplicated, stripped of the post itself and
    /// cut to <see cref="Models.BlogPost.MaxRelated"/>. No tags, no calls.
    /// </summary>
    private async Task<IReadOnlyList<BlogPost>> LoadRelatedPostsAsync(BlogPost post)
    {
        var tagIds = post.Tags?.Select(t => t.Id).Where(id => id != Guid.Empty).Distinct().ToList()
            ?? new List<Guid>();
        if (tagIds.Count == 0)
        {
            return Array.Empty<BlogPost>();
        }

        var candidates = new Dictionary<Guid, BlogPost>();
        foreach (var tagId in tagIds)
        {
            // One extra over the cap so the post itself can be dropped.
            var archive = await _api.Archives.GetByIdAsync<BlogPost>(post.BlogId, 1, null, tagId, null, null,
                Models.BlogPost.MaxRelated + 1);
            foreach (var candidate in archive?.Posts ?? new List<BlogPost>())
            {
                if (candidate.Id != post.Id)
                {
                    candidates.TryAdd(candidate.Id, candidate);
                }
            }
        }

        return candidates.Values
            .OrderByDescending(p => p.Published)
            .ThenBy(p => p.Id)
            .Take(Models.BlogPost.MaxRelated)
            .ToList();
    }

    /// <summary>
    /// Story 2.5: gets the craftsman story page with the given id.
    /// </summary>
    /// <param name="id">The unique page id</param>
    /// <param name="draft">If a draft is requested</param>
    [Route("craftsmanstory")]
    public async Task<IActionResult> CraftsmanStory(Guid id, bool draft = false)
    {
        try
        {
            var model = await _loader.GetPageAsync<CraftsmanStoryPage>(id, HttpContext.User, draft);

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Story 3.1: gets the paid-ads landing page with the given id.
    /// </summary>
    /// <param name="id">The unique page id</param>
    /// <param name="draft">If a draft is requested</param>
    [Route("landingpage")]
    public async Task<IActionResult> LandingPage(Guid id, bool draft = false)
    {
        try
        {
            var model = await _loader.GetPageAsync<LandingPage>(id, HttpContext.User, draft);

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Story 6.1: gets Site A's homepage (hero carousel/fallback) with the
    /// given id.
    /// </summary>
    /// <param name="id">The unique page id</param>
    /// <param name="draft">If a draft is requested</param>
    [Route("siteahomepage")]
    public async Task<IActionResult> SiteAHomePage(Guid id, bool draft = false)
    {
        try
        {
            var model = await _loader.GetPageAsync<SiteAHomePage>(id, HttpContext.User, draft);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Saves the given comment and then redirects to the post.
    /// </summary>
    /// <param name="id">The unique post id</param>
    /// <param name="commentModel">The comment model</param>
    [HttpPost]
    [Route("post/comment")]
    public async Task<IActionResult> SavePostComment(SaveCommentModel commentModel)
    {
        try
        {
            var model = await _loader.GetPostAsync<StandardPost>(commentModel.Id, HttpContext.User);

            // Create the comment
            var comment = new PostComment
            {
                IpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                UserAgent = Request.Headers.ContainsKey("User-Agent") ? Request.Headers["User-Agent"].ToString() : "",
                Author = commentModel.CommentAuthor,
                Email = commentModel.CommentEmail,
                Url = commentModel.CommentUrl,
                Body = commentModel.CommentBody
            };
            await _api.Posts.SaveCommentAndVerifyAsync(commentModel.Id, comment);

            return Redirect(model.Permalink + "#comments");
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
