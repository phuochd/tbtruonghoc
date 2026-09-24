using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TbTruongHoc.Web.Notifications;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Boots the real TbTruongHoc.Web app in-process (real Piranha DI graph,
/// real MariaDB connection from appsettings.json/appsettings.Development.json
/// - the same MariaDB the docker-compose "mariadb" service exposes on host
/// port 3307). The only provider swapped out is Story 1.7's outbound email
/// sender, replaced with <see cref="RecordingEmailSender"/> so no test ever
/// opens a real SMTP connection; everything else deliberately exercises the
/// exact same startup path (including <see cref="TbTruongHoc.Web.Data.SiteSeed"/>
/// and Piranha's own hostname-resolution code) that runs in production.
/// </summary>
public class PiranhaWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// The one fake sender the whole shared test host uses (see
    /// <see cref="PiranhaAppCollection"/> - there is only ever one factory).
    /// </summary>
    public RecordingEmailSender EmailSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Explicit for clarity - Microsoft.AspNetCore.Mvc.Testing defaults to
        // "Development" already, which is what makes appsettings.Development.json's
        // tbtruonghoc.local / trongdoitam.local hostname mapping apply.
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<INotificationEmailSender>();
            services.AddSingleton<INotificationEmailSender>(EmailSender);
        });
    }
}
