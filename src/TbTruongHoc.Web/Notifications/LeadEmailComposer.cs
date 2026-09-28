#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Notifications;

/// <summary>
/// Story 1.7: builds the plain-text Vietnamese lead email. Pure function -
/// no I/O, no clock - so every formatting rule is unit-testable directly.
/// </summary>
public static class LeadEmailComposer
{
    /// <summary>Vietnam has no DST, so a fixed UTC+7 offset is exact and needs no time-zone database.</summary>
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public const string TimestampFormat = "dd/MM/yyyy HH:mm";

    private const string FallbackSiteTitle = "Website";

    public static NotificationEmail Compose(FormSubmission submission, string? siteTitle, IReadOnlyList<string> recipients)
    {
        var title = SanitizeForHeader(siteTitle);
        if (title.Length == 0)
        {
            title = FallbackSiteTitle;
        }

        var subject = $"[{title}] Khách mới: {SanitizeForHeader(submission.Name)} – {SanitizeForHeader(submission.Phone)}";

        var body = new StringBuilder();
        body.AppendLine($"Có khách mới để lại thông tin trên {title}.");
        body.AppendLine();
        body.AppendLine($"Website: {title}");
        body.AppendLine($"Loại form: {FormTypeLabel(submission.FormType)}");
        body.AppendLine($"Thời gian: {FormatVietnamTime(submission.CreatedAt)} (giờ Việt Nam)");
        body.AppendLine($"Họ tên: {SanitizeForHeader(submission.Name)}");
        body.AppendLine($"Số điện thoại: {SanitizeForHeader(submission.Phone)}");
        body.AppendLine($"Sản phẩm quan tâm: {OrNone(submission.ProductOfInterest)}");
        body.AppendLine($"Lời nhắn: {OrNone(submission.Message)}");

        if (!string.IsNullOrWhiteSpace(submission.LocationAddress))
        {
            body.AppendLine($"Địa chỉ: {submission.LocationAddress}");
        }

        if (submission.IsOutsideServiceArea.HasValue)
        {
            body.AppendLine(submission.IsOutsideServiceArea.Value
                ? "Khu vực phục vụ: Ngoài khu vực phục vụ"
                : "Khu vực phục vụ: Trong khu vực phục vụ");
        }

        body.AppendLine();
        body.AppendLine("Xem đầy đủ trong Piranha Manager > Danh sách khách để lại thông tin.");

        return new NotificationEmail(recipients, subject, body.ToString());
    }

    public static string FormatVietnamTime(DateTimeOffset value) =>
        value.ToOffset(VietnamOffset).ToString(TimestampFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// Replaces CR/LF, every other control character and every Unicode
    /// whitespace character (tab, NBSP, U+2028/U+2029...) with a space, then collapses the
    /// result to a single trimmed line - visitor input must never be able to
    /// break the subject header.
    /// </summary>
    public static string SanitizeForHeader(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var cleaned = new string(value.Select(c => char.IsControl(c) || char.IsWhiteSpace(c) ? ' ' : c).ToArray());
        return string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string FormTypeLabel(string? formType) => formType switch
    {
        "general" => "Liên hệ / báo giá",
        "survey" => "Khảo sát",
        LandingPage.FormType => "Landing page quảng cáo",
        _ => formType ?? string.Empty
    };

    private static string OrNone(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "(không có)" : value;
}
