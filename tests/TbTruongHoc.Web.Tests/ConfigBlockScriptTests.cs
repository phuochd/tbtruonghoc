#nullable enable

using System.IO;
using Jint;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 4.2 (FR-11): runs the real <c>sb-config.js</c> + <c>lead-form.js</c>
/// in Jint against a stubbed DOM shaped like <c>ConfigBlock.cshtml</c> and
/// <c>_QuoteRequestForm.cshtml</c>, and captures the JSON body the quote form
/// would POST. Covers the matrix rows "Selections submitted", "Nothing
/// selected" and "Over length". Same approach as
/// <see cref="LandingPageScriptTests"/>; no database.
/// </summary>
public class ConfigBlockScriptTests
{
    private readonly Engine _js = new();

    public ConfigBlockScriptTests()
    {
        _js.Execute(@"
            var sent = null;
            var submitHandler = null;
            var AbortSignal = { timeout: function () { return null; } };
            function thenable() {
                return { then: function () { return thenable(); }, 'catch': function () { return thenable(); } };
            }
            function fetch(url, opts) { sent = JSON.parse(opts.body); return thenable(); }

            function control(tagName, type, value) {
                return { tagName: tagName, type: type, value: value || '', checked: false };
            }
            // Only the selectors sb-config.js is meant to use match; any
            // other selector finds nothing, so selector drift fails a test.
            var SELECTORS = {
                'select': function (c) { return c.tagName === 'SELECT'; },
                'input[type=""text""]': function (c) { return c.tagName === 'INPUT' && c.type === 'text'; },
                'input[type=""checkbox""]': function (c) { return c.tagName === 'INPUT' && c.type === 'checkbox'; }
            };
            function matching(controls, sel) {
                var test = SELECTORS[sel];
                return test ? controls.filter(test) : [];
            }
            function row(kind, label, controls) {
                return {
                    kind: kind,
                    getAttribute: function (n) { return n === 'data-sb-config-row' ? kind : n === 'data-label' ? label : null; },
                    querySelector: function (sel) { return matching(controls, sel)[0] || null; },
                    querySelectorAll: function (sel) { return matching(controls, sel); }
                };
            }
            var size = control('SELECT', 'select-one');
            var loai = control('SELECT', 'select-one');
            var wheels = control('INPUT', 'checkbox', 'Có');
            var paint = control('INPUT', 'text');
            var art = [control('INPUT', 'checkbox', 'Rồng'), control('INPUT', 'checkbox', 'Chữ')];
            var rows = [
                row('option', 'Kích thước', [size]),
                row('option', 'Loại', [loai]),
                row('check', 'Bánh xe', [wheels]),
                row('text', 'Sơn', [paint]),
                row('multi', 'Vẽ mặt trống', art)
            ];
            var section = { querySelectorAll: function (sel) { return sel === '[data-sb-config-row]' ? rows : []; } };
            var hasSection = true;

            var fields = {
                name: { value: 'Anh Ba' }, phone: { value: '0912345678' },
                productOfInterest: { value: 'Trống 60' }, message: { value: '' },
                formType: { value: 'general' }
            };
            var form = {
                addEventListener: function (t, fn) { submitHandler = fn; },
                querySelector: function (sel) {
                    var m = /name=""([^""]+)""/.exec(sel);
                    return m ? fields[m[1]] || null : null;
                },
                querySelectorAll: function () { return []; }
            };
            var container = {
                querySelector: function (sel) { return sel === 'form' ? form : null; }
            };
            var document = {
                readyState: 'complete',
                querySelectorAll: function () { return [container]; },
                querySelector: function (sel) { return sel === '[data-sb-config]' && hasSection ? section : null; }
            };
            var window = {};
            function submit() { submitHandler({ preventDefault: function () {} }); }");
        var js = Path.Combine(FindRepoRoot(), "src", "TbTruongHoc.Web", "wwwroot", "assets", "js");
        _js.Execute(File.ReadAllText(Path.Combine(js, "sb-config.js")));
        _js.Execute(File.ReadAllText(Path.Combine(js, "lead-form.js")));
    }

    private string SentMessage => _js.Evaluate("sent.message").AsString();

    [Fact]
    public void Selections_Are_Prepended_To_The_Typed_Message()
    {
        _js.Execute(@"
            size.value = '60cm'; loai.value = '2'; wheels.checked = true; paint.value = '  đỏ ';
            fields.message.value = 'Giao trước Tết';
            submit();");

        Assert.Equal("Cấu hình đã chọn: Kích thước: 60cm; Loại: 2; Bánh xe: Có; Sơn: đỏ\n\nGiao trước Tết", SentMessage);
        // The Lời nhắn box itself is untouched.
        Assert.Equal("Giao trước Tết", _js.Evaluate("fields.message.value").AsString());
    }

    [Fact]
    public void Multi_Option_Values_Are_Joined_And_Blank_Rows_Skipped()
    {
        _js.Execute("art[0].checked = true; art[1].checked = true; paint.value = '   '; submit();");

        Assert.Equal("Cấu hình đã chọn: Vẽ mặt trống: Rồng, Chữ", SentMessage);
    }

    [Fact]
    public void Nothing_Selected_Sends_The_Typed_Message_Unchanged()
    {
        _js.Execute("fields.message.value = 'Chỉ hỏi giá'; submit();");

        Assert.Equal("Chỉ hỏi giá", SentMessage);
    }

    [Fact]
    public void Page_Without_A_Config_Block_Sends_The_Typed_Message_Unchanged()
    {
        _js.Execute("hasSection = false; size.value = '60cm'; fields.message.value = 'x'; submit();");

        Assert.Equal("x", SentMessage);
    }

    [Fact]
    public void Over_Length_Truncates_The_Summary_And_Keeps_The_Typed_Message_Whole()
    {
        var typed = new string('m', 1900);
        _js.Execute($"paint.value = '{new string('s', 500)}'; fields.message.value = '{typed}'; submit();");

        var message = SentMessage;
        Assert.Equal(2000, message.Length);
        Assert.StartsWith("Cấu hình đã chọn: Sơn: sss", message);
        Assert.EndsWith("…\n\n" + typed, message);
    }

    [Fact]
    public void Typed_Message_Too_Long_For_Any_Summary_Is_Sent_As_Typed()
    {
        var typed = new string('m', 1990);
        _js.Execute($"size.value = '60cm'; fields.message.value = '{typed}'; submit();");

        Assert.Equal(typed, SentMessage);
    }

    [Fact]
    public void Js_Message_Max_Matches_The_Lead_Dto_Limit()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "TbTruongHoc.Web", "wwwroot", "assets", "js", "lead-form.js"));
        var match = System.Text.RegularExpressions.Regex.Match(js, @"var MESSAGE_MAX = (\d+);");
        Assert.True(match.Success, "Expected MESSAGE_MAX in lead-form.js.");

        var attribute = (System.ComponentModel.DataAnnotations.StringLengthAttribute?)System.Attribute.GetCustomAttribute(
            typeof(TbTruongHoc.Web.Models.LeadSubmissionRequest).GetProperty(nameof(TbTruongHoc.Web.Models.LeadSubmissionRequest.Message))!,
            typeof(System.ComponentModel.DataAnnotations.StringLengthAttribute));
        Assert.NotNull(attribute);
        Assert.Equal(attribute!.MaximumLength, int.Parse(match.Groups[1].Value));
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
