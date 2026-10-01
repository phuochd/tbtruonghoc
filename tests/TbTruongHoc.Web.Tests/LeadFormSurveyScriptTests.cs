#nullable enable

using System.IO;
using Jint;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 6.5: runs the real <c>lead-form.js</c> in Jint against a stubbed
/// <c>_SurveyRequestForm.cshtml</c> (formType survey + locationAddress, the
/// location field's aria-describedby listing its error slot first and the
/// warning banner after it). Covers the matrix row "Missing location": the
/// server's LocationAddress error lands under địa điểm, values are kept.
/// </summary>
public class LeadFormSurveyScriptTests
{
    private readonly Engine _js = new();

    public LeadFormSurveyScriptTests()
    {
        _js.Execute(@"
            var sent = null;
            var submitHandler = null;
            var response = { ok: true, status: 200 };
            var AbortSignal = { timeout: function () { return null; } };
            function fetch(url, opts) {
                sent = JSON.parse(opts.body);
                return Promise.resolve(response);
            }

            function input(name, value, describedBy) {
                var attrs = { 'aria-invalid': 'false' };
                if (describedBy) { attrs['aria-describedby'] = describedBy; }
                return {
                    name: name, value: value, attrs: attrs,
                    getAttribute: function (n) { return n in this.attrs ? this.attrs[n] : null; },
                    setAttribute: function (n, v) { this.attrs[n] = String(v); }
                };
            }
            var fields = {
                formType: input('formType', 'survey'),
                productOfInterest: input('productOfInterest', 'Dù che sân trường học', 'surveyProductError surveyProductHint'),
                locationAddress: input('locationAddress', '', 'surveyLocationError surveyLocationWarning'),
                name: input('name', 'Cô Lan', 'surveyNameError'),
                phone: input('phone', '0912345678', 'surveyPhoneError')
            };
            var slots = {
                surveyLocationError: { textContent: '' },
                surveyLocationWarning: { textContent: 'warning text' },
                surveyNameError: { textContent: '' },
                surveyPhoneError: { textContent: '' },
                surveyProductError: { textContent: '' }
            };
            var submitErrorEl = { textContent: '' };
            var submitButton = { disabled: false };
            var form = {
                addEventListener: function (t, fn) { submitHandler = fn; },
                querySelector: function (sel) {
                    if (sel === 'button[type=""submit""]') { return submitButton; }
                    var m = /name=""([^""]+)""/.exec(sel);
                    return m ? fields[m[1]] || null : null;
                },
                querySelectorAll: function (sel) {
                    if (sel === 'input, textarea') { return [fields.productOfInterest, fields.locationAddress, fields.name, fields.phone]; }
                    if (sel === '.quote-request-form__error') { return [slots.surveyLocationError, slots.surveyNameError, slots.surveyPhoneError, slots.surveyProductError]; }
                    return [];
                }
            };
            var container = {
                innerHTML: 'form',
                querySelector: function (sel) {
                    if (sel === 'form') { return form; }
                    if (sel === '.quote-request-form__submit-error') { return submitErrorEl; }
                    return null;
                }
            };
            var document = {
                readyState: 'complete',
                querySelectorAll: function () { return [container]; },
                querySelector: function () { return null; },
                getElementById: function (id) { return slots[id] || null; }
            };
            var window = {};
            function submit() { submitHandler({ preventDefault: function () {} }); }");
        var js = Path.Combine(FindRepoRoot(), "src", "TbTruongHoc.Web", "wwwroot", "assets", "js");
        _js.Execute(File.ReadAllText(Path.Combine(js, "lead-form.js")));
    }

    [Fact]
    public void Survey_Payload_Carries_Location_And_Survey_Form_Type()
    {
        _js.Execute("fields.locationAddress.value = 'Quận 1, TP. Hồ Chí Minh'; submit();");

        Assert.Equal("survey", _js.Evaluate("sent.formType").AsString());
        Assert.Equal("Quận 1, TP. Hồ Chí Minh", _js.Evaluate("sent.locationAddress").AsString());
        Assert.Equal("Dù che sân trường học", _js.Evaluate("sent.productOfInterest").AsString());
    }

    // Matrix: Missing location (400 -> error under địa điểm, values kept).
    [Fact]
    public void Location_Error_Lands_In_The_First_Described_By_Slot_And_Values_Are_Kept()
    {
        _js.Execute(@"
            response = { ok: false, status: 400, json: function () {
                return Promise.resolve({ errors: { LocationAddress: ['Vui lòng nhập địa điểm / địa chỉ lắp đặt.'] } });
            } };
            submit();");

        Assert.Equal("Vui lòng nhập địa điểm / địa chỉ lắp đặt.", _js.Evaluate("slots.surveyLocationError.textContent").AsString());
        Assert.Equal("warning text", _js.Evaluate("slots.surveyLocationWarning.textContent").AsString());
        Assert.Equal("true", _js.Evaluate("fields.locationAddress.getAttribute('aria-invalid')").AsString());
        Assert.Equal("Cô Lan", _js.Evaluate("fields.name.value").AsString());
        Assert.Equal("0912345678", _js.Evaluate("fields.phone.value").AsString());
        Assert.Equal("form", _js.Evaluate("container.innerHTML").AsString());
        Assert.False(_js.Evaluate("submitButton.disabled").AsBoolean());
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
