using System;
using System.Collections.Generic;
using System.Text;

namespace NotificationService.Lib.Helper
{
    public interface IKeyValuePairHelper
    {
        /// <summary>
        /// 取得符合 pattern 的所有 key 。
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        IEnumerable<string> GetKeys(string pattern);
        bool IsKeyPatternExist(string pattern);
        /// <summary>
        /// 取得 key 相對應的 value 。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string Get(string key);

        /// <summary>
        /// lock key 並取得 key 相對應的 value 。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string GetWithLock(string key);

        /// <summary>
        /// 以預設的資料有效時間來設置一組 key-value pair 。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Set(string key, string value);

        /// <summary>
        /// lock key 並以預設的資料有效時間來設置一組 key-value pair 。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool SetWithLock(string key, string value);

        /// <summary>
        /// 設置一組 key-value pair 。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="effectiveTimeInSeconds">有效時間，以秒為單位</param>
        /// <returns></returns>
        bool Set(string key, string value, int? effectiveTimeInSeconds = null);

        /// <summary>
        /// lock key 並設置一組 key-value pair 。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="effectiveTimeInSeconds">有效時間，以秒為單位</param>
        /// <returns></returns>
        bool SetWithLock(string key, string value, int? effectiveTimeInSeconds = null);

        /// <summary>
        /// 刪除資料。
        /// </summary>
        /// <param name="key">Session Key</param>
        /// <returns></returns>
        bool Delete(string key);

        /// <summary>
        /// lock key 並刪除資料。
        /// </summary>
        /// <param name="key">Session Key</param>
        /// <returns></returns>
        bool DeleteWithLock(string key);

        /// <summary>
        /// lock key 並執行 action 。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="action"></param>
        /// <param name="arg"></param>
        void LockKey(string key, Action<object[]> action, object[] arg);

        /// <summary>
        /// lock key 並執行 func 。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="func"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        T LockKey<T>(string key, Func<object[], T> func, object[] arg);
    }
}
