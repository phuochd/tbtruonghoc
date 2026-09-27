#nullable enable

using System.IO;
using Jint;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 2.4: runs the real <c>wwwroot/assets/js/sb-gallery.js</c> in Jint
/// against a stubbed DOM shaped like the PDP gallery in
/// <c>Views/Cms/ProductPost.cshtml</c> (main image + three thumbnail links),
/// covering the in-place swap, aria-current moving, modified clicks being
/// left to the browser, and nothing running on its own. Same approach as
/// <see cref="SiteBNavScriptTests"/>; no database.
/// </summary>
public class SbGalleryScriptTests
{
    private readonly Engine _js = new();

    public SbGalleryScriptTests()
    {
        _js.Execute(@"
            var window = {};
            var timers = 0;
            function setTimeout() { timers++; }
            function setInterval() { timers++; }
            function element(name, attrs) {
                var el = {
                    name: name,
                    attrs: {},
                    listeners: {},
                    getAttribute: function (n) { return n in this.attrs ? this.attrs[n] : null; },
                    setAttribute: function (n, v) { this.attrs[n] = String(v); },
                    removeAttribute: function (n) { delete this.attrs[n]; },
                    addEventListener: function (type, fn) { this.listeners[type] = fn; }
                };
                for (var k in attrs) { el.attrs[k] = attrs[k]; }
                return el;
            }
            var main = element('main', { src: '/uploads/a.png', alt: 'Trống – ảnh 1', width: '800', height: '600' });
            var t1 = element('t1', { href: '/uploads/a.png', 'data-alt': 'Trống – ảnh 1', 'data-width': '800', 'data-height': '600', 'aria-current': 'true' });
            var t2 = element('t2', { href: '/uploads/b.png', 'data-alt': 'Mặt trống nhìn nghiêng', 'data-width': '400', 'data-height': '900' });
            var t3 = element('t3', { href: '/uploads/c.png', 'data-alt': 'Trống – ảnh 3' });
            var root = element('root', {});
            root.querySelector = function (sel) { return sel === '[data-sb-gallery-main]' ? main : null; };
            root.querySelectorAll = function (sel) { return sel === '[data-sb-gallery-thumb]' ? [t1, t2, t3] : []; };
            var document = {
                readyState: 'complete',
                addEventListener: function () {},
                querySelectorAll: function (sel) { return sel === '[data-sb-gallery]' ? [root] : []; }
            };
            function tap(el, extra) {
                var ev = { button: 0, defaultPrevented: false, prevented: false,
                    preventDefault: function () { this.prevented = true; this.defaultPrevented = true; } };
                for (var k in extra) { ev[k] = extra[k]; }
                el.listeners.click(ev);
                return ev.prevented;
            }");
        _js.Execute(File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "TbTruongHoc.Web", "wwwroot", "assets", "js", "sb-gallery.js")));
    }

    private string? Attr(string el, string name) => _js.Evaluate($"{el}.getAttribute('{name}')").ToObject() as string;

    [Fact]
    public void Tapping_A_Thumbnail_Swaps_Main_Image_In_Place()
    {
        var prevented = _js.Evaluate("tap(t2)").AsBoolean();

        Assert.True(prevented);
        Assert.Equal("/uploads/b.png", Attr("main", "src"));
        Assert.Equal("Mặt trống nhìn nghiêng", Attr("main", "alt"));
        Assert.Equal("400", Attr("main", "width"));
        Assert.Equal("900", Attr("main", "height"));
    }

    [Fact]
    public void Tapping_A_Thumbnail_Moves_Aria_Current()
    {
        _js.Execute("tap(t2);");
        Assert.Null(Attr("t1", "aria-current"));
        Assert.Equal("true", Attr("t2", "aria-current"));
        Assert.Null(Attr("t3", "aria-current"));

        _js.Execute("tap(t1);");
        Assert.Equal("true", Attr("t1", "aria-current"));
        Assert.Null(Attr("t2", "aria-current"));
    }

    [Fact]
    public void Thumbnail_Without_Dimensions_Clears_Stale_Width_And_Height()
    {
        _js.Execute("tap(t3);");

        Assert.Equal("/uploads/c.png", Attr("main", "src"));
        Assert.Null(Attr("main", "width"));
        Assert.Null(Attr("main", "height"));
    }

    [Theory]
    [InlineData("{ ctrlKey: true }")]
    [InlineData("{ metaKey: true }")]
    [InlineData("{ shiftKey: true }")]
    [InlineData("{ altKey: true }")]
    [InlineData("{ defaultPrevented: true }")]
    [InlineData("{ button: 1 }")]
    public void Modified_Click_Is_Left_To_The_Browser(string extra)
    {
        var prevented = _js.Evaluate($"tap(t2, {extra})").AsBoolean();

        Assert.False(prevented);
        Assert.Equal("/uploads/a.png", Attr("main", "src"));
        Assert.Equal("true", Attr("t1", "aria-current"));
    }

    [Fact]
    public void Nothing_Runs_On_A_Timer()
    {
        Assert.Equal(0, (int)_js.Evaluate("timers").AsNumber());
        Assert.Equal("/uploads/a.png", Attr("main", "src"));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TbTruongHoc.sln")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
