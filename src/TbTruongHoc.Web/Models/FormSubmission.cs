#nullable enable

using System;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 1.4 (FR-3): a single lead-capture submission (contact/quote form
/// today; the survey form from Story 6.5 reuses this same table with
/// <see cref="FormType"/> = "survey"). Persisted through the standalone
/// <c>LeadDbContext</c> - deliberately not a Piranha content type and not
/// Piranha's built-in Comment/PostComment model (AD-3) - so it is queryable
/// by Story 1.5's future Manager module independent of Piranha's own content
/// pipeline.
///
/// <see cref="LocationAddress"/> and <see cref="IsOutsideServiceArea"/> are
/// reserved, nullable columns for the later survey-form-modal work
/// (UX-DR8) - this story never populates them, but the schema exists now so
/// that later story doesn't need its own migration to add them.
/// </summary>
public class FormSubmission
{
    public Guid Id { get; set; }

    public Guid SiteId { get; set; }

    public string FormType { get; set; } = "general";

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? ProductOfInterest { get; set; }

    public string? Message { get; set; }

    public string? LocationAddress { get; set; }

    public bool? IsOutsideServiceArea { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
