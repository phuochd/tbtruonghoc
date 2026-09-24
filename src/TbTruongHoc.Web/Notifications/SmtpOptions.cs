#nullable enable

namespace TbTruongHoc.Web.Notifications;

/// <summary>
/// Story 1.7: the single outbound SMTP configuration for every lead email
/// (AD-3 - never per form or per site). Bound from the <c>Smtp</c> config
/// section: user-secrets in dev, <c>Smtp__*</c> environment variables in
/// prod. <c>appsettings.json</c> ships it empty - no credential is ever
/// committed.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    /// <summary>Optional - leave blank for an unauthenticated relay.</summary>
    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? FromAddress { get; set; }

    public string? FromName { get; set; }

    /// <summary>True only when both <see cref="Host"/> and <see cref="FromAddress"/> are set.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}
