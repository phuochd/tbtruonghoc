#nullable enable

using Microsoft.Extensions.Hosting;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 1.10: the one code-level gate deciding whether GA4 may load at all
/// on a render. Shared by _Analytics.cshtml (consent script include) and
/// _CookieConsent.cshtml (banner + footer link) so both apply the same rule.
/// Each partial reads SiteSettings itself; if a save ever landed between the
/// two reads, either half alone still fails closed (a hidden banner with no
/// script, or a script with no banner to accept). Outside Production nothing GA4-related is emitted, whatever
/// SiteSettings holds - a real measurement ID copied into a dev/staging
/// database must never send events to the production property.
/// </summary>
public static class AnalyticsGate
{
    public static bool ShouldLoadGa4(IHostEnvironment env, string? ga4Id) =>
        env.IsProduction() && ga4Id is not null && SiteSettingsValidation.IsValidGa4MeasurementId(ga4Id);
}
