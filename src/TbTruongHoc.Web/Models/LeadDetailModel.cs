#nullable enable

using System;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 1.5 (AD-3): full detail of a single lead submission - the wire shape
/// <see cref="Controllers.LeadApiController"/>'s <c>GET manager/api/lead/{id}</c>
/// returns. Mirrors every field on <see cref="FormSubmission"/> plus a
/// server-resolved <see cref="SiteName"/> (never guessed client-side from
/// <see cref="SiteId"/>). The nullable fields
/// (<see cref="ProductOfInterest"/>, <see cref="Message"/>,
/// <see cref="LocationAddress"/>, <see cref="IsOutsideServiceArea"/>) are
/// always present in the serialized JSON response, even when null - System.Text.Json's
/// default behavior serializes null properties rather than omitting them, which
/// is exactly what the I/O &amp; Edge-Case Matrix requires for a general
/// submission's null <see cref="LocationAddress"/>/<see cref="IsOutsideServiceArea"/>.
/// </summary>
public class LeadDetailModel
{
    public Guid Id { get; set; }

    public Guid SiteId { get; set; }

    public string? SiteName { get; set; }

    public string FormType { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? ProductOfInterest { get; set; }

    public string? Message { get; set; }

    public string? LocationAddress { get; set; }

    public bool? IsOutsideServiceArea { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
