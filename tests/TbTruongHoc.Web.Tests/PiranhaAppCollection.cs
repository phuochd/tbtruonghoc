using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Piranha's static <c>App</c> module registry (<c>App.Init(...)</c>, called
/// from Program.cs inside <c>app.UsePiranha</c>) is a process-wide singleton
/// that cannot be initialized twice in the same process - a second
/// WebApplicationFactory-driven app startup throws
/// ("Sequence contains more than one matching element") because it re-scans
/// and re-registers the same modules (e.g. the Manager module) into the
/// already-populated list. All tests that boot the app via
/// <see cref="PiranhaWebApplicationFactory"/> therefore share a single
/// xUnit collection so only one instance of the factory - and hence one
/// App.Init() call - exists for the whole test run.
/// </summary>
[CollectionDefinition(Name)]
public class PiranhaAppCollection : ICollectionFixture<PiranhaWebApplicationFactory>
{
    public const string Name = "Piranha app (shared, in-process)";
}
