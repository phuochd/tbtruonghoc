#nullable enable

using System.IO;
using System.Text.Json;
using Jint;
using TbTruongHoc.Web.Models;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 6.5: runs the real <c>site-a.js</c> in Jint against a stubbed survey
/// form shaped like <c>_SurveyRequestForm.cshtml</c>, fed the real
/// <see cref="ServiceArea.OutsideTerms"/> (as the view does), plus the
/// <see cref="ServiceArea"/> matching rules themselves. No database.
/// </summary>
public class SiteASurveyScriptTests
{
    private readonly Engine _js = new();

    public SiteASurveyScriptTests()
    {
        var areas = JsonSerializer.Serialize(JsonSerializer.Serialize(ServiceArea.OutsideTerms));
        _js.Execute(@"
            var window = {};
            function element(name, attrs) {
                var el = {
                    name: name,
                    attrs: {},
                    classes: {},
                    listeners: {},
                    value: '',
                    disabled: false,
                    id: attrs && attrs.id,
                    getAttribute: function (n) { return n in this.attrs ? this.attrs[n] : null; },
                    setAttribute: function (n, v) { this.attrs[n] = String(v); },
                    removeAttribute: function (n) { delete this.attrs[n]; },
                    addEventListener: function (type, fn) { (this.listeners[type] = this.listeners[type] || []).push(fn); },
                    querySelector: function () { return null; },
                    querySelectorAll: function () { return []; }
                };
                el.classList = {
                    add: function (c) { el.classes[c] = true; },
                    remove: function (c) { delete el.classes[c]; },
                    contains: function (c) { return !!el.classes[c]; }
                };
                for (var k in attrs) { el.attrs[k] = attrs[k]; }
                return el;
            }
            function type(text) {
                field.value = text;
                var fns = field.listeners.input || [];
                for (var i = 0; i < fns.length; i++) { fns[i].call(field, { type: 'input', target: field }); }
            }

            var form = element('form', { 'data-sa-survey-areas': " + areas + @" });
            var field = element('field', { 'data-sa-survey-location': '', 'aria-describedby': 'surveyLocationError' });
            var banner = element('banner', { id: 'surveyLocationWarning', hidden: 'hidden' });
            var submit = element('submit', {});
            form.querySelector = function (sel) {
                if (sel === '[data-sa-survey-location]') { return field; }
                if (sel === '[data-sa-survey-warning]') { return banner; }
                if (sel === 'button[type=""submit""]') { return submit; }
                return null;
            };

            var document = {
                readyState: 'complete',
                addEventListener: function () {},
                getElementById: function () { return null; },
                querySelector: function () { return null; },
                querySelectorAll: function (sel) { return sel === '[data-sa-survey-areas]' ? [form] : []; }
            };");
        var js = Path.Combine(FindRepoRoot(), "src", "TbTruongHoc.Web", "wwwroot", "assets", "js");
        _js.Execute(File.ReadAllText(Path.Combine(js, "site-a.js")));
    }

    private string? Attr(string el, string name) => _js.Evaluate($"{el}.getAttribute('{name}')").ToObject() as string;

    private bool Warned => Attr("banner", "hidden") == null;

    // Matrix: Out-of-area survey (live banner + amber field, submit untouched).
    [Theory]
    [InlineData("Trường Tiểu học Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh")]
    [InlineData("Da Nang")]
    [InlineData("tp.hcm")]
    public void Out_Of_Area_Text_Shows_The_Warning_Without_Blocking(string text)
    {
        _js.Execute($"type({JsonSerializer.Serialize(text)});");

        Assert.True(Warned);
        Assert.True(_js.Evaluate("field.classList.contains('sa-survey-form__field--warn')").AsBoolean());
        Assert.Equal("surveyLocationError surveyLocationWarning", Attr("field", "aria-describedby"));
        Assert.False(_js.Evaluate("submit.disabled").AsBoolean());
        Assert.Null(Attr("submit", "disabled"));
    }

    // Matrix: In-area survey.
    [Fact]
    public void In_Area_Text_Shows_Nothing_And_Clearing_A_Warning_Restores_The_Field()
    {
        _js.Execute("type('Trường MN Hoa Sen, số 12 đường Lê Lợi, TP. Thanh Hóa');");
        Assert.False(Warned);

        _js.Execute("type('Sài Gòn');");
        Assert.True(Warned);
        Assert.True(_js.Evaluate("field.classList.contains('sa-survey-form__field--warn')").AsBoolean());
        Assert.Equal("surveyLocationError surveyLocationWarning", Attr("field", "aria-describedby"));

        _js.Execute("type('Hà Đông, Hà Nội');");
        Assert.False(Warned);
        Assert.False(_js.Evaluate("field.classList.contains('sa-survey-form__field--warn')").AsBoolean());
        Assert.Equal("surveyLocationError", Attr("field", "aria-describedby"));
    }

    [Theory]
    [InlineData("Quận 1, TP. Hồ Chí Minh", true)]
    [InlineData("Trường Tiểu học, Da Nang", true)]
    [InlineData("TPHCM", true)]
    [InlineData("Buôn Ma Thuột, ĐẮK LẮK", true)]
    [InlineData("Trường MN Hoa Sen, TP. Thanh Hóa", false)]
    [InlineData("Hà Đông, Hà Nội", false)]
    [InlineData("Vĩnh Yên, Vĩnh Phúc", false)]
    [InlineData("Trường MN Hoa Sen, đường Lê Lợi", false)]
    [InlineData("Quảng Ninh", false)]
    [InlineData("Long Biên, Hà Nội", false)]
    [InlineData("Huyện Phù Yên, Sơn La", false)]
    [InlineData("Phường Tân An, Quảng Yên, Quảng Ninh", false)]
    [InlineData("Saigon", true)]
    [InlineData("Danang city", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Server_Reading_Matches_The_Page(string? text, bool outside)
    {
        Assert.Equal(outside, ServiceArea.IsOutside(text));

        _js.Execute($"type({JsonSerializer.Serialize(text ?? string.Empty)});");
        Assert.Equal(outside, Warned);
    }

    [Theory]
    [InlineData("TP. Hồ Chí Minh", "tp ho chi minh")]
    [InlineData("  ĐẮK  LẮK!! ", "dak lak")]
    [InlineData("Quận 1,Thủ Đức", "quan 1 thu duc")]
    [InlineData("Thừa Thiên Huế", "thua thien hue")]
    [InlineData("Buôn Ma Thuột", "buon ma thuot")]
    [InlineData("Ưu Ơn Ăn Ân Ê Ô Ỵ Ữ Ặ", "uu on an an e o y u a")]
    public void Fold_Is_The_Same_In_CSharp_And_Js(string text, string folded)
    {
        Assert.Equal(folded, ServiceArea.Fold(text));
        Assert.Equal(folded, _js.Evaluate($"window.siteA.foldPlace({JsonSerializer.Serialize(text)})").AsString());
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
