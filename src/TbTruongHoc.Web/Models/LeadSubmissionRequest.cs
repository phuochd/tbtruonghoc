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
    /// - e.g. Story 6.5's survey form ("survey"). Never client-trusted for
    /// anything beyond this label; every other field/validation rule is
    /// identical regardless of value.
    /// </summary>
    public string? FormType { get; set; }
}
