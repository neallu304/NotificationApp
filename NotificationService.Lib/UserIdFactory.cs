using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text;

namespace NotificationService.Lib
{
    public class UserIdFactory : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            var userId =connection.GetHttpContext().Request.Query["UserId"];
            if (userId.Count > 0)
            {
                return userId[0];
            }
            throw new ArgumentNullException($"Invalid connection request, UserId is null");
        }
    }
}
