#nullable enable

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 6.5 (FR-8): Site A's installation service area is Miền Bắc – Thanh
/// Hóa. A survey's free-text location reads as out-of-area when it names a
/// recognisable place outside it (decision Q1/A): a province or city in the
/// list below, matched whole-word, ignoring case and diacritics. No
/// recognisable name means "not outside" - the warning is a hint for sales,
/// never a gate, so a rare false hit (a Hà Nội street named after a southern
/// province) is accepted.
///
/// The same folded list is handed to site-a.js (the survey form's
/// <c>data-sa-survey-areas</c>) for the live warning, so there is one copy;
/// the server recomputes the stored flag and never trusts the client's.
/// </summary>
public static class ServiceArea
{
    /// <summary>
    /// Places outside the area: pre- and post-2025-merger province names,
    /// their main cities, and common short and run-together forms. Stored
    /// readable; folded once.
    ///
    /// Left out because they fold onto common in-area names: "Vinh" (every
    /// "Vĩnh …", e.g. Vĩnh Phúc), "Đông Hà" ("Hà Đông, Hà Nội" folds to
    /// "ha dong ha noi"), "Phú Yên" (Phù Yên district, Sơn La) and "Tân An"
    /// (wards/communes across the north). Accepted, rarer collisions:
    /// "đường Đà Nẵng" (Hải Phòng) and same-named northern communes
    /// (Bình Định, Long Xuyên, Đồng Tháp) - the warning never blocks.
    /// </summary>
    private static readonly string[] OutsidePlaces =
    {
        // Bắc Trung Bộ (south of Thanh Hóa) and Duyên hải Nam Trung Bộ.
        "Nghệ An", "Hà Tĩnh", "Quảng Bình", "Đồng Hới", "Quảng Trị",
        "Thừa Thiên Huế", "TP Huế", "Thành phố Huế", "Đà Nẵng", "Quảng Nam", "Hội An", "Tam Kỳ",
        "Quảng Ngãi", "Bình Định", "Quy Nhơn", "Tuy Hòa", "Khánh Hòa", "Nha Trang",
        "Cam Ranh", "Ninh Thuận", "Phan Rang", "Bình Thuận", "Phan Thiết",
        // Tây Nguyên.
        "Kon Tum", "Gia Lai", "Pleiku", "Đắk Lắk", "Đắc Lắc", "Daklak", "Buôn Ma Thuột",
        "Đắk Nông", "Gia Nghĩa", "Lâm Đồng", "Đà Lạt", "Bảo Lộc",
        // Đông Nam Bộ.
        "Hồ Chí Minh", "TP HCM", "TPHCM", "HCM", "Sài Gòn", "Thủ Đức", "Bình Phước", "Đồng Xoài",
        "Tây Ninh", "Bình Dương", "Thủ Dầu Một", "Đồng Nai", "Biên Hòa",
        "Bà Rịa", "Vũng Tàu",
        // Đồng bằng sông Cửu Long.
        "Long An", "Tiền Giang", "Mỹ Tho", "Bến Tre", "Trà Vinh", "Vĩnh Long",
        "Đồng Tháp", "Cao Lãnh", "An Giang", "Long Xuyên", "Châu Đốc", "Kiên Giang", "Rạch Giá",
        "Phú Quốc", "Cần Thơ", "Hậu Giang", "Vị Thanh", "Sóc Trăng", "Bạc Liêu", "Cà Mau",
        // Run-together and unaccented English spellings.
        "Saigon", "HCMC", "Danang", "Dalat", "Nhatrang", "Vungtau", "Kontum", "Cantho", "Quynhon", "Hoian",
        // Regions. Not "Miền Trung"/"Trung Bộ": Thanh Hóa itself is Bắc Trung Bộ.
        "Miền Nam", "Tây Nguyên", "Nam Bộ",
    };

    /// <summary>The folded, distinct list (what <see cref="IsOutside"/> and site-a.js match against).</summary>
    public static IReadOnlyList<string> OutsideTerms { get; } =
        OutsidePlaces.Select(Fold).Where(t => t.Length > 0).Distinct().ToArray();

    /// <summary>True when <paramref name="location"/> names a place outside Miền Bắc – Thanh Hóa.</summary>
    public static bool IsOutside(string? location)
    {
        var text = " " + Fold(location) + " ";
        return text.Trim().Length > 0 && OutsideTerms.Any(term => text.Contains(" " + term + " ", System.StringComparison.Ordinal));
    }

    /// <summary>
    /// Lower-case, strip diacritics (đ → d), every non-letter/digit run to
    /// one space. Mirrors site-a.js <c>foldPlace()</c>.
    /// </summary>
    public static string Fold(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var space = false;
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var c = ch == 'đ' ? 'd' : ch;
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                if (space && sb.Length > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(c);
                space = false;
            }
            else
            {
                space = true;
            }
        }

        return sb.ToString();
    }
}
