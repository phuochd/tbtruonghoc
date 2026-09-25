#nullable enable

using System.IO;
using Jint;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 1.10: runs the real <c>wwwroot/assets/js/analytics-consent.js</c> in
/// Jint against a stubbed DOM and localStorage, covering the spec's consent
/// matrix rows (first visit, accept, returning, decline, expiry, reopen,
/// storage unavailable). "A GA4 request was made" is observed as a script
/// element injected into document.head - the only way the file ever reaches
/// Google. No database, so no shared-app collection.
/// </summary>
public class AnalyticsConsentScriptTests
{
    private const string Ga4Id = "G-TEST12345";

    private Engine _js = new();

    private void Boot(string? storedJs = null, bool storageThrows = false, string? ga4Id = Ga4Id)
    {
        _js = new Engine();
        _js.SetValue("storageThrows", storageThrows);
        _js.SetValue("scriptGa4Id", ga4Id);
        _js.Execute(@"
            var window = {};
            var injected = [];
            var focused = null;
            function element(name) {
                return {
                    name: name,
                    hidden: true,
                    listeners: {},
                    addEventListener: function (type, fn) { this.listeners[type] = fn; },
                    click: function () { if (this.listeners.click) { this.listeners.click(); } },
                    focus: function () { focused = this.name; }
                };
            }
            var acceptButton = element('accept');
            var declineButton = element('decline');
            var reopenLink = element('reopen');
            var bannerEl = element('banner');
            bannerEl.offsetHeight = 72;
            bannerEl.querySelector = function (sel) {
                return sel === '[data-consent-accept]' ? acceptButton
                    : sel === '[data-consent-decline]' ? declineButton : null;
            };
            var store = {};
            window.localStorage = {
                getItem: function (k) { if (storageThrows) { throw new Error('denied'); } return k in store ? store[k] : null; },
                setItem: function (k, v) { if (storageThrows) { throw new Error('denied'); } store[k] = String(v); }
            };
            var document = {
                readyState: 'complete',
                currentScript: { getAttribute: function (n) { return n === 'data-ga4-id' ? scriptGa4Id : null; } },
                head: { appendChild: function (el) { injected.push(el); } },
                body: { style: { paddingBottom: '' } },
                createElement: function (tag) { return { tagName: tag }; },
                addEventListener: function () {},
                querySelector: function (sel) {
                    return sel === '[data-consent-banner]' ? bannerEl
                        : sel === '[data-consent-reopen]' ? reopenLink : null;
                }
            };");
        if (storedJs != null)
        {
            _js.Execute($"store['analyticsConsent'] = JSON.stringify({storedJs});");
        }
        _js.Execute(File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "TbTruongHoc.Web", "wwwroot", "assets", "js", "analytics-consent.js")));
    }

    private bool Eval(string expr) => _js.Evaluate(expr).AsBoolean();

    private int InjectedCount => (int)_js.Evaluate("injected.length").AsNumber();

    private string? StoredChoice =>
        _js.Evaluate("store.analyticsConsent ? JSON.parse(store.analyticsConsent).choice : null").ToObject() as string;

    [Fact]
    public void First_Visit_Shows_Banner_And_Sends_Nothing()
    {
        Boot();

        Assert.False(Eval("bannerEl.hidden"));
        Assert.Equal(0, InjectedCount);
        Assert.True(Eval("window.dataLayer === undefined"));
        Assert.True(Eval("focused === null"), "The banner must not steal focus on page load.");
    }

    [Fact]
    public void Accept_Stores_Choice_Hides_Banner_And_Loads_Ga4_For_This_Site()
    {
        Boot();

        _js.Execute("acceptButton.click();");

        Assert.True(Eval("bannerEl.hidden"));
        Assert.Equal("granted", StoredChoice);
        Assert.Equal(1, InjectedCount);
        Assert.Equal($"https://www.googletagmanager.com/gtag/js?id={Ga4Id}", _js.Evaluate("injected[0].src").AsString());
        Assert.True(Eval("injected[0].async === true"));
        Assert.True(Eval($"window.dataLayer[1][0] === 'config' && window.dataLayer[1][1] === '{Ga4Id}'"));
    }

    [Fact]
    public void Accepting_Twice_Injects_Ga4_Only_Once()
    {
        Boot();

        _js.Execute("acceptButton.click(); window.analyticsConsent.reopen(); acceptButton.click();");

        Assert.Equal(1, InjectedCount);
    }

    [Fact]
    public void Returning_Accepted_Visitor_Loads_Ga4_Without_Banner()
    {
        Boot("{ choice: 'granted', at: Date.now() - 1000 }");

        Assert.True(Eval("bannerEl.hidden"));
        Assert.Equal(1, InjectedCount);
    }

    [Fact]
    public void Decline_Stores_Choice_Hides_Banner_And_Sends_Nothing()
    {
        Boot();

        _js.Execute("declineButton.click();");

        Assert.True(Eval("bannerEl.hidden"));
        Assert.Equal("denied", StoredChoice);
        Assert.Equal(0, InjectedCount);
    }

    [Fact]
    public void Returning_Declined_Visitor_Sees_No_Banner_And_Sends_Nothing()
    {
        Boot("{ choice: 'denied', at: Date.now() - 1000 }");

        Assert.True(Eval("bannerEl.hidden"));
        Assert.Equal(0, InjectedCount);
    }

    [Theory]
    [InlineData("granted")]
    [InlineData("denied")]
    public void Expired_Choice_Is_Treated_As_First_Visit(string choice)
    {
        Boot($"{{ choice: '{choice}', at: Date.now() - (180 * 24 * 60 * 60 * 1000) - 60000 }}");

        Assert.False(Eval("bannerEl.hidden"));
        Assert.Equal(0, InjectedCount);
    }

    [Theory]
    [InlineData("'not json'")]
    [InlineData("{ choice: 'maybe', at: Date.now() }")]
    [InlineData("{ choice: 'granted' }")]
    public void Unrecognized_Stored_Value_Fails_Closed_To_Banner(string storedJs)
    {
        Boot(storedJs);

        Assert.False(Eval("bannerEl.hidden"));
        Assert.Equal(0, InjectedCount);
    }

    [Fact]
    public void Reopen_Link_Shows_Banner_Focuses_Accept_And_New_Choice_Overwrites_Old()
    {
        Boot("{ choice: 'denied', at: Date.now() - 1000 }");

        _js.Execute("reopenLink.click();");

        Assert.False(Eval("bannerEl.hidden"));
        Assert.Equal("accept", _js.Evaluate("focused").AsString());

        _js.Execute("acceptButton.click();");

        Assert.Equal("granted", StoredChoice);
        Assert.Equal(1, InjectedCount);
    }

    [Fact]
    public void Declining_After_Accept_Stops_Ga4_On_Later_Page_Loads()
    {
        Boot("{ choice: 'granted', at: Date.now() - 1000 }");
        _js.Execute("reopenLink.click(); declineButton.click();");
        var stored = _js.Evaluate("store.analyticsConsent").AsString();

        Boot($"JSON.parse('{stored}')");

        Assert.True(Eval("bannerEl.hidden"));
        Assert.Equal(0, InjectedCount);
    }

    [Fact]
    public void Choice_Just_Under_180_Days_Is_Still_Honoured()
    {
        Boot("{ choice: 'granted', at: Date.now() - (179 * 24 * 60 * 60 * 1000) }");

        Assert.True(Eval("bannerEl.hidden"));
        Assert.Equal(1, InjectedCount);
    }

    [Theory]
    [InlineData("{ choice: 'granted', at: Date.now() + 1e12 }")]
    [InlineData("{ choice: 'granted', at: Date.now() + 60000 }")]
    public void Future_Timestamp_Is_Treated_As_No_Choice(string storedJs)
    {
        Boot(storedJs);

        Assert.False(Eval("bannerEl.hidden"));
        Assert.Equal(0, InjectedCount);
    }

    [Fact]
    public void Reopen_Link_Is_Revealed_Only_Once_Wired()
    {
        Boot("{ choice: 'denied', at: Date.now() - 1000 }");

        Assert.False(Eval("reopenLink.hidden"));
        Assert.True(Eval("typeof reopenLink.listeners.click === 'function'"));
    }

    [Theory]
    [InlineData("acceptButton")]
    [InlineData("declineButton")]
    public void Choice_From_Reopened_Banner_Returns_Focus_To_Reopen_Link(string button)
    {
        Boot("{ choice: 'denied', at: Date.now() - 1000 }");

        _js.Execute($"reopenLink.click(); {button}.click();");

        Assert.True(Eval("bannerEl.hidden"));
        Assert.Equal("reopen", _js.Evaluate("focused").AsString());
    }

    [Fact]
    public void Choice_From_First_Visit_Banner_Does_Not_Move_Focus()
    {
        Boot();

        _js.Execute("declineButton.click();");

        Assert.True(Eval("focused === null"));
    }

    [Fact]
    public void Body_Is_Padded_While_Banner_Is_Visible_And_Restored_After()
    {
        Boot();

        Assert.Equal("72px", _js.Evaluate("document.body.style.paddingBottom").AsString());

        _js.Execute("acceptButton.click();");

        Assert.Equal("", _js.Evaluate("document.body.style.paddingBottom").AsString());
    }

    [Fact]
    public void Storage_Unavailable_Shows_Banner_And_Accept_Still_Works_For_This_Page()
    {
        Boot(storageThrows: true);

        Assert.False(Eval("bannerEl.hidden"));
        Assert.Equal(0, InjectedCount);

        _js.Execute("acceptButton.click();");

        Assert.True(Eval("bannerEl.hidden"));
        Assert.Equal(1, InjectedCount);
    }

    [Fact]
    public void Storage_Unavailable_Decline_Sends_Nothing()
    {
        Boot(storageThrows: true);

        _js.Execute("declineButton.click();");

        Assert.True(Eval("bannerEl.hidden"));
        Assert.Equal(0, InjectedCount);
    }

    [Fact]
    public void Missing_Measurement_Id_Never_Injects()
    {
        Boot(ga4Id: null);

        _js.Execute("acceptButton.click();");

        Assert.Equal(0, InjectedCount);
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
