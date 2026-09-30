#nullable enable

using System.IO;
using Jint;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 6.1: runs the real <c>wwwroot/assets/js/site-a.js</c> in Jint
/// against a stubbed DOM shaped like <c>_SiteANav.cshtml</c> (hamburger,
/// sheet close button, one "Sản phẩm" dropdown with three rows) and
/// <c>_SiteAHero.cshtml</c> (three-slide carousel). Covers dropdown
/// open / arrow keys / Escape / click-away / hover, the sheet toggle and
/// the manual carousel. Same approach as <see cref="SiteBNavScriptTests"/>;
/// no database.
/// </summary>
public class SiteAScriptTests
{
    private readonly Engine _js = new();

    public SiteAScriptTests()
    {
        _js.Execute(@"
            var desktop = true;
            var window = {
                matchMedia: function (q) { return { matches: q === '(min-width: 1024px)' && desktop }; }
            };
            var focused = null;
            var docListeners = {};
            var intervals = 0;
            function setInterval() { intervals++; }
            function setTimeout() { intervals++; }
            function element(name) {
                return {
                    name: name,
                    attrs: {},
                    listeners: {},
                    children: [],
                    getAttribute: function (n) { return n in this.attrs ? this.attrs[n] : null; },
                    setAttribute: function (n, v) { this.attrs[n] = String(v); },
                    removeAttribute: function (n) { delete this.attrs[n]; },
                    hasAttribute: function (n) { return n in this.attrs; },
                    addEventListener: function (type, fn) { this.listeners[type] = fn; },
                    click: function () { if (this.listeners.click) { this.listeners.click({ target: this }); } },
                    focus: function () { focused = this.name; document.activeElement = this; },
                    contains: function (el) {
                        if (el === this) { return true; }
                        for (var i = 0; i < this.children.length; i++) {
                            if (this.children[i].contains(el)) { return true; }
                        }
                        return false;
                    }
                };
            }

            // Nav
            var root = element('root');
            root.setAttribute('data-menu-open', 'false');
            var toggle = element('toggle');
            toggle.setAttribute('aria-expanded', 'false');
            var closeBtn = element('close');
            var item = element('item');
            var trigger = element('trigger');
            trigger.setAttribute('aria-expanded', 'false');
            var rows = [element('row0'), element('row1'), element('row2')];
            var viewAll = rows[2];
            var plainLink = element('plainLink');
            item.children = [trigger].concat(rows);
            root.children = [toggle, closeBtn, item, plainLink];
            item.querySelector = function (sel) { return sel === '[data-sa-dropdown-toggle]' ? trigger : null; };
            item.querySelectorAll = function (sel) { return sel === '[data-sa-dropdown-item]' ? rows : []; };
            root.querySelector = function (sel) {
                if (sel === '[data-sa-nav-toggle]') { return toggle; }
                if (sel === '[data-sa-nav-close]') { return closeBtn; }
                return null;
            };
            root.querySelectorAll = function (sel) { return sel === '[data-sa-dropdown]' ? [item] : []; };

            // Carousel
            var carousel = element('carousel');
            var slides = [element('s0'), element('s1'), element('s2')];
            slides[1].setAttribute('hidden', 'hidden');
            slides[2].setAttribute('hidden', 'hidden');
            var dots = [element('d0'), element('d1'), element('d2')];
            dots[0].setAttribute('aria-current', 'true');
            var prev = element('prev');
            var next = element('next');
            carousel.querySelector = function (sel) {
                if (sel === '[data-sa-carousel-prev]') { return prev; }
                if (sel === '[data-sa-carousel-next]') { return next; }
                return null;
            };
            carousel.querySelectorAll = function (sel) {
                if (sel === '[data-sa-slide]') { return slides; }
                if (sel === '[data-sa-carousel-dot]') { return dots; }
                return [];
            };

            var outside = element('outside');
            var document = {
                readyState: 'complete',
                addEventListener: function (type, fn) { docListeners[type] = fn; },
                activeElement: null,
                querySelector: function (sel) { return sel === '[data-sa-nav]' ? root : null; },
                querySelectorAll: function (sel) { return sel === '[data-sa-carousel]' ? [carousel] : []; }
            };

            var prevented = false;
            function key(el, k) {
                prevented = false;
                el.listeners.keydown({ key: k, target: el, preventDefault: function () { prevented = true; } });
            }
            function press(k, active) {
                document.activeElement = active === undefined ? trigger : active;
                docListeners.keydown({ key: k });
            }
            function clickOn(el) { el.click(); docListeners.click({ target: el }); }
            function visible() {
                var out = [];
                for (var i = 0; i < slides.length; i++) { if (!slides[i].hasAttribute('hidden')) { out.push(i); } }
                return out.join(',');
            }
            function currentDot() {
                var out = [];
                for (var i = 0; i < dots.length; i++) { if (dots[i].getAttribute('aria-current') === 'true') { out.push(i); } }
                return out.join(',');
            }");
        _js.Execute(File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "TbTruongHoc.Web", "wwwroot", "assets", "js", "site-a.js")));
    }

    private string? Attr(string el, string name) => _js.Evaluate($"{el}.getAttribute('{name}')").ToObject() as string;

    private string? Focused => _js.Evaluate("focused").ToObject() as string;

    private string Eval(string expr) => _js.Evaluate(expr).ToString();

    // --- dropdown ---

    [Fact]
    public void Click_Or_Enter_On_Trigger_Opens_And_Closes_Dropdown()
    {
        // Enter/Space on a <button> fire its click.
        _js.Execute("clickOn(trigger);");
        Assert.Equal("true", Attr("trigger", "aria-expanded"));

        _js.Execute("clickOn(trigger);");
        Assert.Equal("false", Attr("trigger", "aria-expanded"));
    }

    [Fact]
    public void ArrowDown_On_Trigger_Opens_And_Focuses_First_Row()
    {
        _js.Execute("key(trigger, 'ArrowDown');");

        Assert.Equal("true", Attr("trigger", "aria-expanded"));
        Assert.Equal("row0", Focused);
        Assert.Equal("true", Eval("prevented"));
    }

    [Fact]
    public void Arrow_Keys_Move_Through_Rows_And_Wrap()
    {
        _js.Execute("key(trigger, 'ArrowDown'); key(rows[0], 'ArrowDown');");
        Assert.Equal("row1", Focused);

        _js.Execute("key(rows[1], 'ArrowDown'); key(rows[2], 'ArrowDown');");
        Assert.Equal("row0", Focused);

        _js.Execute("key(rows[0], 'ArrowUp');");
        Assert.Equal("row2", Focused);

        _js.Execute("key(rows[2], 'Home');");
        Assert.Equal("row0", Focused);

        _js.Execute("key(rows[0], 'End');");
        Assert.Equal("row2", Focused);
    }

    [Fact]
    public void Tab_In_Rows_Is_Left_To_The_Browser()
    {
        _js.Execute("key(trigger, 'ArrowDown'); key(rows[0], 'Tab');");

        Assert.Equal("row0", Focused);
        Assert.Equal("false", Eval("prevented"));
        Assert.Equal("true", Attr("trigger", "aria-expanded"));
    }

    [Fact]
    public void Escape_Closes_Dropdown_And_Returns_Focus_To_Trigger()
    {
        _js.Execute("key(trigger, 'ArrowDown'); key(rows[0], 'ArrowDown'); press('Escape', rows[1]);");

        Assert.Equal("false", Attr("trigger", "aria-expanded"));
        Assert.Equal("trigger", Focused);
    }

    [Fact]
    public void Click_Away_Closes_Dropdown()
    {
        _js.Execute("clickOn(trigger); clickOn(outside);");
        Assert.Equal("false", Attr("trigger", "aria-expanded"));
    }

    [Fact]
    public void Click_On_Another_Nav_Link_Closes_Dropdown()
    {
        _js.Execute("clickOn(trigger); clickOn(plainLink);");
        Assert.Equal("false", Attr("trigger", "aria-expanded"));
    }

    [Fact]
    public void Click_Inside_Dropdown_Keeps_It_Open()
    {
        _js.Execute("clickOn(trigger); docListeners.click({ target: rows[1] });");
        Assert.Equal("true", Attr("trigger", "aria-expanded"));
    }

    [Fact]
    public void Hover_Opens_On_Desktop_And_Leaving_Closes_It()
    {
        _js.Execute("item.listeners.mouseenter({});");
        Assert.Equal("true", Attr("trigger", "aria-expanded"));

        _js.Execute("item.listeners.mouseleave({});");
        Assert.Equal("false", Attr("trigger", "aria-expanded"));
    }

    [Fact]
    public void Click_After_Hover_Pins_It_Open()
    {
        _js.Execute("item.listeners.mouseenter({}); clickOn(trigger); item.listeners.mouseleave({});");
        Assert.Equal("true", Attr("trigger", "aria-expanded"));
    }

    [Fact]
    public void Hover_Does_Nothing_Below_Desktop()
    {
        _js.Execute("desktop = false; item.listeners.mouseenter({});");
        Assert.Equal("false", Attr("trigger", "aria-expanded"));
    }

    [Fact]
    public void Focus_Leaving_The_Dropdown_Closes_It_On_Desktop_Only()
    {
        _js.Execute("clickOn(trigger); item.listeners.focusout({ relatedTarget: plainLink });");
        Assert.Equal("false", Attr("trigger", "aria-expanded"));

        // Mobile accordion stays expanded while tabbing on.
        _js.Execute("desktop = false; clickOn(trigger); item.listeners.focusout({ relatedTarget: plainLink });");
        Assert.Equal("true", Attr("trigger", "aria-expanded"));
    }

    // --- sheet ---

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
    public void Close_Button_Closes_The_Sheet_And_Focuses_Hamburger()
    {
        _js.Execute("clickOn(toggle); clickOn(closeBtn);");

        Assert.Equal("false", Attr("toggle", "aria-expanded"));
        Assert.Equal("false", Attr("root", "data-menu-open"));
        Assert.Equal("toggle", Focused);
    }

    [Fact]
    public void Escape_Closes_Accordion_Before_Sheet()
    {
        _js.Execute("desktop = false; clickOn(toggle); clickOn(trigger); press('Escape', trigger);");
        Assert.Equal("false", Attr("trigger", "aria-expanded"));
        Assert.Equal("true", Attr("toggle", "aria-expanded"));

        _js.Execute("press('Escape', trigger);");
        Assert.Equal("false", Attr("toggle", "aria-expanded"));
        Assert.Equal("toggle", Focused);
    }

    [Fact]
    public void Escape_Outside_The_Nav_Closes_Quietly()
    {
        _js.Execute("clickOn(toggle); clickOn(trigger); press('Escape', outside);");

        Assert.Equal("false", Attr("trigger", "aria-expanded"));
        Assert.Equal("false", Attr("toggle", "aria-expanded"));
        Assert.Null(Focused);
    }

    [Fact]
    public void Focus_Leaving_The_Nav_Closes_Everything()
    {
        _js.Execute("clickOn(toggle); clickOn(trigger); root.listeners.focusout({ relatedTarget: outside });");

        Assert.Equal("false", Attr("trigger", "aria-expanded"));
        Assert.Equal("false", Attr("toggle", "aria-expanded"));
    }

    // --- carousel ---

    [Fact]
    public void Next_And_Prev_Move_One_Slide_And_Wrap()
    {
        _js.Execute("next.click();");
        Assert.Equal("1", Eval("visible()"));
        Assert.Equal("1", Eval("currentDot()"));

        _js.Execute("next.click(); next.click();");
        Assert.Equal("0", Eval("visible()"));

        _js.Execute("prev.click();");
        Assert.Equal("2", Eval("visible()"));
        Assert.Equal("2", Eval("currentDot()"));
    }

    [Fact]
    public void Dot_Jumps_To_Its_Slide()
    {
        _js.Execute("dots[2].click();");
        Assert.Equal("2", Eval("visible()"));
        Assert.Equal("2", Eval("currentDot()"));
        Assert.Null(Attr("dots[0]", "aria-current"));
    }

    [Fact]
    public void Carousel_Never_Auto_Advances()
    {
        Assert.Equal("0", Eval("intervals"));
        Assert.Equal("0", Eval("visible()"));
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
