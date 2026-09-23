#nullable enable

using System;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 1.5 (AD-3): one row of the Manager "Danh sách khách để lại thông
/// tin" list screen - the wire shape <see cref="Controllers.LeadApiController"/>'s
/// <c>GET manager/api/lead/list/{siteId?}</c> returns. Deliberately narrower
/// than <see cref="FormSubmission"/> (the list view only needs enough to
/// triage a row; full detail is a separate fetch) and carries a
/// server-resolved <see cref="SiteName"/> - per the spec's Boundaries &amp;
/// Constraints, site names are always resolved server-side via
/// <c>IApi.Sites.GetAllAsync()</c>, never guessed client-side from
/// <see cref="SiteId"/>.
/// </summary>
public class LeadListItemModel
{
    public Guid Id { get; set; }

    public Guid SiteId { get; set; }

    public string? SiteName { get; set; }

    public string FormType { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
