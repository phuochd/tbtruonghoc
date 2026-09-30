#nullable enable

using System.IO;
using Jint;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 6.2: runs the real <c>wwwroot/assets/js/site-a.js</c> in Jint
/// against a stubbed DOM shaped like <c>Views/Cms/SiteAProductHub.cshtml</c>'s
/// <c>category-search</c>: a search input, two filter chips and four tiles
/// (the placeholder tile carries no <c>data-sa-cat-tile</c>, so it is not
/// part of the stub). Same approach as <see cref="SiteAScriptTests"/>.
/// </summary>
public class SiteACatalogScriptTests
{
    private readonly Engine _js = new();

    public SiteACatalogScriptTests()
    {
        _js.Execute(@"
            var window = {};
            function element(name) {
                return {
                    name: name,
                    attrs: {},
                    listeners: {},
                    value: '',
                    textContent: '',
                    getAttribute: function (n) { return n in this.attrs ? this.attrs[n] : null; },
                    setAttribute: function (n, v) { this.attrs[n] = String(v); },
                    removeAttribute: function (n) { delete this.attrs[n]; },
                    addEventListener: function (type, fn) { this.listeners[type] = fn; }
                };
            }
            function tile(name, text, groups) {
                var el = element(name);
                el.setAttribute('data-sa-search', text);
                el.setAttribute('data-sa-groups', JSON.stringify(groups));
                return el;
            }

            var input = element('input');
            var empty = element('empty');
            empty.setAttribute('data-message', 'Không tìm thấy nhóm sản phẩm phù hợp.');
            var chipMamNon = element('chipMamNon');
            chipMamNon.setAttribute('aria-pressed', 'false');
            chipMamNon.setAttribute('data-sa-cat-chip', 'mầm non');
            var chipCongNghe = element('chipCongNghe');
            chipCongNghe.setAttribute('aria-pressed', 'false');
            chipCongNghe.setAttribute('data-sa-cat-chip', 'thiết bị công nghệ');
            var tiles = [
                tile('du', 'Dù che nắng sân trường Dù che, mái che di động', ['ngoài trời']),
                tile('noiThat', 'Nội thất mầm non Bàn ghế, tủ kệ', ['mầm non']),
                tile('bang', 'Bảng tương tác Bảng thông minh', ['thiết bị công nghệ']),
                tile('ngoaiTroi', 'Thiết bị mầm non ngoài trời Đồ chơi vận động', ['mầm non', 'ngoài trời'])
            ];

            var catalog = element('catalog');
            catalog.querySelector = function (sel) {
                if (sel === '[data-sa-cat-search]') { return input; }
                if (sel === '[data-sa-cat-empty]') { return empty; }
                return null;
            };
            catalog.querySelectorAll = function (sel) {
                if (sel === '[data-sa-cat-chip]') { return [chipMamNon, chipCongNghe]; }
                if (sel === '[data-sa-cat-tile]') { return tiles; }
                return [];
            };

            var document = {
                readyState: 'complete',
                addEventListener: function () {},
                querySelector: function () { return null; },
                querySelectorAll: function (sel) { return sel === '[data-sa-catalog]' ? [catalog] : []; }
            };

            function type(text) { input.value = text; input.listeners.input({ target: input }); }
            function visible() {
                var out = [];
                for (var i = 0; i < tiles.length; i++) {
                    if (tiles[i].getAttribute('hidden') === null) { out.push(tiles[i].name); }
                }
                return out.join(',');
            }
        ");
        _js.Execute(File.ReadAllText(ScriptPath()));
    }

    private static string ScriptPath()
    {
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TbTruongHoc.sln")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", "TbTruongHoc.Web", "wwwroot", "assets", "js", "site-a.js");
    }

    private string Eval(string expression) => _js.Evaluate(expression).ToString();

    [Fact]
    public void Nothing_Is_Filtered_Before_Input()
    {
        Assert.Equal("du,noiThat,bang,ngoaiTroi", Eval("visible()"));
        Assert.Equal("", Eval("empty.textContent"));
    }

    // Matrix: Search.
    [Theory]
    [InlineData("du che", "du")]
    [InlineData("DÙ CHE", "du")]
    [InlineData("  mam   non ", "noiThat,ngoaiTroi")]
    [InlineData("do choi", "ngoaiTroi")]
    [InlineData("", "du,noiThat,bang,ngoaiTroi")]
    public void Search_Is_Case_And_Diacritic_Insensitive(string query, string expected)
    {
        _js.Execute($"type({System.Text.Json.JsonSerializer.Serialize(query)});");
        Assert.Equal(expected, Eval("visible()"));
        Assert.Equal("", Eval("empty.textContent"));
    }

    [Fact]
    public void No_Match_Fills_The_Live_Region_And_A_Match_Clears_It()
    {
        _js.Execute("type('xyz khong co');");
        Assert.Equal("", Eval("visible()"));
        Assert.Equal("Không tìm thấy nhóm sản phẩm phù hợp.", Eval("empty.textContent"));

        _js.Execute("type('bang');");
        Assert.Equal("bang", Eval("visible()"));
        Assert.Equal("", Eval("empty.textContent"));
    }

    [Fact]
    public void Chips_Toggle_Aria_Pressed_And_Are_Ored_Together()
    {
        _js.Execute("chipMamNon.listeners.click({ target: chipMamNon });");
        Assert.Equal("true", Eval("chipMamNon.getAttribute('aria-pressed')"));
        Assert.Equal("noiThat,ngoaiTroi", Eval("visible()"));

        _js.Execute("chipCongNghe.listeners.click({ target: chipCongNghe });");
        Assert.Equal("noiThat,bang,ngoaiTroi", Eval("visible()"));

        _js.Execute("chipMamNon.listeners.click({ target: chipMamNon });");
        Assert.Equal("false", Eval("chipMamNon.getAttribute('aria-pressed')"));
        Assert.Equal("bang", Eval("visible()"));

        _js.Execute("chipCongNghe.listeners.click({ target: chipCongNghe });");
        Assert.Equal("du,noiThat,bang,ngoaiTroi", Eval("visible()"));
    }

    [Fact]
    public void Search_And_Chips_Are_Anded()
    {
        _js.Execute("chipMamNon.listeners.click({ target: chipMamNon }); chipCongNghe.listeners.click({ target: chipCongNghe });");
        _js.Execute("type('ngoai troi');");
        Assert.Equal("ngoaiTroi", Eval("visible()"));

        _js.Execute("type('du che');");
        Assert.Equal("", Eval("visible()"));
        Assert.Equal("Không tìm thấy nhóm sản phẩm phù hợp.", Eval("empty.textContent"));
    }

    [Fact]
    public void Fold_Maps_D_Stroke_And_Vietnamese_Marks()
    {
        Assert.Equal("dung cu thi nghiem ly hoa sinh", Eval("window.siteA.fold('Dụng cụ thí nghiệm Lý  Hóa Sinh')"));
        Assert.Equal("do dung day hoc", Eval("window.siteA.fold('Đồ dùng dạy học')"));
        Assert.Equal("uu o", Eval("window.siteA.fold('ƯỪ Ơ')"));
    }
}
