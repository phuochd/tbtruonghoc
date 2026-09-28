#nullable enable

using System.IO;
using Jint;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 3.2: runs the real <c>wwwroot/assets/js/landing-page.js</c> in Jint
/// against a stubbed DOM shaped like <c>LandingPage.cshtml</c> (a variant
/// CTA with a child span, an outside element, the form's product field).
/// Same approach as <see cref="SiteBNavScriptTests"/>; no database.
/// </summary>
public class LandingPageScriptTests
{
    private readonly Engine _js = new();

    public LandingPageScriptTests()
    {
        _js.Execute(@"
            var docListeners = {};
            var prevented = false;
            var field = { value: 'Page title' };
            var fieldPresent = true;
            function element(attrs, parent) {
                return {
                    attrs: attrs,
                    parent: parent,
                    getAttribute: function (n) { return n in this.attrs ? this.attrs[n] : null; },
                    closest: function (sel) {
                        var el = this;
                        while (el) {
                            if (sel === 'a[data-lp-variant]' && 'data-lp-variant' in el.attrs) { return el; }
                            el = el.parent;
                        }
                        return null;
                    }
                };
            }
            var cta = element({ 'data-lp-variant': '2 ngựa – 5.500.000đ', href: '#dat-hang' }, null);
            var ctaChild = element({}, cta);
            var outside = element({}, null);
            var document = {
                readyState: 'complete',
                addEventListener: function (type, fn) { docListeners[type] = fn; },
                querySelector: function (sel) {
                    return fieldPresent && sel === '#dat-hang [name=""productOfInterest""]' ? field : null;
                }
            };
            function clickOn(el) {
                docListeners.click({ target: el, preventDefault: function () { prevented = true; } });
            }");
        _js.Execute(File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "TbTruongHoc.Web", "wwwroot", "assets", "js", "landing-page.js")));
    }

    private string FieldValue => _js.Evaluate("field.value").AsString();

    private bool Prevented => _js.Evaluate("prevented").AsBoolean();

    [Fact]
    public void Click_On_Cta_Sets_The_Product_Field_To_The_Prefill()
    {
        _js.Execute("clickOn(cta);");

        Assert.Equal("2 ngựa – 5.500.000đ", FieldValue);
        Assert.False(Prevented);
    }

    [Fact]
    public void Click_On_A_Child_Of_The_Cta_Sets_The_Product_Field()
    {
        _js.Execute("clickOn(ctaChild);");

        Assert.Equal("2 ngựa – 5.500.000đ", FieldValue);
        Assert.False(Prevented);
    }

    [Fact]
    public void Click_Outside_A_Variant_Cta_Leaves_The_Field_Unchanged()
    {
        _js.Execute("clickOn(outside);");

        Assert.Equal("Page title", FieldValue);
        Assert.False(Prevented);
    }

    [Fact]
    public void Click_After_The_Form_Was_Replaced_Does_Not_Throw()
    {
        _js.Execute("fieldPresent = false; clickOn(cta);");

        Assert.Equal("Page title", FieldValue);
        Assert.False(Prevented);
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
