using System.Collections.Generic;

namespace NotificationService.Lib.Connection
{
    public interface IConnectionManager
    {
        void Add(string userId, string connectionId);
        IEnumerable<string> GetConnections(string userId);
        void Remove(string userId, string connectionId);
    }
}
