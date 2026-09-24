using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using TbTruongHoc.Web.Notifications;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 1.7 (FR-3, AD-3): covers every row of the spec's I/O &amp; Edge-Case
/// Matrix. Composition and validation are tested directly; delivery goes
/// through real HTTP POSTs to <c>/api/leads</c> and waits on the shared
/// <see cref="RecordingEmailSender"/> (no real SMTP). Every test restores the
/// site's <see cref="SiteSettings.NotificationEmails"/> and deletes every
/// <see cref="FormSubmission"/> row it creates.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class LeadNotificationTests
{
    private const string LeadsEndpoint = "/api/leads";

    private readonly PiranhaWebApplicationFactory _factory;

    public LeadNotificationTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private RecordingEmailSender Sender => _factory.EmailSender;

    // ---------------------------------------------------------------------
    // Composer (pure) - subject format, Vietnam time, survey lines, CR/LF
    // ---------------------------------------------------------------------

    [Fact]
    public void Composer_Builds_Vietnamese_Subject_And_Vietnam_Time_Body()
    {
        var submission = new FormSubmission
        {
            SiteId = Guid.NewGuid(),
            FormType = "general",
            Name = "Nguyễn Văn A",
            Phone = "0901234567",
            ProductOfInterest = "Bàn học sinh",
            Message = "Xin báo giá.",
            // 17:30 UTC = 00:30 next day in Vietnam (UTC+7).
            CreatedAt = new DateTimeOffset(2026, 9, 24, 17, 30, 0, TimeSpan.Zero)
        };

        var email = LeadEmailComposer.Compose(submission, "TB Truong Hoc", new[] { "a@x.vn", "b@x.vn" });

        Assert.Equal("[TB Truong Hoc] Khách mới: Nguyễn Văn A – 0901234567", email.Subject);
        Assert.Equal(new[] { "a@x.vn", "b@x.vn" }, email.To);
        Assert.Contains("25/09/2026 00:30", email.Body);
        Assert.Contains("TB Truong Hoc", email.Body);
        Assert.Contains("Liên hệ / báo giá", email.Body);
        Assert.Contains("Nguyễn Văn A", email.Body);
        Assert.Contains("0901234567", email.Body);
        Assert.Contains("Bàn học sinh", email.Body);
        Assert.Contains("Xin báo giá.", email.Body);
        Assert.DoesNotContain("Địa chỉ:", email.Body);
        Assert.DoesNotContain("Khu vực phục vụ", email.Body);
    }

    [Fact]
    public void Composer_Includes_Survey_Address_And_Service_Area_When_Set()
    {
        var submission = new FormSubmission
        {
            FormType = "survey",
            Name = "Khách khảo sát",
            Phone = "0909",
            LocationAddress = "12 Lê Lợi, Quận 1",
            IsOutsideServiceArea = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var email = LeadEmailComposer.Compose(submission, "Site", new[] { "a@x.vn" });

        Assert.Contains("Khảo sát", email.Body);
        Assert.Contains("Địa chỉ: 12 Lê Lợi, Quận 1", email.Body);
        Assert.Contains("Khu vực phục vụ: Ngoài khu vực phục vụ", email.Body);

        submission.IsOutsideServiceArea = false;
        email = LeadEmailComposer.Compose(submission, "Site", new[] { "a@x.vn" });
        Assert.Contains("Khu vực phục vụ: Trong khu vực phục vụ", email.Body);
    }

    [Fact]
    public void Composer_Strips_CrLf_And_Control_Characters_From_Subject()
    {
        var submission = new FormSubmission
        {
            FormType = "general",
            Name = "Evil\r\nBcc: victim@x.vn",
            Phone = "090\n1\t2",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var email = LeadEmailComposer.Compose(submission, "Site\r\nX-Injected: 1", new[] { "a@x.vn" });

        Assert.DoesNotContain(email.Subject, c => char.IsControl(c));
        Assert.Equal("[Site X-Injected: 1] Khách mới: Evil Bcc: victim@x.vn – 090 1 2", email.Subject);
    }

    [Fact]
    public void Composer_Omits_Blank_Or_Whitespace_Address()
    {
        var submission = new FormSubmission
        {
            FormType = "survey",
            Name = "A",
            Phone = "1",
            LocationAddress = "   ",
            CreatedAt = DateTimeOffset.UtcNow
        };

        Assert.DoesNotContain("Địa chỉ:", LeadEmailComposer.Compose(submission, "Site", new[] { "a@x.vn" }).Body);

        submission.LocationAddress = string.Empty;
        Assert.DoesNotContain("Địa chỉ:", LeadEmailComposer.Compose(submission, "Site", new[] { "a@x.vn" }).Body);
    }

    [Fact]
    public void Composer_Collapses_Unicode_Whitespace_In_Subject_To_One_Line()
    {
        var submission = new FormSubmission
        {
            FormType = "general",
            Name = "An\u2028Bình\u2029Chi\tDũng\u00A0Em",
            Phone = "090",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var email = LeadEmailComposer.Compose(submission, "Site", new[] { "a@x.vn" });

        Assert.Equal("[Site] Khách mới: An Bình Chi Dũng Em – 090", email.Subject);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public void Composer_Falls_Back_To_Website_Title(string? siteTitle)
    {
        var submission = new FormSubmission { FormType = "general", Name = "A", Phone = "1", CreatedAt = DateTimeOffset.UtcNow };

        var email = LeadEmailComposer.Compose(submission, siteTitle, new[] { "a@x.vn" });

        Assert.StartsWith("[Website] Khách mới: ", email.Subject);
    }

    // ---------------------------------------------------------------------
    // Queue full - the POST must never block when the worker falls behind
    // ---------------------------------------------------------------------

    [Fact]
    public void Queue_Full_Returns_Immediately_And_Warns_Once()
    {
        var logger = new ListLogger<FormNotificationService>();
        var service = new FormNotificationService(logger); // no reader drains it

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < FormNotificationService.Capacity + 1; i++)
        {
            service.NotifyNewSubmission(new FormSubmission { Id = Guid.NewGuid(), FormType = "general", Name = "A", Phone = "1" });
        }
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Queueing took {stopwatch.Elapsed}.");
        Assert.Equal(FormNotificationService.Capacity, service.Reader.Count);
        Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("queue is full"));
    }

    // ---------------------------------------------------------------------
    // SMTP TLS selection - never a cleartext fallback
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(465, MailKit.Security.SecureSocketOptions.SslOnConnect)]
    [InlineData(587, MailKit.Security.SecureSocketOptions.StartTls)]
    [InlineData(25, MailKit.Security.SecureSocketOptions.StartTls)]
    [InlineData(2525, MailKit.Security.SecureSocketOptions.StartTls)]
    public void Smtp_Tls_Is_Mandatory_For_Every_Port(int port, MailKit.Security.SecureSocketOptions expected)
    {
        Assert.Equal(expected, SmtpNotificationEmailSender.SelectTls(port));
    }

    // ---------------------------------------------------------------------
    // Validation helpers (pure)
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("a@x.vn", true)]
    [InlineData("a@x.vn; b@x.vn", true)]
    [InlineData("a@x.vn,b@x.vn;;  c@x.vn ,", true)]
    [InlineData("not-an-email", false)]
    [InlineData("a@x.vn; not-an-email", false)]
    [InlineData("a@x.vn\r\nBcc: evil@x.vn", false)]
    [InlineData("a@x.vn\r\n", false)]
    [InlineData("Sales <a@x.vn>", false)]
    [InlineData("\"Sales\" a@x.vn", false)]
    [InlineData(" ; , ", false)]
    public void IsValidNotificationEmailList_Accepts_Only_Plain_Address_Lists(string value, bool expected)
    {
        Assert.Equal(expected, SiteSettingsValidation.IsValidNotificationEmailList(value));
    }

    [Fact]
    public void ParseNotificationEmails_Splits_Trims_And_Drops_Blanks()
    {
        Assert.Equal(
            new[] { "a@x.vn", "b@x.vn", "c@x.vn" },
            SiteSettingsValidation.ParseNotificationEmails(" a@x.vn ;b@x.vn,, ; c@x.vn "));
        Assert.Empty(SiteSettingsValidation.ParseNotificationEmails(null));
        Assert.Empty(SiteSettingsValidation.ParseNotificationEmails("   "));
    }

    // ---------------------------------------------------------------------
    // Matrix: SMTP not configured
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(null, "no-reply@x.vn")]
    [InlineData("smtp.example.com", "")]
    public async Task Smtp_Sender_Skips_And_Warns_When_Not_Configured(string? host, string? fromAddress)
    {
        var logger = new ListLogger<SmtpNotificationEmailSender>();
        var sender = new SmtpNotificationEmailSender(
            new StaticOptionsMonitor<SmtpOptions>(new SmtpOptions { Host = host, FromAddress = fromAddress }),
            logger);

        // Completes without throwing and without any network I/O (an
        // attempted connection to an unset host would throw).
        await sender.SendAsync(new NotificationEmail(new[] { "a@x.vn" }, "subject", "body"), CancellationToken.None);

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("SMTP is not configured"));
    }

    // ---------------------------------------------------------------------
    // Matrix: happy path, site isolation, no recipient, send throws
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Happy_Path_SiteA_Sends_One_Email_To_Every_Configured_Recipient()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await GetNotificationEmailsAsync(api, siteA.Id);
        var created = new List<Guid>();

        try
        {
            await SetNotificationEmailsAsync(api, siteA.Id, "a@x.vn; b@x.vn");

            var suffix = Guid.NewGuid().ToString("N");
            var name = $"Notify Happy {suffix}";
            var product = $"Bảng từ {suffix}";
            var message = $"Xin báo giá {suffix}";

            var id = await PostLeadAsync(HostnameOf(siteA), new { name, phone = "0901234567", productOfInterest = product, message });
            created.Add(id);

            var email = await Sender.WaitForSubjectContainingAsync(name);

            Assert.Equal($"[{siteA.Title}] Khách mới: {name} – 0901234567", email.Subject);
            Assert.Equal(new[] { "a@x.vn", "b@x.vn" }, email.To);
            Assert.Contains(siteA.Title, email.Body);
            Assert.Contains("Liên hệ / báo giá", email.Body);
            Assert.Contains(name, email.Body);
            Assert.Contains("0901234567", email.Body);
            Assert.Contains(product, email.Body);
            Assert.Contains(message, email.Body);

            // Time line is the saved row's CreatedAt, shown in Vietnam time.
            using (var dbScope = _factory.Services.CreateScope())
            {
                var leadDb = dbScope.ServiceProvider.GetRequiredService<LeadDbContext>();
                var row = await leadDb.FormSubmissions.AsNoTracking().SingleAsync(s => s.Id == id);
                Assert.Contains($"Thời gian: {LeadEmailComposer.FormatVietnamTime(row.CreatedAt)} (giờ Việt Nam)", email.Body);
            }

            // Exactly one email for this lead.
            Assert.Single(Sender.Attempts, e => e.Subject.Contains(name, StringComparison.Ordinal));
        }
        finally
        {
            await SetNotificationEmailsAsync(api, siteA.Id, original);
            await DeleteSubmissionsAsync(created);
        }
    }

    [Fact]
    public async Task SiteB_Lead_Goes_Only_To_SiteB_Recipients()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var originalA = await GetNotificationEmailsAsync(api, siteA.Id);
        var originalB = await GetNotificationEmailsAsync(api, siteB.Id);
        var created = new List<Guid>();

        try
        {
            await SetNotificationEmailsAsync(api, siteA.Id, "sales-a@x.vn");
            await SetNotificationEmailsAsync(api, siteB.Id, "sales-b@x.vn, boss-b@x.vn");

            var name = $"Notify Site B {Guid.NewGuid():N}";
            created.Add(await PostLeadAsync(HostnameOf(siteB), new { name, phone = "0909876543" }));

            var email = await Sender.WaitForSubjectContainingAsync(name);

            Assert.Equal(new[] { "sales-b@x.vn", "boss-b@x.vn" }, email.To);
            Assert.DoesNotContain("sales-a@x.vn", email.To);
            Assert.StartsWith($"[{siteB.Title}] ", email.Subject);
            Assert.Single(Sender.Attempts, e => e.Subject.Contains(name, StringComparison.Ordinal));
        }
        finally
        {
            await SetNotificationEmailsAsync(api, siteA.Id, originalA);
            await SetNotificationEmailsAsync(api, siteB.Id, originalB);
            await DeleteSubmissionsAsync(created);
        }
    }

    [Fact]
    public async Task No_Recipient_Returns_200_And_Sends_Nothing()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var siteB = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var originalA = await GetNotificationEmailsAsync(api, siteA.Id);
        var originalB = await GetNotificationEmailsAsync(api, siteB.Id);
        var created = new List<Guid>();

        try
        {
            await SetNotificationEmailsAsync(api, siteA.Id, string.Empty);
            await SetNotificationEmailsAsync(api, siteB.Id, "sentinel@x.vn");

            var silentName = $"Notify Silent {Guid.NewGuid():N}";
            created.Add(await PostLeadAsync(HostnameOf(siteA), new { name = silentName, phone = "0901000000" }));

            // The worker drains the queue in order, so once a later lead's
            // email has arrived, the silent lead has definitely been processed.
            var sentinelName = $"Notify Sentinel {Guid.NewGuid():N}";
            created.Add(await PostLeadAsync(HostnameOf(siteB), new { name = sentinelName, phone = "0902000000" }));
            await Sender.WaitForSubjectContainingAsync(sentinelName);

            Assert.DoesNotContain(Sender.Attempts, e => e.Subject.Contains(silentName, StringComparison.Ordinal));
        }
        finally
        {
            await SetNotificationEmailsAsync(api, siteA.Id, originalA);
            await SetNotificationEmailsAsync(api, siteB.Id, originalB);
            await DeleteSubmissionsAsync(created);
        }
    }

    [Fact]
    public async Task Send_Failure_Keeps_200_And_Row_And_Worker_Keeps_Running()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await GetNotificationEmailsAsync(api, siteA.Id);
        var created = new List<Guid>();

        try
        {
            await SetNotificationEmailsAsync(api, siteA.Id, "a@x.vn");

            var failingName = $"Notify {RecordingEmailSender.FailureMarker} {Guid.NewGuid():N}";
            var failingId = await PostLeadAsync(HostnameOf(siteA), new { name = failingName, phone = "0903000000" });
            created.Add(failingId);

            // The send was attempted (and threw) ...
            await Sender.WaitForSubjectContainingAsync(failingName);

            // ... the committed row is untouched ...
            using (var dbScope = _factory.Services.CreateScope())
            {
                var leadDb = dbScope.ServiceProvider.GetRequiredService<LeadDbContext>();
                var row = await leadDb.FormSubmissions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == failingId);
                Assert.NotNull(row);
                Assert.Equal(failingName, row!.Name);
            }

            // ... and the worker survived to send the next lead.
            var nextName = $"Notify After Failure {Guid.NewGuid():N}";
            created.Add(await PostLeadAsync(HostnameOf(siteA), new { name = nextName, phone = "0904000000" }));
            var next = await Sender.WaitForSubjectContainingAsync(nextName);
            Assert.Equal(new[] { "a@x.vn" }, next.To);

            // No retry of the failed one.
            Assert.Single(Sender.Attempts, e => e.Subject.Contains(failingName, StringComparison.Ordinal));
        }
        finally
        {
            await SetNotificationEmailsAsync(api, siteA.Id, original);
            await DeleteSubmissionsAsync(created);
        }
    }

    [Fact]
    public async Task Failed_Lead_Save_Sends_No_Email()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await GetNotificationEmailsAsync(api, siteA.Id);
        var created = new List<Guid>();

        try
        {
            await SetNotificationEmailsAsync(api, siteA.Id, "a@x.vn");

            // Same trigger as LeadSubmissionTests' persistence-failure test:
            // a Message past the varchar(2000) column makes MariaDB reject the INSERT.
            var failedName = $"Notify Save Failed {Guid.NewGuid():N}";
            var response = await SendLeadAsync(HostnameOf(siteA), new { name = failedName, phone = "0907000000", message = new string('x', 3000) });
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);

            // The worker drains in order: once the sentinel's email arrives,
            // anything the failed lead might have queued has been processed.
            var sentinelName = $"Notify Save Sentinel {Guid.NewGuid():N}";
            created.Add(await PostLeadAsync(HostnameOf(siteA), new { name = sentinelName, phone = "0908000000" }));
            await Sender.WaitForSubjectContainingAsync(sentinelName);

            Assert.DoesNotContain(Sender.Attempts, e => e.Subject.Contains(failedName, StringComparison.Ordinal));
        }
        finally
        {
            await SetNotificationEmailsAsync(api, siteA.Id, original);
            await DeleteSubmissionsAsync(created);
        }
    }

    [Fact]
    public async Task Visitor_CrLf_In_Name_Never_Reaches_The_Subject()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await GetNotificationEmailsAsync(api, siteA.Id);
        var created = new List<Guid>();

        try
        {
            await SetNotificationEmailsAsync(api, siteA.Id, "a@x.vn");

            var marker = Guid.NewGuid().ToString("N");
            var name = $"Evil {marker}\r\nBcc: victim@x.vn";
            created.Add(await PostLeadAsync(HostnameOf(siteA), new { name, phone = "0905000000" }));

            var email = await Sender.WaitForSubjectContainingAsync(marker);
            Assert.DoesNotContain(email.Subject, c => char.IsControl(c));
            Assert.Contains($"Evil {marker} Bcc: victim@x.vn", email.Subject);
            Assert.Equal(new[] { "a@x.vn" }, email.To);
        }
        finally
        {
            await SetNotificationEmailsAsync(api, siteA.Id, original);
            await DeleteSubmissionsAsync(created);
        }
    }

    [Fact]
    public async Task Service_Copies_Every_Field_And_Worker_Dedupes_Recipients()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var siteA = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await GetNotificationEmailsAsync(api, siteA.Id);

        try
        {
            await SetNotificationEmailsAsync(api, siteA.Id, "a@x.vn; A@x.vn, b@x.vn");

            var name = $"Notify Direct {Guid.NewGuid():N}";
            var createdAt = new DateTimeOffset(2026, 1, 2, 20, 15, 0, TimeSpan.Zero); // 03/01/2026 03:15 in Vietnam
            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                SiteId = siteA.Id,
                FormType = "survey",
                Name = name,
                Phone = "0906000000",
                ProductOfInterest = "Tủ gỗ",
                Message = "Khảo sát giúp",
                LocationAddress = "34 Trần Phú, Hà Đông",
                IsOutsideServiceArea = false,
                CreatedAt = createdAt
            };

            // Straight through the shared service - no DB row is involved,
            // so this verifies the service's detached copy end to end.
            _factory.Services.GetRequiredService<IFormNotificationService>().NotifyNewSubmission(submission);

            var email = await Sender.WaitForSubjectContainingAsync(name);

            Assert.Equal(new[] { "a@x.vn", "b@x.vn" }, email.To);
            Assert.Contains("Loại form: Khảo sát", email.Body);
            Assert.Contains("Thời gian: 03/01/2026 03:15 (giờ Việt Nam)", email.Body);
            Assert.Contains("Sản phẩm quan tâm: Tủ gỗ", email.Body);
            Assert.Contains("Lời nhắn: Khảo sát giúp", email.Body);
            Assert.Contains("Địa chỉ: 34 Trần Phú, Hà Đông", email.Body);
            Assert.Contains("Khu vực phục vụ: Trong khu vực phục vụ", email.Body);
        }
        finally
        {
            await SetNotificationEmailsAsync(api, siteA.Id, original);
        }
    }

    // ---------------------------------------------------------------------
    // Matrix: invalid recipient saved - both save paths (Story 1.9 lesson)
    // ---------------------------------------------------------------------

    private const string ExpectedRejection =
        "Notification emails must be plain email addresses (e.g. sales@example.vn), separated by commas or semicolons.";

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@x.vn\r\nBcc: evil@x.vn")]
    [InlineData("Sales <a@x.vn>")]
    public async Task Typed_Save_Path_Rejects_Invalid_Notification_Emails(string invalid)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await GetNotificationEmailsAsync(api, site.Id);
        const string safe = "still-safe@x.vn";

        try
        {
            await SetNotificationEmailsAsync(api, site.Id, safe);

            var rejection = await Assert.ThrowsAsync<ValidationException>(() => SetNotificationEmailsAsync(api, site.Id, invalid));
            Assert.Equal(ExpectedRejection, rejection.Message);

            Assert.Equal(safe, await GetNotificationEmailsAsync(api, site.Id));
        }
        finally
        {
            await SetNotificationEmailsAsync(api, site.Id, original);
        }
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@x.vn\r\nBcc: evil@x.vn")]
    [InlineData("Sales <a@x.vn>")]
    public async Task Manager_Save_Path_Rejects_Invalid_Notification_Emails_Via_DynamicSiteContent(string invalid)
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TbTruongHocInternalId);
        var original = await GetNotificationEmailsAsync(api, site.Id);
        const string safe = "still-safe-dyn@x.vn";

        try
        {
            await SetNotificationEmailsAsync(api, site.Id, safe);

            var dyn = await api.Sites.GetContentByIdAsync(site.Id);
            Assert.NotNull(dyn);
            SetRegionValue(dyn!, nameof(SiteSettings.NotificationEmails), invalid);

            var rejection = await Assert.ThrowsAsync<ValidationException>(() => api.Sites.SaveContentAsync(site.Id, dyn!));
            Assert.Equal(ExpectedRejection, rejection.Message);

            Assert.Equal(safe, await GetNotificationEmailsAsync(api, site.Id));
        }
        finally
        {
            await SetNotificationEmailsAsync(api, site.Id, original);
        }
    }

    [Fact]
    public async Task Manager_Save_Path_Persists_A_Valid_Notification_Email_List()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetSiteAsync(api, SiteSeed.TrongDoiTamInternalId);
        var original = await GetNotificationEmailsAsync(api, site.Id);
        const string valid = "sales@x.vn; boss@x.vn";

        try
        {
            var dyn = await api.Sites.GetContentByIdAsync(site.Id);
            Assert.NotNull(dyn);
            SetRegionValue(dyn!, nameof(SiteSettings.NotificationEmails), valid);

            await api.Sites.SaveContentAsync(site.Id, dyn!);

            Assert.Equal(valid, await GetNotificationEmailsAsync(api, site.Id));
        }
        finally
        {
            await SetNotificationEmailsAsync(api, site.Id, original);
        }
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private async Task<Guid> PostLeadAsync(string hostname, object payload)
    {
        var response = await SendLeadAsync(hostname, payload);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}: {body}");

        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<HttpResponseMessage> SendLeadAsync(string hostname, object payload)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, LeadsEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Host = hostname;

        return await client.SendAsync(request);
    }

    private async Task DeleteSubmissionsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
        {
            return;
        }

        using var scope = _factory.Services.CreateScope();
        var leadDb = scope.ServiceProvider.GetRequiredService<LeadDbContext>();
        var rows = await leadDb.FormSubmissions.Where(s => idList.Contains(s.Id)).ToListAsync();
        leadDb.FormSubmissions.RemoveRange(rows);
        await leadDb.SaveChangesAsync();
    }

    private static async Task<string?> GetNotificationEmailsAsync(IApi api, Guid siteId)
    {
        var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(siteId);
        return settings?.NotificationEmails?.Value;
    }

    private static async Task SetNotificationEmailsAsync(IApi api, Guid siteId, string? value)
    {
        var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(siteId)
            ?? await api.Sites.CreateContentAsync<SiteSettings>();
        settings.NotificationEmails = value;
        await api.Sites.SaveContentAsync(siteId, settings);
    }

    /// <summary>See <c>SiteSettingsTests.SetRegionValue</c> - same Manager-shaped region mutation.</summary>
    private static void SetRegionValue(DynamicSiteContent dyn, string regionKey, string? value)
    {
        var regions = (IDictionary<string, object>)dyn.Regions;
        if (regions.TryGetValue(regionKey, out var existing) && existing is StringField field)
        {
            field.Value = value;
        }
        else
        {
            regions[regionKey] = new StringField { Value = value };
        }
    }

    private static async Task<Site> GetSiteAsync(IApi api, string internalId)
    {
        var site = await api.Sites.GetByInternalIdAsync(internalId);
        Assert.NotNull(site);
        return site!;
    }

    private static string HostnameOf(Site site)
    {
        var hostname = site.Hostnames?.Split(',').FirstOrDefault()?.Trim();
        Assert.False(string.IsNullOrEmpty(hostname));
        return hostname!;
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T value) => CurrentValue = value;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
