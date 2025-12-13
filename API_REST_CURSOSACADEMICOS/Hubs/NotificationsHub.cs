using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using API_REST_CURSOSACADEMICOS.Extensions;

namespace API_REST_CURSOSACADEMICOS.Hubs
{
    [Authorize]
    public class NotificationsHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.TryGetUserId(out var id) ? id.ToString() : null;
            Console.WriteLine($"Cliente conectado al hub de notificaciones. Usuario: {userId ?? "(unknown)"}, ConnectionId: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User.TryGetUserId(out var id) ? id.ToString() : null;
            Console.WriteLine($"Cliente desconectado del hub de notificaciones. Usuario: {userId ?? "(unknown)"}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}