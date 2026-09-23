using System;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Shared http(s)-only URL safety check for <see cref="SiteSettings"/>'s
/// Zalo/Maps fields. Used both to filter what <c>_ContactBlock.cshtml</c>
/// renders and, as defense in depth, to reject unsafe values at save time
/// via the <c>App.Hooks.SiteContent.RegisterOnBeforeSave</c> hook wired in
/// Program.cs - so a future view that renders these fields without going
/// through <c>_ContactBlock.cshtml</c> can't reintroduce the same
/// scheme-injection risk (e.g. a Manager-entered "javascript:"/"data:"
/// value).
/// </summary>
public static class SiteSettingsValidation
{
    public static bool IsSafeAbsoluteUrl(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
