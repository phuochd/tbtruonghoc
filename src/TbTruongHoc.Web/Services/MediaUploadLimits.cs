using Microsoft.AspNetCore.Http.Features;

namespace TbTruongHoc.Web.Services;

/// <summary>
/// Story 2.5: lets the Manager's media upload take a self-hosted story video.
/// Kestrel caps every request body at ~28.6 MB and the multipart reader at
/// 128 MB, so a real mp4 upload was cut off (the Manager showed "Server
/// responded with 0 code"). The limit is raised only for Piranha's upload
/// endpoint; every other request - the public lead forms included - keeps the
/// defaults. The endpoint is behind Piranha's MediaAdd policy, which rejects
/// an anonymous request before its body is read.
/// </summary>
public static class MediaUploadLimits
{
    /// <summary>Piranha.Manager 12's media upload endpoint.</summary>
    public const string UploadPath = "/manager/api/media/upload";

    /// <summary>1 GB (Phước, 2026-09-28).</summary>
    public const long MaxBytes = 1024L * 1024 * 1024;

    /// <summary>Raises the body and form limits when the request is a media upload.</summary>
    public static void Apply(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            || !context.Request.Path.StartsWithSegments(UploadPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var bodySize = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySize is { IsReadOnly: false })
        {
            bodySize.MaxRequestBodySize = MaxBytes;
        }

        // Same as MVC's [RequestFormLimits]: a form feature with its own
        // options, set before anything reads the form.
        if (context.Features.Get<IFormFeature>()?.Form == null)
        {
            context.Features.Set<IFormFeature>(new FormFeature(context.Request, new FormOptions
            {
                MultipartBodyLengthLimit = MaxBytes,
            }));
        }
    }
}
