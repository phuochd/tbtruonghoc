#nullable enable

using System;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Notifications;

/// <summary>
/// Story 1.7 (FR-3, AD-3): the one shared notification entry point for every
/// <see cref="FormSubmission"/> creation, any form type, either site. Called
/// only after the row is committed.
/// </summary>
public interface IFormNotificationService
{
    /// <summary>
    /// Queues a notification for an already-committed submission and returns
    /// immediately. Never throws and never waits on SMTP - fail-open, so the
    /// visitor's response is never affected by the email step.
    /// </summary>
    void NotifyNewSubmission(FormSubmission submission);
}

/// <summary>
/// Drops each submission onto a bounded in-memory queue drained by
/// <see cref="FormNotificationWorker"/>. Not durable by design: items still
/// queued when the process stops are lost, which is acceptable because the
/// lead itself is already committed and Manager's Leads screen is the source
/// of truth. No retry, no outbox.
/// </summary>
public class FormNotificationService : IFormNotificationService
{
    public const int Capacity = 100;

    private readonly Channel<FormSubmission> _channel = Channel.CreateBounded<FormSubmission>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait, // TryWrite returns false when full instead of dropping silently
            SingleReader = true,
            SingleWriter = false
        });

    private readonly ILogger<FormNotificationService> _logger;

    public FormNotificationService(ILogger<FormNotificationService> logger)
    {
        _logger = logger;
    }

    internal ChannelReader<FormSubmission> Reader => _channel.Reader;

    public void NotifyNewSubmission(FormSubmission submission)
    {
        try
        {
            // Detached copy: the caller's instance is tracked by a request-
            // scoped DbContext and must not be shared with the worker thread.
            var copy = new FormSubmission
            {
                Id = submission.Id,
                SiteId = submission.SiteId,
                FormType = submission.FormType,
                Name = submission.Name,
                Phone = submission.Phone,
                ProductOfInterest = submission.ProductOfInterest,
                Message = submission.Message,
                LocationAddress = submission.LocationAddress,
                IsOutsideServiceArea = submission.IsOutsideServiceArea,
                CreatedAt = submission.CreatedAt
            };

            if (!_channel.Writer.TryWrite(copy))
            {
                _logger.LogWarning(
                    "Lead notification queue is full ({Capacity} items); no email will be sent for submission {SubmissionId}.",
                    Capacity,
                    submission.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue the lead notification for submission {SubmissionId}.", submission?.Id);
        }
    }
}
