#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Piranha;
using Piranha.Manager;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Controllers;

/// <summary>
/// Story 1.5 (AD-3): the Manager-only, read-side API backing the "Danh sách
/// khách để lại thông tin" screen. Structurally mirrors piranha.core v12.0's
/// own <c>Piranha.Manager.Controllers.AliasApiController</c> (same
/// <c>[Area("Manager")]</c> / <c>manager/api/...</c> route convention, same
/// <c>Permission.Admin</c> policy AliasApiController itself is gated by) -
/// see this story's Code Map for the exact reference files. Unlike
/// AliasApiController, every action here shares the single
/// <see cref="Permission.Admin"/> policy at the class level: this app has no
/// custom roles/permissions (Boundaries &amp; Constraints - "Never introduce
/// a new Piranha permission/role"), so there is no finer-grained tier to
/// split actions across the way Alias/Edit/Delete are.
///
/// Deliberately read-only: only <see cref="List"/> and <see cref="Get"/>
/// exist - no create/update/delete action is ever added here (Boundaries -
/// "Never add edit/delete/export actions for leads").
/// </summary>
[Area("Manager")]
[Route("manager/api/lead")]
[Authorize(Policy = Permission.Admin)]
[ApiController]
public class LeadApiController : ControllerBase
{
    private readonly IApi _api;
    private readonly LeadDbContext _leadDb;

    public LeadApiController(IApi api, LeadDbContext leadDb)
    {
        _api = api;
        _leadDb = leadDb;
    }

    /// <summary>
    /// Lists every lead submission, optionally filtered to a single site,
    /// most recent first. Site names are resolved once per call via
    /// <c>IApi.Sites.GetAllAsync()</c> - never hardcoded, never left for the
    /// client to guess from <c>SiteId</c> - matching this story's Boundaries
    /// &amp; Constraints.
    /// </summary>
    /// <param name="siteId">Optional site id to filter by</param>
    [HttpGet]
    [Route("list/{siteId?}")]
    public async Task<ActionResult<IReadOnlyList<LeadListItemModel>>> List(Guid? siteId = null)
    {
        var sites = await _api.Sites.GetAllAsync();
        var siteNames = sites.ToDictionary(s => s.Id, s => s.Title);

        var query = _leadDb.FormSubmissions.AsNoTracking().AsQueryable();
        if (siteId.HasValue)
        {
            query = query.Where(s => s.SiteId == siteId.Value);
        }

        // Unconditional CreatedAt-descending order per this story's
        // Boundaries &amp; Constraints - most recent first, always, not just
        // when no other sort is requested (there is no other sort).
        var rows = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var result = rows
            .Select(row => new LeadListItemModel
            {
                Id = row.Id,
                SiteId = row.SiteId,
                SiteName = siteNames.TryGetValue(row.SiteId, out var name) ? name : null,
                FormType = row.FormType,
                Name = row.Name,
                Phone = row.Phone,
                CreatedAt = row.CreatedAt
            })
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Gets the full detail of a single lead submission, including
    /// <see cref="FormSubmission.LocationAddress"/> and
    /// <see cref="FormSubmission.IsOutsideServiceArea"/> when present (both
    /// serialize as explicit <c>null</c>, never omitted, when the submission
    /// is a general/non-survey one - see <see cref="LeadDetailModel"/>).
    /// </summary>
    /// <param name="id">The submission id</param>
    /// <returns>404 (empty body, no exception) when no row matches</returns>
    [HttpGet]
    [Route("{id:guid}")]
    public async Task<ActionResult<LeadDetailModel>> Get(Guid id)
    {
        var row = await _leadDb.FormSubmissions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);
        if (row == null)
        {
            return NotFound();
        }

        var site = await _api.Sites.GetByIdAsync(row.SiteId);

        return Ok(new LeadDetailModel
        {
            Id = row.Id,
            SiteId = row.SiteId,
            SiteName = site?.Title,
            FormType = row.FormType,
            Name = row.Name,
            Phone = row.Phone,
            ProductOfInterest = row.ProductOfInterest,
            Message = row.Message,
            LocationAddress = row.LocationAddress,
            IsOutsideServiceArea = row.IsOutsideServiceArea,
            CreatedAt = row.CreatedAt
        });
    }
}
