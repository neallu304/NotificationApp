using NotificationService.Lib.Helper;
using System.Collections.Generic;
using System.Linq;

namespace NotificationService.Lib.Connection
{
    public class SimpleConnectionManager : IConnectionManager
    {
        private readonly Dictionary<string, HashSet<string>> _connections =
            new Dictionary<string, HashSet<string>>();
        public SimpleConnectionManager()
        {
        }
        public int Count
        {
            get
            {
                return _connections.Count;
            }
        }

        public void Add(string userId, string connectionId)
        {
            lock (_connections)
            {
                HashSet<string> connections;
                if (!_connections.TryGetValue(userId, out connections))
                {
                    connections = new HashSet<string>();
                    _connections.Add(userId, connections);
                }

                lock (connections)
                {
                    connections.Add(connectionId);
                }
            }
        }

        public IEnumerable<string> GetConnections(string userId)
        {
            HashSet<string> connections;
            if (_connections.TryGetValue(userId, out connections))
            {
                return connections;
            }

            return Enumerable.Empty<string>();
        }

        public void Remove(string userId, string connectionId)
        {
            lock (_connections)
            {
                HashSet<string> connections;
                if (!_connections.TryGetValue(userId, out connections))
                {
                    return;
                }

                lock (connections)
                {
                    connections.Remove(connectionId);

                    if (connections.Count == 0)
                    {
                        _connections.Remove(userId);
                    }
                }
            }
        }
    }
}
