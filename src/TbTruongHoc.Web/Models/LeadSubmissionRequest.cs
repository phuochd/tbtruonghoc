#nullable enable

using System.ComponentModel.DataAnnotations;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 1.4 (FR-3): the JSON body <c>POST /api/leads</c> accepts. This is
/// the server-side validation source of truth (mirrored, not replaced, by
/// the client's own <c>required</c> attributes in
/// <c>_QuoteRequestForm.cshtml</c>) - a request missing Name or Phone is
/// rejected here regardless of what the client did or didn't check.
/// </summary>
public class LeadSubmissionRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(200, ErrorMessage = "Họ tên tối đa 200 ký tự.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(50, ErrorMessage = "Số điện thoại tối đa 50 ký tự.")]
    public string? Phone { get; set; }

    [StringLength(200, ErrorMessage = "Sản phẩm quan tâm tối đa 200 ký tự.")]
    public string? ProductOfInterest { get; set; }

    [StringLength(2000, ErrorMessage = "Lời nhắn tối đa 2000 ký tự.")]
    public string? Message { get; set; }

    /// <summary>
    /// Distinguishes this general contact/quote form ("general", the default
    /// when the client omits it) from later reuses of the same endpoint/table
    /// - e.g. Story 3.1's paid-ads landing page ("landing") and Story 6.5's
    /// survey form ("survey"). Never client-trusted for
    /// anything beyond this label; every other field/validation rule is
    /// identical regardless of value.
    /// </summary>
    public string? FormType { get; set; }

    /// <summary>
    /// Story 6.5 (FR-8): the survey form's "Địa điểm / địa chỉ lắp đặt".
    /// Required and limited to 500 characters only for a "survey"
    /// submission (<see cref="SurveyLocationAttribute"/>); every other form
    /// type ignores it and the controller never stores it for them.
    /// </summary>
    [SurveyLocation]
    public string? LocationAddress { get; set; }
}

/// <summary>
/// Story 6.5: a property-level check, so it runs in the same automatic
/// [ApiController] 400 as Name/Phone (an object-level or in-action check
/// would only surface after those pass - a second submit for the visitor).
/// Applies only when <see cref="LeadSubmissionRequest.FormType"/> is
/// "survey" (case-insensitive, the controller's own accepted spelling).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class SurveyLocationAttribute : ValidationAttribute
{
    /// <summary>The <c>FormSubmission.LocationAddress</c> column length.</summary>
    public const int MaxLength = 500;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is not LeadSubmissionRequest request
            || !string.Equals(request.FormType, "survey", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Success;
        }

        var memberNames = new[] { validationContext.MemberName ?? nameof(LeadSubmissionRequest.LocationAddress) };
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ValidationResult("Vui lòng nhập địa điểm / địa chỉ lắp đặt.", memberNames);
        }

        return text.Length > MaxLength
            ? new ValidationResult($"Địa điểm tối đa {MaxLength} ký tự.", memberNames)
            : ValidationResult.Success;
    }
}
