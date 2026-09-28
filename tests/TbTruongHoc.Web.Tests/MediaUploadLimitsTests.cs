using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using TbTruongHoc.Web.Services;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 2.5: the raised upload limit applies to the Manager's media upload
/// only - every other request keeps ASP.NET Core's defaults.
/// </summary>
public class MediaUploadLimitsTests
{
    private const long KestrelDefault = 30_000_000;

    [Theory]
    [InlineData("POST", "/manager/api/media/upload")]
    [InlineData("post", "/Manager/API/Media/Upload")]
    public void Media_Upload_Gets_The_Raised_Limits(string method, string path)
    {
        var (context, bodySize) = Context(method, path);

        MediaUploadLimits.Apply(context);

        Assert.Equal(MediaUploadLimits.MaxBytes, bodySize.MaxRequestBodySize);
        Assert.IsType<FormFeature>(context.Features.Get<IFormFeature>());
    }

    [Theory]
    [InlineData("GET", "/manager/api/media/upload")]
    [InlineData("POST", "/api/leads")]
    [InlineData("POST", "/manager/api/media/save")]
    [InlineData("POST", "/manager/api/media/uploadx")]
    public void Other_Requests_Keep_The_Defaults(string method, string path)
    {
        var (context, bodySize) = Context(method, path);

        MediaUploadLimits.Apply(context);

        Assert.Equal(KestrelDefault, bodySize.MaxRequestBodySize);
        Assert.Null(context.Features.Get<IFormFeature>());
    }

    [Fact]
    public void Read_Only_Body_Size_Is_Left_Alone()
    {
        var (context, bodySize) = Context("POST", MediaUploadLimits.UploadPath);
        bodySize.IsReadOnly = true;

        MediaUploadLimits.Apply(context);

        Assert.Equal(KestrelDefault, bodySize.MaxRequestBodySize);
    }

    private static (DefaultHttpContext Context, FakeBodySize BodySize) Context(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        var bodySize = new FakeBodySize { MaxRequestBodySize = KestrelDefault };
        context.Features.Set<IHttpMaxRequestBodySizeFeature>(bodySize);
        return (context, bodySize);
    }

    private sealed class FakeBodySize : IHttpMaxRequestBodySizeFeature
    {
        public bool IsReadOnly { get; set; }
        public long? MaxRequestBodySize { get; set; }
    }
}
