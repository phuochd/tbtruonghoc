#nullable enable

using System.Text.RegularExpressions;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Single source of the render-time sanitizing rules for
/// <see cref="SiteSettings"/>'s contact fields (Story 1.3's rules, extracted
/// by Story 2.1 so Site A's <c>_ContactBlock</c> and Site B's nav hotline /
/// sticky contact bar / footer never drift apart). A <c>null</c> result means
/// "treat as unset": the caller omits the element instead of rendering a
/// dead or unsafe link.
/// </summary>
public static class ContactLinks
{
    /// <summary>
    /// Fewer digits than any real phone number can have (e.g. "N/A", or
    /// garbage text that only incidentally contains a stray digit) is
    /// treated as unset rather than rendered as a nonsensical "tel:" link.
    /// </summary>
    public const int MinPhoneDigits = 7;

    private static readonly Regex NonDigits = new("[^0-9]", RegexOptions.Compiled);

    /// <summary>
    /// The value for a <c>tel:</c> href (without the scheme): digits only,
    /// plus a leading "+" when the editor entered an internationally
    /// formatted number ("+84 90 123 4567" -> "+84901234567") - the dialing
    /// prefix is never stripped. <c>null</c> when unset or too short.
    /// </summary>
    public static string? TelHref(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digitsOnly = NonDigits.Replace(phone, "");
        if (digitsOnly.Length < MinPhoneDigits)
        {
            return null;
        }

        return (phone.TrimStart().StartsWith('+') ? "+" : "") + digitsOnly;
    }

    /// <summary>
    /// Zalo/Maps must be an absolute http(s) URL - a "javascript:"/"data:"
    /// value must never execute on click, and a relative/malformed value
    /// must never render a broken link. Returns the URL unchanged when safe
    /// (never rewritten), otherwise <c>null</c>.
    /// </summary>
    public static string? SafeUrl(string? url) =>
        url != null && SiteSettingsValidation.IsSafeAbsoluteUrl(url) ? url : null;
}
