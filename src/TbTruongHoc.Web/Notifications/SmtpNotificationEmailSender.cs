#nullable enable

using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace TbTruongHoc.Web.Notifications;

/// <summary>
/// Story 1.7: the single SMTP sender (MailKit - Microsoft discourages
/// <c>System.Net.Mail.SmtpClient</c> for new code). One connection per
/// email: lead volume is low and this keeps no long-lived socket state to go
/// stale between leads. Throws on any SMTP failure - the caller
/// (<see cref="FormNotificationWorker"/>) catches and logs; there is no retry.
/// </summary>
public class SmtpNotificationEmailSender : INotificationEmailSender
{
    private const int TimeoutMilliseconds = 30_000;

    private readonly IOptionsMonitor<SmtpOptions> _options;
    private readonly ILogger<SmtpNotificationEmailSender> _logger;

    public SmtpNotificationEmailSender(IOptionsMonitor<SmtpOptions> options, ILogger<SmtpNotificationEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// TLS is mandatory. Port 465 is implicit TLS (<see cref="SecureSocketOptions.SslOnConnect"/>);
    /// every other port requires STARTTLS (<see cref="SecureSocketOptions.StartTls"/>), which fails
    /// the connection if the server does not offer it. Never <c>Auto</c> or
    /// <c>StartTlsWhenAvailable</c>: those silently fall back to cleartext and would send the
    /// SMTP credentials and the visitor's name/phone unencrypted.
    /// </summary>
    public static SecureSocketOptions SelectTls(int port) =>
        port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

    public async Task SendAsync(NotificationEmail email, CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (!options.IsConfigured)
        {
            // Subject deliberately not logged - it carries the visitor's name/phone.
            _logger.LogWarning(
                "Lead notification email not sent: SMTP is not configured (Smtp:Host and Smtp:FromAddress are required).");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName ?? string.Empty, options.FromAddress!));
        foreach (var to in email.To)
        {
            message.To.Add(MailboxAddress.Parse(to));
        }

        message.Subject = email.Subject;
        message.Body = new TextPart("plain") { Text = email.Body };

        using var client = new SmtpClient { Timeout = TimeoutMilliseconds };
        await client.ConnectAsync(options.Host!, options.Port, SelectTls(options.Port), cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            await client.AuthenticateAsync(options.Username, options.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
