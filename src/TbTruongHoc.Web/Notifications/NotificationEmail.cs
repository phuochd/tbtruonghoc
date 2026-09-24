#nullable enable

using System.Collections.Generic;

namespace TbTruongHoc.Web.Notifications;

/// <summary>
/// Story 1.7: one composed, plain-text notification email, ready to hand to
/// <see cref="INotificationEmailSender"/>. <see cref="To"/> holds only
/// already-validated plain addresses.
/// </summary>
public sealed record NotificationEmail(IReadOnlyList<string> To, string Subject, string Body);
