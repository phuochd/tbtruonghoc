#nullable enable

using System;
using System.IO;
using Jint;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 1.8 (AD-1): runs the real <c>wwwroot/assets/js/manager-page-tabs.js</c>
/// in Jint against a stubbed Vue, covering the spec's tab-selection matrix
/// rows without a Manager login. The sites arrays mimic what
/// <c>GET manager/api/page/list</c> returns (default site first); each
/// "reload" builds fresh objects, as piranha.pagelist's <c>load()</c> does.
/// No database, so no shared-app collection.
/// </summary>
public class ManagerPageTabsScriptTests
{
    private const string SiteA = "aaaaaaaa-0000-0000-0000-000000000001";
    private const string SiteB = "bbbbbbbb-0000-0000-0000-000000000002";
    private const string SiteC = "cccccccc-0000-0000-0000-000000000003";

    private readonly Engine _js;

    public ManagerPageTabsScriptTests()
    {
        _js = new Engine();
        _js.Execute(@"
            var window = {};
            var focusedId = null;
            var observed = null;
            var Vue = {
                prototype: {},
                observable: function (o) { observed = o; return o; },
                nextTick: function (fn) { fn(); }
            };
            var document = {
                getElementById: function (id) { return { focus: function () { focusedId = id; } }; }
            };
            function sites() {
                var ids = Array.prototype.slice.call(arguments);
                return ids.map(function (id) { return { id: id, title: id, pages: [] }; });
            }
            function key(k, mods) {
                var e = { key: k, prevented: false, preventDefault: function () { this.prevented = true; } };
                for (var m in (mods || {})) { e[m] = mods[m]; }
                return e;
            }");
        _js.Execute(File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "TbTruongHoc.Web", "wwwroot", "assets", "js", "manager-page-tabs.js")));
    }

    private string? Active(string sitesExpr) =>
        _js.Evaluate($"window.managerPageTabs.resolveId({sitesExpr})").ToObject() as string;

    private void Run(string script) => _js.Execute(script);

    [Fact]
    public void Two_Sites_First_Load_Activates_Default_Site_Only()
    {
        Assert.Equal(SiteA, Active($"sites('{SiteA}', '{SiteB}')"));
        Assert.True(_js.Evaluate($"window.managerPageTabs.isActive('{SiteA}', sites('{SiteA}', '{SiteB}'))").AsBoolean());
        Assert.False(_js.Evaluate($"window.managerPageTabs.isActive('{SiteB}', sites('{SiteA}', '{SiteB}'))").AsBoolean());
    }

    [Fact]
    public void Switch_Tab_Activates_Selected_Site_And_Hides_Other()
    {
        Run($"window.managerPageTabs.select('{SiteB}')");

        Assert.Equal(SiteB, Active($"sites('{SiteA}', '{SiteB}')"));
        Assert.False(_js.Evaluate($"window.managerPageTabs.isActive('{SiteA}', sites('{SiteA}', '{SiteB}'))").AsBoolean());
    }

    [Fact]
    public void Reload_After_Action_Keeps_Active_Site()
    {
        Run($"window.managerPageTabs.select('{SiteB}')");

        // load() after a delete/move/site edit replaces sites with fresh objects.
        Assert.Equal(SiteB, Active($"sites('{SiteA}', '{SiteB}')"));
    }

    [Fact]
    public void Drag_Drop_Transient_Empty_Sites_Does_Not_Lose_Active_Site()
    {
        Run($"window.managerPageTabs.select('{SiteB}')");

        // piranha.pagelist's move callback sets sites = [] before restoring them.
        Assert.Null(Active("[]"));
        Assert.Equal(SiteB, Active($"sites('{SiteA}', '{SiteB}')"));
    }

    [Fact]
    public void Active_Site_Removed_Falls_Back_To_First_Site()
    {
        Run($"window.managerPageTabs.select('{SiteB}')");

        Assert.Equal(SiteA, Active($"sites('{SiteA}')"));
    }

    [Fact]
    public void Site_Added_Appears_As_Tab_And_Keeps_Current_Selection()
    {
        Run($"window.managerPageTabs.select('{SiteB}')");

        Assert.Equal(SiteB, Active($"sites('{SiteA}', '{SiteB}', '{SiteC}')"));
        Assert.False(_js.Evaluate($"window.managerPageTabs.isActive('{SiteC}', sites('{SiteA}', '{SiteB}', '{SiteC}'))").AsBoolean());

        Run($"window.managerPageTabs.select('{SiteC}')");
        Assert.Equal(SiteC, Active($"sites('{SiteA}', '{SiteB}', '{SiteC}')"));
    }

    [Theory]
    [InlineData("ArrowRight", SiteB)]
    [InlineData("ArrowLeft", SiteC)]
    [InlineData("End", SiteC)]
    [InlineData("Home", SiteA)]
    public void Keyboard_Moves_Selection_And_Focus(string keyName, string expected)
    {
        var prevented = _js.Evaluate($@"(function () {{
                var e = key('{keyName}');
                window.managerPageTabs.onKeydown(e, sites('{SiteA}', '{SiteB}', '{SiteC}'));
                return e.prevented;
            }})()").AsBoolean();

        Assert.True(prevented);
        Assert.Equal(expected, Active($"sites('{SiteA}', '{SiteB}', '{SiteC}')"));
        Assert.Equal("site-tab-" + expected, _js.Evaluate("focusedId").AsString());
    }

    [Fact]
    public void Other_Keys_Are_Ignored()
    {
        var prevented = _js.Evaluate($@"(function () {{
                var e = key('Enter');
                window.managerPageTabs.onKeydown(e, sites('{SiteA}', '{SiteB}'));
                return e.prevented;
            }})()").AsBoolean();

        Assert.False(prevented);
        Assert.Equal(SiteA, Active($"sites('{SiteA}', '{SiteB}')"));
    }

    [Fact]
    public void State_Is_A_Vue_Observable_With_ActiveId()
    {
        Assert.True(_js.Evaluate("observed !== null && typeof observed === 'object' && 'activeId' in observed").AsBoolean(),
            "manager-page-tabs.js must create its state with Vue.observable({ activeId: ... }) so tab switches re-render.");
        Assert.True(_js.Evaluate("window.managerPageTabs === observed").AsBoolean(),
            "window.managerPageTabs must be the exact object returned by Vue.observable.");
    }

    [Fact]
    public void State_Is_Exposed_On_Vue_Prototype_For_The_Template()
    {
        // Vue 2 in-DOM templates cannot see window globals; without this the
        // Pages screen throws "Cannot read properties of undefined (reading 'isActive')".
        Assert.True(_js.Evaluate("Vue.prototype.managerPageTabs === observed").AsBoolean(),
            "manager-page-tabs.js must set Vue.prototype.managerPageTabs so piranha.pagelist's template can reach it.");
    }

    [Fact]
    public void ArrowRight_From_Last_Tab_Wraps_To_First()
    {
        Run($"window.managerPageTabs.select('{SiteC}')");

        var prevented = _js.Evaluate($@"(function () {{
                var e = key('ArrowRight');
                window.managerPageTabs.onKeydown(e, sites('{SiteA}', '{SiteB}', '{SiteC}'));
                return e.prevented;
            }})()").AsBoolean();

        Assert.True(prevented);
        Assert.Equal(SiteA, Active($"sites('{SiteA}', '{SiteB}', '{SiteC}')"));
    }

    [Fact]
    public void Keydown_With_Empty_Sites_Is_A_NoOp()
    {
        var prevented = _js.Evaluate(@"(function () {
                var e = key('ArrowRight');
                window.managerPageTabs.onKeydown(e, []);
                return e.prevented;
            })()").AsBoolean();

        Assert.False(prevented);
        Assert.True(_js.Evaluate("window.managerPageTabs.activeId === null").AsBoolean());
        Assert.True(_js.Evaluate("focusedId === null").AsBoolean());
    }

    [Fact]
    public void Modifier_Held_Arrow_Is_Ignored_And_Not_Prevented()
    {
        var prevented = _js.Evaluate($@"(function () {{
                var e = key('ArrowRight', {{ altKey: true }});
                window.managerPageTabs.onKeydown(e, sites('{SiteA}', '{SiteB}'));
                return e.prevented;
            }})()").AsBoolean();

        Assert.False(prevented);
        Assert.Equal(SiteA, Active($"sites('{SiteA}', '{SiteB}')"));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TbTruongHoc.sln")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new InvalidOperationException(
                $"Could not locate repo root (TbTruongHoc.sln) starting from {AppContext.BaseDirectory}");
        }

        return dir.FullName;
    }
}
