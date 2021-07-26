using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NotificationService.Lib.Helper
{
    public class RedisHelper : IKeyValuePairHelper
    {
        private ConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _db;
        private readonly int _databaseSelect;
        private readonly int _databaseSelectByLock;
        private int? _dataEffectiveTime;
        private RedLockFactory _redLockFactory = null;
        private double _lockExpirySeconds = 15;
        private double _lockWaitSeconds = 13;
        private double _lockRetrySeconds = 1;

        public static IServer RedisServer { get; set; }

        public class RedisConfig
        {
            public string ConnectionString { get; set; }
            public int? DatabaseSelect { get; set; }
            public int? DataEffectiveTime { get; set; }
            public int? DatabaseSelectByLock { get; set; }
            public double? LockExpirySeconds { get; set; }
            public double? LockWaitSeconds { get; set; }
            public double? LockRetrySeconds { get; set; }
        }

        public RedisHelper(RedisConfig redisConfig)
        {
            if (redisConfig == null || string.IsNullOrWhiteSpace(redisConfig?.ConnectionString) || redisConfig?.DatabaseSelect == null)
            {
                throw new ArgumentNullException($"Please check redis config.");
            }

            _databaseSelect = (int)redisConfig?.DatabaseSelect;
            _dataEffectiveTime = redisConfig?.DataEffectiveTime;
            _databaseSelectByLock = (int)redisConfig?.DatabaseSelectByLock;

            var configOptions = ConfigurationOptions.Parse(redisConfig.ConnectionString);
            configOptions.DefaultDatabase = _databaseSelectByLock;
            _connectionMultiplexer = ConnectionMultiplexer.Connect(configOptions);
            _db = _connectionMultiplexer.GetDatabase(_databaseSelect);
            RedisServer = _connectionMultiplexer.GetServer(configOptions.EndPoints.First());
            _redLockFactory = RedLockFactory.Create(new RedLockMultiplexer[] { _connectionMultiplexer });

            if (redisConfig.LockExpirySeconds != null)
            {
                _lockExpirySeconds = (double)redisConfig.LockExpirySeconds;
            }

            if (redisConfig.LockWaitSeconds != null)
            {
                _lockWaitSeconds = (double)redisConfig.LockWaitSeconds;
            }

            if (redisConfig.LockRetrySeconds != null)
            {
                _lockRetrySeconds = (double)redisConfig.LockRetrySeconds;
            }
        }

        /// <summary>
        /// 測試 Redis 連線
        /// </summary>
        /// <returns></returns>
        public TimeSpan Ping()
        {
            return _db.Ping();
        }

        public bool Delete(string key)
        {
            return _db.KeyDelete(key);
        }

        public bool DeleteWithLock(string key)
        {
            return LockKey(
                key,
                arg =>
                {
                    return Delete(arg[0] as string);
                },
                new object[] { key });
        }

        public IEnumerable<string> GetKeys(string pattern)
        {
            return RedisServer.Keys(_databaseSelect, pattern).Select(x => x.ToString());
        }
        public bool IsKeyPatternExist(string pattern)
        {
            return RedisServer.Keys(_databaseSelect, pattern,1).Any();
        }

        public string Get(string key)
        {
            return _db.StringGet(key);
        }

        public string GetWithLock(string key)
        {
            return LockKey(
                key,
                arg =>
                {
                    return Get(arg[0] as string);
                },
                new object[] { key });
        }

        public bool Set(string key, string value, int? effectiveTimeInSeconds = null)
        {
            if (effectiveTimeInSeconds == null)
            {
                return _db.StringSet(key, value);
            }
            else
            {
                return _db.StringSet(key, value, TimeSpan.FromSeconds((int)effectiveTimeInSeconds));
            }
        }

        public bool SetWithLock(string key, string value, int? effectiveTimeInSeconds = null)
        {
            return LockKey(
                key,
                arg =>
                {
                    return Set(arg[0] as string, arg[1] as string, arg[2] as int?);
                },
                new object[] { key, value, effectiveTimeInSeconds });
        }

        public bool Set(string key, string value)
        {
            if (_dataEffectiveTime == null)
            {
                return _db.StringSet(key, value);
            }
            else
            {
                return _db.StringSet(key, value, TimeSpan.FromSeconds((int)_dataEffectiveTime));
            }
        }

        public bool SetWithLock(string key, string value)
        {
            return LockKey(
                key,
                arg =>
                {
                    return Set(arg[0] as string, arg[1] as string);
                },
                new object[] { key, value });
        }

        public void LockKey(string key, Action<object[]> action, object[] arg)
        {
            // lock object 失效時間
            var expiry = TimeSpan.FromSeconds(_lockExpirySeconds);
            // thread等待時間
            var wait = TimeSpan.FromSeconds(_lockWaitSeconds);
            // 重試間隔時間
            var retry = TimeSpan.FromSeconds(_lockRetrySeconds);

            // blocks 直到取得 lock 資源或是達到thread等待時間
            using (var redLock = _redLockFactory.CreateLock(key, expiry, wait, retry))
            {
                // 確定取得 lock 所有權
                if (redLock.IsAcquired)
                {
                    action(arg);
                }
                else
                {
                    throw new InvalidOperationException($"Key is locked: { key }");
                }
            }
        }

        public T LockKey<T>(string key, Func<object[], T> func, object[] arg)
        {
            T result = default(T);

            // lock object 失效時間
            var expiry = TimeSpan.FromSeconds(_lockExpirySeconds);
            // thread等待時間
            var wait = TimeSpan.FromSeconds(_lockWaitSeconds);
            // 重試間隔時間
            var retry = TimeSpan.FromSeconds(_lockRetrySeconds);

            // blocks 直到取得 lock 資源或是達到thread等待時間
            using (var redLock = _redLockFactory.CreateLock(key, expiry, wait, retry))
            {
                // 確定取得 lock 所有權
                if (redLock.IsAcquired)
                {
                    result = func(arg);
                }
                else
                {
                    throw new InvalidOperationException($"Key is locked: { key }");
                }
            }

            return result;
        }

        public ITransaction BeginTran(string key, Condition cond = null)
        {
            var tran = _db.CreateTransaction();
            if (cond != null)
            {
                tran.AddCondition(cond);
            }
            return tran;
        }
    }
}
