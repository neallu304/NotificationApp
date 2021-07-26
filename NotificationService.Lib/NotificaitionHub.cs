using NotificationService.Lib.Connection;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace NotificationService.Lib
{
    public class NotificaitionHub : Hub<IClientNotification>
    {
        private readonly IConnectionManager _connections;
        public NotificaitionHub(IConnectionManager connections)
        {
            _connections = connections;
        }

        public override async Task OnConnectedAsync()
        {
            _connections.Add(Context.UserIdentifier, Context.ConnectionId);

            await base.OnConnectedAsync();
        }
        // 斷線
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _connections.Remove(Context.UserIdentifier, Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

    }
}
