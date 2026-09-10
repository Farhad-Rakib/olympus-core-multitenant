using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ProjectNamePlaceholder.Api.Hubs;

// Receive-only. Clients connect to listen for "ReceiveNotification"; they cannot invoke anything.
//
// This deliberately declares NO public methods. A public hub method is callable by any connected
// client, not just by server code -- this class previously exposed
// SendNotificationToUser(userId, notification), which let any authenticated user push an arbitrary
// payload to any user id. Since user ids are global rather than per-tenant, that reached across
// tenant boundaries.
//
// To push from server code, inject IHubContext<NotificationHub> and send from there:
//
//     await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", dto, ct);
//
// When you do, resolve the recipient inside the sending tenant's scope first. Clients.User() takes
// the raw user id and performs no tenant check of its own, so the caller owns that guarantee.
[Authorize]
public class NotificationHub : Hub
{
}
