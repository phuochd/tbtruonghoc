using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Boots the real TbTruongHoc.Web app in-process (real Piranha DI graph,
/// real MariaDB connection from appsettings.json/appsettings.Development.json
/// - the same MariaDB the docker-compose "mariadb" service exposes on host
/// port 3307). No provider is swapped out: this deliberately exercises the
/// exact same startup path (including <see cref="TbTruongHoc.Web.Data.SiteSeed"/>
/// and Piranha's own hostname-resolution code) that runs in production.
/// </summary>
public class PiranhaWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Explicit for clarity - Microsoft.AspNetCore.Mvc.Testing defaults to
        // "Development" already, which is what makes appsettings.Development.json's
        // tbtruonghoc.local / trongdoitam.local hostname mapping apply.
        builder.UseEnvironment("Development");
    }
}
