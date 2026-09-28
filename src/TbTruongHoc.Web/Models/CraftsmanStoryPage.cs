using System.Collections.Generic;
using System.Linq;
using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 2.5 (AD-2, FR-10): the standalone craftsman story page (Phạm Trí
/// Trong) - captioned photos, an optional self-hosted video (with an
/// optional .vtt captions track), prose blocks, and the short quote the shared
/// trust-block (<c>Views/Shared/_TrustBlock.cshtml</c>) shows on the hub,
/// category and product pages. Every field is optional; a blank one renders
/// nothing.
/// </summary>
[PageType(Title = "Câu chuyện nghệ nhân")]
[ContentTypeRoute(Title = "Default", Route = "/craftsmanstory")]
public class CraftsmanStoryPage : Page<CraftsmanStoryPage>
{
    /// <summary>Small label above the title.</summary>
    [Region(Title = "Dòng nhãn nhỏ (eyebrow)")]
    public StringField Eyebrow { get; set; }

    /// <summary>Photos in display order, each with an optional caption.</summary>
    [Region(Title = "Ảnh", ListTitle = "Caption", Description = "Mỗi ảnh hiện kèm dòng chú thích bên dưới (nếu có). Mục không chọn ảnh sẽ bị bỏ qua.")]
    public IList<StoryPhoto> Photos { get; set; } = new List<StoryPhoto>();

    /// <summary>Optional self-hosted video (mp4).</summary>
    [Region(Title = "Video", Description = "Tải video mp4 lên thư viện Media (tối đa 1 GB; nên nén về 720p, khoảng 20–60 MB, để khách xem trên điện thoại không phải tải nặng). Không bắt buộc; để trống thì trang không hiện khung video.")]
    public VideoField Video { get; set; }

    /// <summary>The video's WebVTT captions file.</summary>
    [Region(Title = "Phụ đề video (.vtt)", Description = "Không bắt buộc. File phụ đề .vtt cho video ở trên; chỉ nhận file .vtt, file khác bị bỏ qua.")]
    public DocumentField VideoCaptions { get; set; }

    /// <summary>The short quote shown by the trust-block.</summary>
    [Region(Title = "Trích dẫn ngắn (trust-block)", Description = "Câu nói ngắn của nghệ nhân (1–2 câu ngắn), hiển thị trong khung tin cậy trên trang danh mục và trang sản phẩm. Cần có cả Trích dẫn và Người nói mới hiển thị.")]
    public TextField Quote { get; set; }

    /// <summary>Who said the quote, e.g. "Nghệ nhân Phạm Trí Trong".</summary>
    [Region(Title = "Người nói (trích dẫn)")]
    public StringField QuoteAttribution { get; set; }

    /// <summary>The eyebrow, trimmed, or null when blank.</summary>
    public string EyebrowText => Trimmed(Eyebrow?.Value);

    /// <summary>The quote, trimmed, or null when blank.</summary>
    public string QuoteText => Trimmed(Quote?.Value);

    /// <summary>The quote attribution, trimmed, or null when blank.</summary>
    public string QuoteAttributionText => Trimmed(QuoteAttribution?.Value);

    /// <summary>
    /// Photos whose media exists, in order, with the caption trimmed (null
    /// when blank). Items without an image are skipped.
    /// </summary>
    public IReadOnlyList<(ImageField Image, string Caption)> PhotoItems =>
        (Photos ?? Enumerable.Empty<StoryPhoto>())
            .Where(p => p?.Image != null && p.Image.HasValue && p.Image.Media != null)
            .Select(p => (p.Image, Trimmed(p.Caption?.Value)))
            .ToList();

    /// <summary>True when a video file is set - the video renders.</summary>
    public bool HasVideo => Video != null && Video.HasValue && Video.Media != null;

    /// <summary>
    /// True when a captions file is set and it is a WebVTT (.vtt) file - the
    /// video then gets a captions track. Any other document is ignored.
    /// </summary>
    public bool HasVideoCaptions =>
        VideoCaptions != null && VideoCaptions.HasValue && VideoCaptions.Media != null
        && IsVtt(VideoCaptions.Media);

    private static bool IsVtt(Media media) =>
        (media.Filename ?? "").EndsWith(".vtt", System.StringComparison.OrdinalIgnoreCase)
        || string.Equals(media.ContentType, "text/vtt", System.StringComparison.OrdinalIgnoreCase);

    private static string Trimmed(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Story 2.5: one captioned photo on a <see cref="CraftsmanStoryPage"/>.</summary>
public class StoryPhoto
{
    [Field(Title = "Ảnh")]
    public ImageField Image { get; set; }

    [Field(Title = "Chú thích")]
    public StringField Caption { get; set; }
}
