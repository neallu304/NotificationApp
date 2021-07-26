using NotificationService.Lib.Helper;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using LogManager = NLog.LogManager;

namespace NotificationService.Lib.Connection
{
    public class RedisConnectionManager : IConnectionManager
    {
        public readonly IKeyValuePairHelper _redisHelper;
        readonly ILogger _logger = LogManager.GetLogger("Log");
        public RedisConnectionManager(IKeyValuePairHelper redisHelper)
        {
            _redisHelper = redisHelper;
        }

        public void Add(string userId, string connectionId)
        {
            try
            {
                _redisHelper.Set($"ws_{userId}_ntfy_{connectionId}", "");
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex}");
                throw ex;
            }
        }

        public IEnumerable<string> GetConnections(string userId)
        {
            try 
            {
                var connectionIdList = _redisHelper.GetKeys($"ws_{userId}_ntfy_*");
                if (connectionIdList.Any())
                {
                    var count = connectionIdList.Count();
                    return connectionIdList;
                }

                return Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex}");
                throw ex;
            }
        }

        public void Remove(string userId, string connectionId)
        {
            _redisHelper.Delete($"{userId}_{connectionId}");
        }
    }
}

