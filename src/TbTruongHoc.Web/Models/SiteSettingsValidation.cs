using System;
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
}
