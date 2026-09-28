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
