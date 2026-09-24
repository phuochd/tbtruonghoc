#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Shared safety checks for <see cref="SiteSettings"/>'s free-text fields.
/// Used both to filter what the rendering partials emit and, as defense in
/// depth, to reject unsafe values at save time via the
/// <c>App.Hooks.SiteContent.RegisterOnBeforeSave</c> hook wired in
/// Program.cs - so a future view that renders these fields directly can't
/// reintroduce the same injection risk (e.g. a Manager-entered
/// "javascript:"/"data:" URL, or a GA4 ID/verification value crafted to
/// break out of the inline &lt;script&gt;/attribute _Analytics.cshtml
/// embeds it in).
/// </summary>
public static class SiteSettingsValidation
{
    // Google's own GA4 measurement ID format: "G-" followed by alphanumeric
    // characters (real IDs are 10 characters long, e.g. G-XXXXXXXXXX). The
    // {4,} minimum only guards against an obviously truncated/mistyped
    // paste (e.g. "G-X"); it deliberately has no upper bound in case
    // Google's own format ever changes length. Anchoring the whole value
    // (not just a prefix match) means anything with extra characters -
    // including a quote/angle-bracket injection attempt - fails validation
    // rather than being partially accepted.
    private static readonly Regex Ga4MeasurementIdPattern = new("^G-[A-Za-z0-9]{4,}$", RegexOptions.Compiled);

    // Search Console's HTML-tag verification content value is an opaque
    // token; Google does not publish a strict grammar for it. This only
    // needs to exclude the characters that could break out of the
    // "content=\"...\"" attribute it's rendered into (a quote, an angle
    // bracket, or whitespace) - Razor's default @-expression HTML-encoding
    // is the actual safety net for those anyway, so this allow-list stays
    // permissive (includes '.', '+', '=', common in real base64url-ish
    // verification tokens) rather than risk rejecting a legitimate value
    // Google issues.
    private static readonly Regex SearchConsoleVerificationPattern = new(@"^[A-Za-z0-9_.=+-]+$", RegexOptions.Compiled);

    public static bool IsSafeAbsoluteUrl(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public static bool IsValidGa4MeasurementId(string value) =>
        !string.IsNullOrWhiteSpace(value) && Ga4MeasurementIdPattern.IsMatch(value);

    public static bool IsValidSearchConsoleVerification(string value) =>
        !string.IsNullOrWhiteSpace(value) && SearchConsoleVerificationPattern.IsMatch(value);

    private static readonly char[] NotificationEmailSeparators = { ',', ';' };

    /// <summary>
    /// Story 1.7: splits a <see cref="SiteSettings.NotificationEmails"/> value
    /// on ',' / ';', trims each entry and drops blanks. Does not validate -
    /// see <see cref="IsValidNotificationEmail"/>.
    /// </summary>
    public static IReadOnlyList<string> ParseNotificationEmails(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(NotificationEmailSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// A single entry is valid only when <see cref="MailAddress"/> parses it
    /// and its normalized <c>.Address</c> equals the entry verbatim - which
    /// rules out display names ("Sales &lt;a@x.vn&gt;") and any CR/LF or other
    /// header-injection payload riding along with an otherwise valid address.
    /// </summary>
    public static bool IsValidNotificationEmail(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry) || entry.Any(char.IsControl))
        {
            return false;
        }

        try
        {
            return new MailAddress(entry).Address == entry;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// True when the value holds at least one entry and every entry is a
    /// valid plain address. Callers treat a blank value as "not set" before
    /// calling this (same convention as the other validators here).
    /// </summary>
    public static bool IsValidNotificationEmailList(string? value)
    {
        // Checked on the raw value, not per entry: trimming would otherwise
        // silently accept a trailing CR/LF that is still persisted verbatim.
        if (value is null || value.Any(char.IsControl))
        {
            return false;
        }

        var entries = ParseNotificationEmails(value);
        return entries.Count > 0 && entries.All(IsValidNotificationEmail);
    }
}
