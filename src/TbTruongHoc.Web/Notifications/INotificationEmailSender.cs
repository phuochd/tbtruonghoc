#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace TbTruongHoc.Web.Notifications;

/// <summary>
/// Story 1.7: the one outbound email seam. Production uses
/// <see cref="SmtpNotificationEmailSender"/>; tests swap in a recording
/// fake so no real SMTP connection is ever attempted.
/// </summary>
public interface INotificationEmailSender
{
    Task SendAsync(NotificationEmail email, CancellationToken cancellationToken);
}
