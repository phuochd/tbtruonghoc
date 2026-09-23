#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piranha;
using Piranha.Manager;

namespace TbTruongHoc.Web.Areas.Manager.Pages;

/// <summary>
/// Story 1.5 (AD-3): the Manager-only "Danh sách khách để lại thông tin"
/// screen - a plain Razor Page rendered inside Piranha Manager's own
/// <c>_Layout.cshtml</c>, mirroring piranha.core v12.0's own
/// <c>AliasEdit.cshtml</c> / <c>AliasEditViewModel</c> pattern (same
/// <c>[Authorize(Policy = Permission.Admin)]</c> gate
/// <see cref="Controllers.LeadApiController"/> uses - see this story's Code
/// Map). All of the actual list/filter/detail behavior lives client-side in
/// <c>manager-leads.js</c> against that controller; this PageModel's only
/// job is (a) enforcing the page-level auth gate and (b) handing the view
/// the site list for the filter dropdown, resolved server-side via
/// <c>IApi.Sites.GetAllAsync()</c> - never hardcoded, never guessed
/// client-side from a site id.
/// </summary>
[Authorize(Policy = Permission.Admin)]
public class LeadsModel : PageModel
{
    private readonly IApi _api;

    public LeadsModel(IApi api)
    {
        _api = api;
    }

    public IReadOnlyList<SiteOption> Sites { get; private set; } = Array.Empty<SiteOption>();

    public async Task OnGetAsync()
    {
        var sites = await _api.Sites.GetAllAsync();
        Sites = sites
            .Select(s => new SiteOption(s.Id, s.Title))
            .ToList();
    }

    public record SiteOption(Guid Id, string Title);
}
