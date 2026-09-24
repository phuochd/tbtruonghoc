#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Piranha;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Notifications;

/// <summary>
/// Story 1.7: drains <see cref="FormNotificationService"/>'s queue. For each
/// submission it opens its own DI scope, resolves that submission's own
/// site's title and <see cref="SiteSettings.NotificationEmails"/> (never
/// another site's, never a global fallback), composes and sends one email.
/// Every failure is caught and logged per item - one bad item or an SMTP
/// outage never stops the worker, and nothing is retried.
/// </summary>
public class FormNotificationWorker : BackgroundService
{
    private readonly FormNotificationService _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FormNotificationWorker> _logger;

    public FormNotificationWorker(FormNotificationService queue, IServiceScopeFactory scopeFactory, ILogger<FormNotificationWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var submission in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessAsync(submission, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown - anything still queued is dropped by design,
            // but leave a trace so a lost notification is explainable.
            if (_queue.Reader.CanCount && _queue.Reader.Count > 0)
            {
                _logger.LogWarning(
                    "Shutting down with {Count} lead notification(s) still queued; they will not be sent (the leads themselves are saved).",
                    _queue.Reader.Count);
            }
        }
    }

    private async Task ProcessAsync(FormSubmission submission, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();

            var site = await api.Sites.GetByIdAsync(submission.SiteId);
            var settings = await api.Sites.GetContentByIdAsync<SiteSettings>(submission.SiteId);

            var entries = SiteSettingsValidation.ParseNotificationEmails(settings?.NotificationEmails?.Value);
            var invalid = entries.Where(e => !SiteSettingsValidation.IsValidNotificationEmail(e)).ToList();
            if (invalid.Count > 0)
            {
                _logger.LogWarning(
                    "Skipping {Count} invalid notification address(es) configured for site {SiteId}.",
                    invalid.Count,
                    submission.SiteId);
            }

            var recipients = entries
                .Where(SiteSettingsValidation.IsValidNotificationEmail)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (recipients.Count == 0)
            {
                _logger.LogWarning(
                    "No lead notification sent for submission {SubmissionId}: site {SiteId} has no valid Notification emails configured.",
                    submission.Id,
                    submission.SiteId);
                return;
            }

            var email = LeadEmailComposer.Compose(submission, site?.Title, recipients);
            var sender = scope.ServiceProvider.GetRequiredService<INotificationEmailSender>();
            await sender.SendAsync(email, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send the lead notification for submission {SubmissionId} (site {SiteId}); not retried - the lead is still saved and visible in Manager.",
                submission.Id,
                submission.SiteId);
        }
    }
}
