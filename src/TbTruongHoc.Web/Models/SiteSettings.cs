using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 1.3: per-site contact details (FR-2). Story 1.6 (FR-4) extends the
/// same type with GA4/Search Console fields - one shared model, not two.
/// One instance of this <see cref="SiteType"/> exists per Piranha
/// <see cref="Site"/> (Site A and Site B each get their own) - never
/// hardcode these values in a view. Auto-discovered by the existing
/// <c>ContentTypeBuilder(...).AddAssembly(...).Build()</c> call in
/// Program.cs, same as <see cref="StandardPage"/> is for pages, and edited
/// through Piranha Manager's built-in per-site settings UI - no custom
/// Manager module needed.
/// </summary>
[SiteType(Title = "Site settings")]
public class SiteSettings : SiteContent<SiteSettings>
{
    [Region(Title = "Phone", Description = "Click-to-call number, in local or international format (e.g. 090 123 4567 or +84 90 123 4567). Digits become the tel: link automatically - punctuation and spacing are only for display.")]
    public StringField Phone { get; set; }

    [Region(Title = "Zalo URL", Description = "Full Zalo link this site's visitors should open, e.g. https://zalo.me/0901234567 - not just the phone number or handle.")]
    public StringField ZaloUrl { get; set; }

    [Region(Title = "Address", Description = "Plain-text street address shown to visitors - not used for a Maps link, that's the separate Maps URL field below.")]
    public StringField Address { get; set; }

    [Region(Title = "Maps URL", Description = "Link that opens this location in Google Maps (share/directions link), e.g. https://maps.google.com/?q=... - not an embed/iframe URL.")]
    public StringField MapsUrl { get; set; }

    /// <summary>
    /// Story 6.1: the public contact email shown in Site A's footer strip as
    /// a mailto: link - only when it is a single valid address.
    /// </summary>
    [Region(Title = "Email", Description = "Public contact email shown to visitors in the footer, e.g. lienhe@example.vn. One plain address only (no name, no list). Leave blank to hide it. An invalid address is not shown on the site.")]
    public StringField Email { get; set; }

    [Region(Title = "GA4 Measurement ID", Description = "This site's own Google Analytics 4 measurement ID, e.g. G-XXXXXXXXXX. Leave blank to disable analytics on this site - no tracking script is emitted while empty.")]
    public StringField Ga4MeasurementId { get; set; }

    [Region(Title = "Search Console Verification", Description = "The value Google Search Console's HTML-tag verification method gives you - paste only the content value from <meta name=\"google-site-verification\" content=\"...\">, not the whole tag. Leave blank to omit the tag.")]
    public StringField SearchConsoleVerification { get; set; }

    /// <summary>
    /// Story 1.7 (FR-3, AD-3): who gets the new-lead email for this site.
    /// Per site, never a global fallback - left blank, no email is sent.
    /// </summary>
    [Region(Title = "Notification emails", Description = "Email address(es) that receive a notification for every new lead on this site. Separate several addresses with a comma or semicolon, e.g. sales@example.vn; manager@example.vn. Plain addresses only (no display names). Leave blank to send no notification.")]
    public StringField NotificationEmails { get; set; }
}
