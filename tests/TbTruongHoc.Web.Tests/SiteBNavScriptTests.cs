#nullable enable

using System.IO;
using Jint;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 2.1: runs the real <c>wwwroot/assets/js/site-b-nav.js</c> in Jint
/// against a stubbed DOM shaped like <c>_SiteBNav.cshtml</c> (hamburger, two
/// submenu disclosure buttons), covering toggle, one-open-at-a-time,
/// Escape-closes-and-returns-focus (only when focus is in the nav),
/// focus-leaving-the-nav and click-outside. Same approach as
/// <see cref="AnalyticsConsentScriptTests"/>; no database.
/// </summary>
public class SiteBNavScriptTests
{
    private readonly Engine _js = new();

    public SiteBNavScriptTests()
    {
        _js.Execute(@"
            var window = {};
            var focused = null;
            var docListeners = {};
            function element(name) {
                return {
                    name: name,
                    attrs: {},
                    listeners: {},
                    getAttribute: function (n) { return n in this.attrs ? this.attrs[n] : null; },
                    setAttribute: function (n, v) { this.attrs[n] = String(v); },
                    addEventListener: function (type, fn) { this.listeners[type] = fn; },
                    click: function () { if (this.listeners.click) { this.listeners.click({ target: this }); } },
                    focus: function () { focused = this.name; }
                };
            }
            var toggle = element('toggle');
            toggle.setAttribute('aria-expanded', 'false');
            var subA = element('subA');
            subA.setAttribute('aria-expanded', 'false');
            var subB = element('subB');
            subB.setAttribute('aria-expanded', 'false');
            var outside = element('outside');
            var root = element('root');
            root.setAttribute('data-menu-open', 'false');
            var inside = [root, toggle, subA, subB];
            root.contains = function (el) { return inside.indexOf(el) >= 0; };
            root.querySelector = function (sel) { return sel === '[data-sb-nav-toggle]' ? toggle : null; };
            root.querySelectorAll = function (sel) { return sel === '[data-sb-submenu-toggle]' ? [subA, subB] : []; };
            var document = {
                readyState: 'complete',
                addEventListener: function (type, fn) { docListeners[type] = fn; },
                activeElement: null,
                querySelector: function (sel) { return sel === '[data-sb-nav]' ? root : null; }
            };
            // Focus is inside the nav unless a test says otherwise.
            function press(key, active) {
                document.activeElement = active === undefined ? toggle : active;
                docListeners.keydown({ key: key });
            }
            function blurTo(next) { root.listeners.focusout({ target: toggle, relatedTarget: next }); }
            function clickOn(el) { el.click(); docListeners.click({ target: el }); }");
        _js.Execute(File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "TbTruongHoc.Web", "wwwroot", "assets", "js", "site-b-nav.js")));
    }

    private string Attr(string el, string name) => _js.Evaluate($"{el}.getAttribute('{name}')").AsString();

    private string? Focused => _js.Evaluate("focused").ToObject() as string;

    [Fact]
    public void Hamburger_Opens_And_Closes_The_Sheet()
    {
        _js.Execute("clickOn(toggle);");
        Assert.Equal("true", Attr("toggle", "aria-expanded"));
        Assert.Equal("true", Attr("root", "data-menu-open"));

        _js.Execute("clickOn(toggle);");
        Assert.Equal("false", Attr("toggle", "aria-expanded"));
        Assert.Equal("false", Attr("root", "data-menu-open"));
    }

    [Fact]
    public void Submenu_Button_Toggles_Its_Own_Aria_Expanded()
    {
        _js.Execute("clickOn(subA);");
        Assert.Equal("true", Attr("subA", "aria-expanded"));

        _js.Execute("clickOn(subA);");
        Assert.Equal("false", Attr("subA", "aria-expanded"));
    }

    [Fact]
    public void Opening_One_Submenu_Closes_The_Other()
    {
        _js.Execute("clickOn(subA); clickOn(subB);");

        Assert.Equal("false", Attr("subA", "aria-expanded"));
        Assert.Equal("true", Attr("subB", "aria-expanded"));
    }

    [Fact]
    public void Escape_Closes_Open_Submenu_And_Returns_Focus_To_Its_Button()
    {
        _js.Execute("clickOn(subB); press('Escape');");

        Assert.Equal("false", Attr("subB", "aria-expanded"));
        Assert.Equal("subB", Focused);
    }

    [Fact]
    public void Escape_Closes_Sheet_And_Returns_Focus_To_Hamburger()
    {
        _js.Execute("clickOn(toggle); press('Escape');");

        Assert.Equal("false", Attr("toggle", "aria-expanded"));
        Assert.Equal("false", Attr("root", "data-menu-open"));
        Assert.Equal("toggle", Focused);
    }

    [Fact]
    public void Escape_Closes_Submenu_Before_Sheet()
    {
        _js.Execute("clickOn(toggle); clickOn(subA); press('Escape');");

        Assert.Equal("false", Attr("subA", "aria-expanded"));
        Assert.Equal("true", Attr("toggle", "aria-expanded"));
        Assert.Equal("subA", Focused);
    }

    [Fact]
    public void Escape_With_Nothing_Open_Does_Nothing()
    {
        _js.Execute("press('Escape');");

        Assert.Null(Focused);
        Assert.Equal("false", Attr("toggle", "aria-expanded"));
    }

    [Fact]
    public void Other_Keys_Are_Ignored()
    {
        _js.Execute("clickOn(toggle); press('Tab');");

        Assert.Equal("true", Attr("toggle", "aria-expanded"));
        Assert.Null(Focused);
    }

    [Fact]
    public void Escape_Outside_The_Nav_Closes_Quietly_Without_Moving_Focus()
    {
        _js.Execute("clickOn(toggle); clickOn(subA); press('Escape', outside);");

        Assert.Equal("false", Attr("subA", "aria-expanded"));
        Assert.Equal("false", Attr("toggle", "aria-expanded"));
        Assert.Equal("false", Attr("root", "data-menu-open"));
        Assert.Null(Focused);
    }

    [Fact]
    public void Escape_With_No_Focused_Element_Does_Not_Move_Focus()
    {
        _js.Execute("clickOn(subB); press('Escape', null);");

        Assert.Equal("false", Attr("subB", "aria-expanded"));
        Assert.Null(Focused);
    }

    [Fact]
    public void Focus_Leaving_The_Nav_Closes_Submenus_And_Sheet()
    {
        _js.Execute("clickOn(toggle); clickOn(subA); blurTo(outside);");

        Assert.Equal("false", Attr("subA", "aria-expanded"));
        Assert.Equal("false", Attr("toggle", "aria-expanded"));
        Assert.Equal("false", Attr("root", "data-menu-open"));
        Assert.Null(Focused);
    }

    [Fact]
    public void Focus_Moving_Within_The_Nav_Keeps_Things_Open()
    {
        _js.Execute("clickOn(toggle); clickOn(subA); blurTo(subB);");

        Assert.Equal("true", Attr("subA", "aria-expanded"));
        Assert.Equal("true", Attr("toggle", "aria-expanded"));
    }

    [Fact]
    public void Focusout_With_No_Related_Target_Keeps_Things_Open()
    {
        _js.Execute("clickOn(toggle); blurTo(null);");

        Assert.Equal("true", Attr("toggle", "aria-expanded"));
    }

    [Fact]
    public void Click_Outside_Closes_Open_Submenu()
    {
        _js.Execute("clickOn(subA); clickOn(outside);");

        Assert.Equal("false", Attr("subA", "aria-expanded"));
    }

    [Fact]
    public void No_Hover_Listeners_Are_Registered()
    {
        Assert.True(_js.Evaluate(@"
            [toggle, subA, subB, root].every(function (el) {
                return !el.listeners.mouseenter && !el.listeners.mouseover && !el.listeners.pointerenter;
            }) && !docListeners.mouseover").AsBoolean());
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
