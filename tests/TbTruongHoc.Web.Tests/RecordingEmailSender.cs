using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TbTruongHoc.Web.Notifications;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Story 1.7: stands in for <see cref="SmtpNotificationEmailSender"/> in the
/// shared test host (see <see cref="PiranhaWebApplicationFactory"/>) so the
/// suite never opens a real SMTP connection. Records every send attempt and
/// lets a test await a specific email. Any email whose subject contains
/// <see cref="FailureMarker"/> is recorded and then throws, simulating an
/// SMTP outage/auth failure.
/// </summary>
public sealed class RecordingEmailSender : INotificationEmailSender
{
    public const string FailureMarker = "SIMULATE-SMTP-FAILURE";

    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    private readonly object _gate = new();
    private readonly List<NotificationEmail> _attempts = new();
    private readonly List<(Func<NotificationEmail, bool> Match, TaskCompletionSource<NotificationEmail> Tcs)> _waiters = new();

    public IReadOnlyList<NotificationEmail> Attempts
    {
        get
        {
            lock (_gate)
            {
                return _attempts.ToList();
            }
        }
    }

    public Task SendAsync(NotificationEmail email, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _attempts.Add(email);
            foreach (var waiter in _waiters.Where(w => w.Match(email)).ToList())
            {
                waiter.Tcs.TrySetResult(email);
                _waiters.Remove(waiter);
            }
        }

        if (email.Subject.Contains(FailureMarker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Simulated SMTP failure.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Completes with the first recorded send attempt (past or future)
    /// matching <paramref name="match"/>; throws <see cref="TimeoutException"/>
    /// after <paramref name="timeout"/> (default <see cref="DefaultTimeout"/>).
    /// </summary>
    public Task<NotificationEmail> WaitForAsync(Func<NotificationEmail, bool> match, TimeSpan? timeout = null)
    {
        TaskCompletionSource<NotificationEmail> tcs;
        lock (_gate)
        {
            var existing = _attempts.FirstOrDefault(match);
            if (existing != null)
            {
                return Task.FromResult(existing);
            }

            tcs = new TaskCompletionSource<NotificationEmail>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((match, tcs));
        }

        return tcs.Task.WaitAsync(timeout ?? DefaultTimeout);
    }

    public Task<NotificationEmail> WaitForSubjectContainingAsync(string text, TimeSpan? timeout = null) =>
        WaitForAsync(e => e.Subject.Contains(text, StringComparison.Ordinal), timeout);
}
