using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Piranha;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Test-only probe for Story 2.1's "Chrome hidden" matrix row. No production
/// page type sets <c>ViewData["HideSiteChrome"]</c> yet (Epic 3's landing
/// page will), so this controller sets it and renders the real
/// <c>Cms/Page.cshtml</c> through the normal <c>_ViewStart</c> layout
/// selection. Registered as an MVC application part only by
/// <see cref="PiranhaWebApplicationFactory"/>; never part of the app itself.
/// </summary>
public class HideChromeProbeController : Controller
{
    public const string Path = "/__test/hide-chrome-probe";

    private readonly IApi _api;

    public HideChromeProbeController(IApi api)
    {
        _api = api;
    }

    [HttpGet(Path)]
    public async Task<IActionResult> Index()
    {
        var model = await _api.Pages.CreateAsync<StandardPage>();
        model.Title = "Hide chrome probe";

        ViewData["HideSiteChrome"] = true;
        return View("~/Views/Cms/Page.cshtml", model);
    }
}
