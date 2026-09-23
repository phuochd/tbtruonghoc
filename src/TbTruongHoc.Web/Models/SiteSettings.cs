using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 1.3: per-site contact details (FR-2). One instance of this
/// <see cref="SiteType"/> exists per Piranha <see cref="Site"/> (Site A and
/// Site B each get their own) - never hardcode these values in a view.
/// Auto-discovered by the existing <c>ContentTypeBuilder(...).AddAssembly(...).Build()</c>
/// call in Program.cs, same as <see cref="StandardPage"/> is for pages, and
/// edited through Piranha Manager's built-in per-site settings UI - no
/// custom Manager module needed.
///
/// Story 1.6 (GA4/Search Console) extends this same <see cref="SiteType"/>
/// with its own fields later - treat it as one shared model, not two.
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
}
