using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API_REST_CURSOSACADEMICOS.Hubs
{
    [Authorize]
    public class NotificationsHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("id")?.Value;
            Console.WriteLine($"Cliente conectado al hub de notificaciones. Usuario: {userId}, ConnectionId: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("id")?.Value;
            Console.WriteLine($"Cliente desconectado del hub de notificaciones. Usuario: {userId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
