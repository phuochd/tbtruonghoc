#nullable enable

using System.IO;
using Jint;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 6.4: runs the real <c>site-a.js</c> (and, for the photo badge, the
/// real, unmodified <c>sb-gallery.js</c>) in Jint against a stubbed DOM
/// shaped like <c>SiteAProductPost.cshtml</c>: the quote CTA
/// ([data-sa-modal-open]) and its native &lt;dialog&gt;, and a gallery with
/// one regular and one genuine thumbnail. Events bubble through a parent
/// chain so listener order matches a browser. No database.
/// </summary>
public class SiteAPdpScriptTests
{
    private readonly Engine _js = new();

    public SiteAPdpScriptTests()
    {
        _js.Execute(@"
            var window = {};
            var timers = 0;
            function setTimeout() { timers++; }
            function setInterval() { timers++; }
            var focused = null;
            function element(name, attrs, parent) {
                var el = {
                    name: name,
                    attrs: {},
                    listeners: {},
                    parent: parent || null,
                    getAttribute: function (n) { return n in this.attrs ? this.attrs[n] : null; },
                    setAttribute: function (n, v) { this.attrs[n] = String(v); },
                    removeAttribute: function (n) { delete this.attrs[n]; },
                    hasAttribute: function (n) { return n in this.attrs; },
                    addEventListener: function (type, fn) { (this.listeners[type] = this.listeners[type] || []).push(fn); },
                    focus: function () { focused = this.name; },
                    querySelector: function () { return null; },
                    querySelectorAll: function () { return []; }
                };
                for (var k in attrs) { el.attrs[k] = attrs[k]; }
                return el;
            }
            // Dispatch with bubbling: target first, then each ancestor.
            function dispatch(target, type, extra) {
                var ev = { type: type, target: target, button: 0, defaultPrevented: false,
                    preventDefault: function () { this.defaultPrevented = true; } };
                for (var k in extra) { ev[k] = extra[k]; }
                for (var el = target; el; el = el.parent) {
                    var fns = el.listeners[type] || [];
                    for (var i = 0; i < fns.length; i++) { fns[i].call(el, ev); }
                }
                return ev;
            }

            // Quote modal.
            var body = element('body');
            var cta = element('cta', { 'data-sa-modal-open': 'sa-quote-modal' }, body);
            var dialog = element('dialog', { id: 'sa-quote-modal' }, body);
            dialog.open = false;
            dialog.showModal = function () { this.open = true; this.setAttribute('open', ''); };
            dialog.close = function () {
                if (!this.open) { return; }
                this.open = false;
                this.removeAttribute('open');
                dispatch(this, 'close');
            };
            var panel = element('panel', {}, dialog);
            var closeBtn = element('closeBtn', { 'data-sa-modal-close': '' }, panel);
            var field = element('field', {}, panel);
            dialog.querySelectorAll = function (sel) { return sel === '[data-sa-modal-close]' ? [closeBtn] : []; };

            // Gallery: thumb 0 regular (current), thumb 1 genuine.
            var gallery = element('gallery', { 'data-sb-gallery': '', 'data-sa-pdp-gallery': 'true' }, body);
            var mainImg = element('mainImg', { src: '/a.png', alt: 'A' }, gallery);
            var badge = element('badge', { hidden: 'hidden' }, gallery);
            var list = element('list', {}, gallery);
            var t0 = element('t0', { href: '/a.png', 'data-alt': 'A', 'aria-current': 'true' }, list);
            var t1 = element('t1', { href: '/g.png', 'data-alt': 'G', 'data-genuine': 'true' }, list);
            var t0img = element('t0img', {}, t0);
            gallery.querySelector = function (sel) {
                if (sel === '[data-sb-gallery-main]') { return mainImg; }
                if (sel === '[data-sa-photo-badge]') { return badge; }
                return null;
            };
            gallery.querySelectorAll = function (sel) { return sel === '[data-sb-gallery-thumb]' ? [t0, t1] : []; };

            var document = {
                readyState: 'complete',
                addEventListener: function () {},
                getElementById: function (id) { return id === 'sa-quote-modal' ? dialog : null; },
                querySelector: function () { return null; },
                querySelectorAll: function (sel) {
                    if (sel === '[data-sa-modal-open]') { return [cta]; }
                    if (sel === '[data-sa-pdp-gallery=""true""]') { return [gallery]; }
                    if (sel === '[data-sb-gallery]') { return [gallery]; }
                    return [];
                }
            };");
        var js = Path.Combine(FindRepoRoot(), "src", "TbTruongHoc.Web", "wwwroot", "assets", "js");
        // Page order: the view's sb-gallery.js runs before the layout's site-a.js.
        _js.Execute(File.ReadAllText(Path.Combine(js, "sb-gallery.js")));
        _js.Execute(File.ReadAllText(Path.Combine(js, "site-a.js")));
    }

    private string? Attr(string el, string name) => _js.Evaluate($"{el}.getAttribute('{name}')").ToObject() as string;

    private bool IsOpen => _js.Evaluate("dialog.open").AsBoolean();

    private string? Focused => _js.Evaluate("focused").ToObject() as string;

    // --- modal ---

    [Fact]
    public void Cta_Opens_The_Dialog_Modally()
    {
        Assert.Equal("false", Attr("cta", "aria-expanded"));

        _js.Execute("dispatch(cta, 'click');");

        Assert.True(IsOpen);
        Assert.Equal("true", Attr("cta", "aria-expanded"));
    }

    [Fact]
    public void Close_Button_Closes_And_Returns_Focus_To_The_Cta()
    {
        _js.Execute("dispatch(cta, 'click'); dispatch(closeBtn, 'click');");

        Assert.False(IsOpen);
        Assert.Equal("false", Attr("cta", "aria-expanded"));
        Assert.Equal("cta", Focused);
    }

    [Fact]
    public void Backdrop_Click_Closes_But_A_Click_Inside_The_Panel_Does_Not()
    {
        _js.Execute("dispatch(cta, 'click'); dispatch(field, 'click');");
        Assert.True(IsOpen);

        _js.Execute("dispatch(panel, 'click');");
        Assert.True(IsOpen);

        _js.Execute("dispatch(dialog, 'click');");
        Assert.False(IsOpen);
        Assert.Equal("cta", Focused);
    }

    [Fact]
    public void Escape_Native_Close_Also_Returns_Focus()
    {
        // Esc on a modal <dialog> is handled by the browser, which closes it
        // and fires 'close' - the same path as dialog.close().
        _js.Execute("dispatch(cta, 'click'); dialog.close();");

        Assert.False(IsOpen);
        Assert.Equal("cta", Focused);
        Assert.Equal("false", Attr("cta", "aria-expanded"));
    }

    [Fact]
    public void Reopening_Works_And_Nothing_Runs_On_A_Timer()
    {
        _js.Execute("dispatch(cta, 'click'); dispatch(closeBtn, 'click'); dispatch(cta, 'click');");

        Assert.True(IsOpen);
        Assert.Equal(0, (int)_js.Evaluate("timers").AsNumber());
    }

    [Fact]
    public void Opener_Without_A_Matching_Dialog_Is_Left_Alone()
    {
        _js.Execute(@"
            var stray = element('stray', { 'data-sa-modal-open': 'nope' }, body);
            window.siteA.initModalOpeners([stray]);
            dispatch(stray, 'click');");

        Assert.Null(Attr("stray", "aria-expanded"));
        Assert.False(IsOpen);
    }

    // --- photo badge ---

    [Fact]
    public void Badge_Follows_The_Selected_Thumbnail()
    {
        _js.Execute("dispatch(t1, 'click');");
        Assert.Equal("/g.png", Attr("mainImg", "src"));
        Assert.Null(Attr("badge", "hidden"));

        // A click on the thumbnail's <img> bubbles through the link.
        _js.Execute("dispatch(t0img, 'click');");
        Assert.Equal("/a.png", Attr("mainImg", "src"));
        Assert.Equal("hidden", Attr("badge", "hidden"));
    }

    [Fact]
    public void Modified_Click_Leaves_Image_And_Badge_Alone()
    {
        _js.Execute("dispatch(t1, 'click', { ctrlKey: true });");

        Assert.Equal("/a.png", Attr("mainImg", "src"));
        Assert.Equal("hidden", Attr("badge", "hidden"));
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
