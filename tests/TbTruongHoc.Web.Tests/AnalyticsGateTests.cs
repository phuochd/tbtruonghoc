#nullable enable

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TbTruongHoc.Web.Models;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 1.10: <see cref="AnalyticsGate"/> in isolation. Covers the
/// "Production, invalid ID" matrix row, which the HTTP-level tests cannot
/// reach because the OnBeforeSave hook refuses to persist an invalid ID.
/// </summary>
public class AnalyticsGateTests
{
    [Theory]
    [InlineData("Production", "G-ABCDE12345", true)]
    [InlineData("Production", "", false)]
    [InlineData("Production", null, false)]
    [InlineData("Production", "G-X", false)]
    [InlineData("Production", "G-ABC'; alert(1)", false)]
    [InlineData("Development", "G-ABCDE12345", false)]
    [InlineData("Staging", "G-ABCDE12345", false)]
    [InlineData("production-like", "G-ABCDE12345", false)]
    public void Loads_Only_In_Production_With_A_Valid_Id(string environment, string? ga4Id, bool expected)
    {
        Assert.Equal(expected, AnalyticsGate.ShouldLoadGa4(new StubEnvironment(environment), ga4Id));
    }

    private sealed class StubEnvironment : IHostEnvironment
    {
        public StubEnvironment(string name) => EnvironmentName = name;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "TbTruongHoc.Web";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
