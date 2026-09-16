using StackExchange.Redis;
using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// Redis Helper
    /// </summary>
    public class RedisHelper
    {

        /// <summary>
        /// Redis Database
        /// </summary>
        public IDatabase Db
        {
            get;
            private set;
        }

        /// <summary>
        /// Redis Key Prefix
        /// </summary>
        private string? PrefixKey
        {
            get;
            set;
        }

        /// <summary>
        /// Current Redis Connection Multiplexer
        /// </summary>
        public IConnectionMultiplexer ConnMultiplexer
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public RedisHelper(string? name = null)
        {
            RedisConfig config;
            if (!string.IsNullOrEmpty(name))
            {
                config = RedisFactory.TryGetRedisConfig(name)!;
            }
            else
            {
                config = RedisFactory.TryGetDefaultRedisConfig()!;
            }
            PrefixKey = config.PrefixKey;
            ConnMultiplexer = RedisFactory.GetConnectionRedisMultiplexer(config.Name)!;
            Db = ConnMultiplexer.GetDatabase(config.Db);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string AddKeyPrefix(string key)
        {
            if (string.IsNullOrEmpty(PrefixKey))
            {
                return key;
            }
            else
            {
                return $"{PrefixKey}:{key}";
            }
        }

        /// <summary>
        /// 获取带 Prefix 的完整键（供脚本类操作使用）
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetFullKey(string key)
        {
            return AddKeyPrefix(key);
        }

        #region String

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public bool StringSet(string redisKey, string redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.StringSet(redisKey, redisValue, expiry, When.Always);
        }

        /// <summary>
        /// 仅当键不存在时写入（SET NX），可用于幂等占位。
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="expiry"></param>
        /// <returns>true=占位成功；false=键已存在</returns>
        public bool StringSetNx(string redisKey, string redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.StringSet(redisKey, redisValue, expiry, When.NotExists);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyValuePairs"></param>
        /// <returns></returns>
        public bool StringSet(IEnumerable<KeyValuePair<RedisKey, RedisValue>> keyValuePairs)
        {
            keyValuePairs = keyValuePairs.Select(x => new KeyValuePair<RedisKey, RedisValue>(AddKeyPrefix(x.Key!), x.Value));
            return Db.StringSet(keyValuePairs.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public string? StringGet(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.StringGet(redisKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public bool StringSet<T>(string redisKey, T redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            var json = RedisFactory.Serialize(redisValue);
            return Db.StringSet(redisKey, json, expiry, When.Always);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public T? StringGet<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = Db.StringGet(redisKey);
            return string.IsNullOrEmpty(json) ? default(T) : RedisFactory.Deserialize<T>(json);
        }

        #region Async

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task<bool> StringSetAsync(string redisKey, string redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.StringSetAsync(redisKey, redisValue, expiry, When.Always);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyValuePairs"></param>
        /// <returns></returns>
        public async Task<bool> StringSetAsync(IEnumerable<KeyValuePair<RedisKey, RedisValue>> keyValuePairs)
        {
            keyValuePairs = keyValuePairs.Select(x => new KeyValuePair<RedisKey, RedisValue>(AddKeyPrefix(x.Key!), x.Value));
            return await Db.StringSetAsync(keyValuePairs.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task<string?> StringGetAsync(string redisKey, string redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.StringGetAsync(redisKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task<bool> StringSetAsync<T>(string redisKey, T redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            var json = RedisFactory.Serialize(redisValue);
            return await Db.StringSetAsync(redisKey, json, expiry, When.Always);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task<T?> StringGetAsync<T>(string redisKey, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = await Db.StringGetAsync(redisKey);
            return string.IsNullOrEmpty(json) ? default(T) : RedisFactory.Deserialize<T>(json);
        }

        #endregion

        #endregion

        #region Hash

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public bool HashExists(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashExists(redisKey, hashField);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public bool HashDelete(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashDelete(redisKey, hashField);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public long HashDelete(string redisKey, IEnumerable<RedisValue> hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashDelete(redisKey, hashField.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool HashSet(string redisKey, string hashField, string value)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashSet(redisKey, hashField, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashFields"></param>
        public void HashSet(string redisKey, IEnumerable<HashEntry> hashFields)
        {
            redisKey = AddKeyPrefix(redisKey);
            Db.HashSet(redisKey, hashFields.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public RedisValue HashGet(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashGet(redisKey, hashField);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public RedisValue[] HashGet(string redisKey, RedisValue[] hashField, string value)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashGet(redisKey, hashField);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public IEnumerable<RedisValue> HashKeys(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashKeys(redisKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public RedisValue[] HashValues(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashValues(redisKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool HashSet<T>(string redisKey, string hashField, T value)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = RedisFactory.Serialize(value);
            return Db.HashSet(redisKey, hashField, json);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public T? HashGet<T>(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = Db.HashGet(redisKey, hashField);
            return string.IsNullOrEmpty(json) ? default(T) : RedisFactory.Deserialize<T>(json);
        }

        #region async

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public async Task<bool> HashExistsAsync(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashExistsAsync(redisKey, hashField);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public async Task<bool> HashDeleteAsync(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashDeleteAsync(redisKey, hashField);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public async Task<long> HashDeleteAsync(string redisKey, IEnumerable<RedisValue> hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashDeleteAsync(redisKey, hashField.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> HashSetAsync(string redisKey, string hashField, string value)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashSetAsync(redisKey, hashField, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashFields"></param>
        public async Task HashSetAsync(string redisKey, IEnumerable<HashEntry> hashFields)
        {
            redisKey = AddKeyPrefix(redisKey);
            await Db.HashSetAsync(redisKey, hashFields.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public async Task<RedisValue> HashGetAsync(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashGetAsync(redisKey, hashField);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> HashGetAsync(string redisKey, RedisValue[] hashField, string value)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashGetAsync(redisKey, hashField);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> HashKeysAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashKeysAsync(redisKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> HashValuesAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashValuesAsync(redisKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> HashSetAsync<T>(string redisKey, string hashField, T value)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = RedisFactory.Serialize(value);
            return await Db.HashSetAsync(redisKey, hashField, json);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="hashField"></param>
        /// <returns></returns>
        public async Task<T?> HashGetAsync<T>(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = await Db.HashGetAsync(redisKey, hashField);
            return string.IsNullOrEmpty(json) ? default(T) : RedisFactory.Deserialize<T>(json);
        }

        #endregion

        #endregion 

        #region List

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public string? ListLeftPop(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListLeftPop(redisKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public string? ListRightPop(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListRightPop(redisKey);
        }

        /// <summary>
        /// 移除列表指定键上与该值相同的元素
        /// Remove elements from the specified key in the list that are equal to this value
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public long ListRemove(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListRemove(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// Insert a value at the end of the list. If the key does not exist, create it first before inserting the value
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public long ListRightPush(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListRightPush(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// Insert a value at the head of the list. If the key does not exist, create it first before inserting the value
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public long ListLeftPush(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListLeftPush(redisKey, redisValue);
        }

        /// <summary>
        /// 返回列表上该键的长度，如果不存在，返回 0
        /// Return the length of this key in the list, or 0 if it does not exist
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public long ListLength(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListLength(redisKey);
        }

        /// <summary>
        /// 返回在该列表上键所对应的元素
        /// Return the element corresponding to the key in the list
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public IEnumerable<RedisValue> ListRange(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListRange(redisKey);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// Remove and return the first element stored in the key list
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public T? ListLeftPop<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = Db.ListLeftPop(redisKey);
            return string.IsNullOrEmpty(json) ? default(T) : RedisFactory.Deserialize<T>(json);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// Remove and return the last element stored in the key list
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public T? ListRightPop<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = Db.ListRightPop(redisKey);
            return string.IsNullOrEmpty(json) ? default(T) : RedisFactory.Deserialize<T>(json);
        }

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// Insert a value at the end of the list. If the key does not exist, create it first before inserting the value
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public long ListRightPush<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListRightPush(redisKey, RedisFactory.Serialize(redisValue));
        }

        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// Insert a value at the head of the list. If the key does not exist, create it first before inserting the value
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public long ListLeftPush<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListLeftPush(redisKey, RedisFactory.Serialize(redisValue));
        }

        #region List-async

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// Remove and return the first element stored in the key list
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<string?> ListLeftPopAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListLeftPopAsync(redisKey);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// Remove and return the last element stored in the key list
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<string?> ListRightPopAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListRightPopAsync(redisKey);
        }

        /// <summary>
        /// 移除列表指定键上与该值相同的元素
        /// Remove elements from the specified key in the list that are equal to this value
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> ListRemoveAsync(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListRemoveAsync(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// Insert a value at the end of the list. If the key does not exist, create it first before inserting the value
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> ListRightPushAsync(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListRightPushAsync(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// Insert a value at the head of the list. If the key does not exist, create it first before inserting the value
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> ListLeftPushAsync(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListLeftPushAsync(redisKey, redisValue);
        }

        /// <summary>
        /// 返回列表上该键的长度，如果不存在，返回 0
        /// Return the length of this key on the list, or 0 if it does not exist
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<long> ListLengthAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListLengthAsync(redisKey);
        }

        /// <summary>
        /// 返回在该列表上键所对应的元素
        /// Return the element corresponding to the key in the list
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> ListRangeAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListRangeAsync(redisKey);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// Remove and return the first element stored in the key list
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<T?> ListLeftPopAsync<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = await Db.ListLeftPopAsync(redisKey);
            return string.IsNullOrEmpty(json) ? default : RedisFactory.Deserialize<T>(json);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// Remove and return the last element stored in the key list
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<T?> ListRightPopAsync<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = await Db.ListRightPopAsync(redisKey);
            return string.IsNullOrEmpty(json) ? default : RedisFactory.Deserialize<T>(json);
        }

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// Insert a value at the end of the list. If the key does not exist, create it first before inserting the value
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> ListRightPushAsync<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListRightPushAsync(redisKey, RedisFactory.Serialize(redisValue));
        }

        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// Insert a value at the head of the list. If the key does not exist, create it first before inserting the value
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public async Task<long> ListLeftPushAsync<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListLeftPushAsync(redisKey, RedisFactory.Serialize(redisValue));
        }

        #endregion

        #endregion

        #region SortedSet

        /// <summary>
        /// SortedSet Add
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="member"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public bool SortedSetAdd(string redisKey, string member, double score)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.SortedSetAdd(redisKey, member, score);
        }

        /// <summary>
        /// 在有序集合中返回指定范围的元素，默认情况下从低到高。
        /// Return elements within the specified range from an ordered set, by default from low to high.
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public IEnumerable<RedisValue> SortedSetRangeByRank(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.SortedSetRangeByRank(redisKey);
        }

        /// <summary>
        /// 返回有序集合的元素个数
        /// Return the number of elements in the ordered set
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public long SortedSetLength(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.SortedSetLength(redisKey);
        }

        /// <summary>
        /// 返回有序集合的元素个数
        /// Return the number of elements in the ordered set
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="memebr"></param>
        /// <returns></returns>
        public bool SortedSetLength(string redisKey, string memebr)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.SortedSetRemove(redisKey, memebr);
        }

        /// <summary>
        /// SortedSet Add
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="member"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public bool SortedSetAdd<T>(string redisKey, T member, double score)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = RedisFactory.Serialize(member);

            return Db.SortedSetAdd(redisKey, json, score);
        }

        #region SortedSet-Async

        /// <summary>
        /// SortedSet Add
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="member"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public async Task<bool> SortedSetAddAsync(string redisKey, string member, double score)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.SortedSetAddAsync(redisKey, member, score);
        }

        /// <summary>
        /// 在有序集合中返回指定范围的元素，默认情况下从低到高。
        /// Return elements within the specified range from an ordered set, by default from low to high.
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> SortedSetRangeByRankAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.SortedSetRangeByRankAsync(redisKey);
        }

        /// <summary>
        /// 返回有序集合的元素个数
        /// Return the number of elements in the ordered set
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<long> SortedSetLengthAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.SortedSetLengthAsync(redisKey);
        }

        /// <summary>
        /// 返回有序集合的元素个数
        /// Return the number of elements in the ordered set
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="memebr"></param>
        /// <returns></returns>
        public async Task<bool> SortedSetRemoveAsync(string redisKey, string memebr)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.SortedSetRemoveAsync(redisKey, memebr);
        }

        /// <summary>
        /// SortedSet 新增
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="member"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public async Task<bool> SortedSetAddAsync<T>(string redisKey, T member, double score)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = RedisFactory.Serialize(member);

            return await Db.SortedSetAddAsync(redisKey, json, score);
        }

        #endregion

        #endregion

        #region Distributed Lock

        /// <summary>
        /// 尝试获取分布式锁（SET NX + TTL）。value 为持有者 token，释放时必须匹配。
        /// </summary>
        /// <param name="redisKey">锁键（会自动加 Prefix）</param>
        /// <param name="token">持有者唯一标识</param>
        /// <param name="expiry">锁过期时间，必须大于业务临界区</param>
        /// <returns>是否成功获取</returns>
        public bool LockTake(string redisKey, string token, TimeSpan expiry)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Lock token is required.", nameof(token));
            }
            if (expiry <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(expiry), "Lock expiry must be positive.");
            }
            redisKey = AddKeyPrefix(redisKey);
            return Db.StringSet(redisKey, token, expiry, When.NotExists);
        }

        /// <summary>
        /// 释放分布式锁：仅当 value 与 token 一致时删除（Lua 原子比对）。
        /// </summary>
        /// <param name="redisKey">锁键</param>
        /// <param name="token">获取锁时的 token</param>
        /// <returns>是否成功释放（false 表示已过期或非本人持有）</returns>
        public bool LockRelease(string redisKey, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            redisKey = AddKeyPrefix(redisKey);
            // KEYS[1]=key, ARGV[1]=token；匹配则 DEL
            const string script = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
  return redis.call('del', KEYS[1])
else
  return 0
end";
            RedisResult result = Db.ScriptEvaluate(script, new RedisKey[] { redisKey }, new RedisValue[] { token });
            return (int)result > 0;
        }

        /// <summary>
        /// 续期分布式锁：仅当 value 与 token 一致时延长 TTL（看门狗用）。
        /// </summary>
        /// <param name="redisKey">锁键</param>
        /// <param name="token">持有者唯一标识</param>
        /// <param name="expiry">新的过期时间</param>
        /// <returns>true=续期成功；false=锁已过期或非本人持有</returns>
        public bool LockExtend(string redisKey, string token, TimeSpan expiry)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            if (expiry <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(expiry), "Lock expiry must be positive.");
            }
            redisKey = AddKeyPrefix(redisKey);
            // KEYS[1]=key, ARGV[1]=token, ARGV[2]=毫秒；匹配则 PEXPIRE
            const string script = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
  return redis.call('pexpire', KEYS[1], ARGV[2])
else
  return 0
end";
            RedisResult result = Db.ScriptEvaluate(script, new RedisKey[] { redisKey }, new RedisValue[] { token, (long)expiry.TotalMilliseconds });
            return (int)result > 0;
        }

        /// <summary>
        /// 异步续期分布式锁。
        /// </summary>
        public async Task<bool> LockExtendAsync(string redisKey, string token, TimeSpan expiry)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            if (expiry <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(expiry), "Lock expiry must be positive.");
            }
            redisKey = AddKeyPrefix(redisKey);
            const string script = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
  return redis.call('pexpire', KEYS[1], ARGV[2])
else
  return 0
end";
            RedisResult result = await Db.ScriptEvaluateAsync(script, new RedisKey[] { redisKey }, new RedisValue[] { token, (long)expiry.TotalMilliseconds });
            return (int)result > 0;
        }

        /// <summary>
        /// 异步尝试获取分布式锁。
        /// </summary>
        public async Task<bool> LockTakeAsync(string redisKey, string token, TimeSpan expiry)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Lock token is required.", nameof(token));
            }
            if (expiry <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(expiry), "Lock expiry must be positive.");
            }
            redisKey = AddKeyPrefix(redisKey);
            return await Db.StringSetAsync(redisKey, token, expiry, When.NotExists);
        }

        /// <summary>
        /// 异步释放分布式锁。
        /// </summary>
        public async Task<bool> LockReleaseAsync(string redisKey, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            redisKey = AddKeyPrefix(redisKey);
            const string script = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
  return redis.call('del', KEYS[1])
else
  return 0
end";
            RedisResult result = await Db.ScriptEvaluateAsync(script, new RedisKey[] { redisKey }, new RedisValue[] { token });
            return (int)result > 0;
        }

        #endregion

        #region key

        /// <summary>
        /// 移除指定 Key
        /// Remove specified key
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public bool KeyDelete(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.KeyDelete(redisKey);
        }

        /// <summary>
        /// 移除指定 Key
        /// Remove specified key
        /// </summary>
        /// <param name="redisKeys"></param>
        /// <returns></returns>
        public long KeyDelete(IEnumerable<string> redisKeys)
        {
            var keys = redisKeys.Select(x => (RedisKey)AddKeyPrefix(x));
            return Db.KeyDelete(keys.ToArray());
        }

        /// <summary>
        /// 校验 Key 是否存在
        /// Verify if the key exists
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public bool KeyExists(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.KeyExists(redisKey);
        }

        /// <summary>
        /// 重命名 Key
        /// Rename Key
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisNewKey"></param>
        /// <returns></returns>
        public bool KeyRename(string redisKey, string redisNewKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.KeyRename(redisKey, redisNewKey);
        }

        /// <summary>
        /// 设置 Key 的时间
        /// Set the key time
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public bool KeyExpire(string redisKey, TimeSpan? expiry)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.KeyExpire(redisKey, expiry);
        }

        #region key-async

        /// <summary>
        /// 移除指定 Key
        /// Remove specified key
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<bool> KeyDeleteAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.KeyDeleteAsync(redisKey);
        }

        /// <summary>
        /// 移除指定 Key
        /// Remove specified key
        /// </summary>
        /// <param name="redisKeys"></param>
        /// <returns></returns>
        public async Task<long> KeyDeleteAsync(IEnumerable<string> redisKeys)
        {
            var keys = redisKeys.Select(x => (RedisKey)AddKeyPrefix(x));
            return await Db.KeyDeleteAsync(keys.ToArray());
        }

        /// <summary>
        /// 校验 Key 是否存在
        /// Verify if the key exists
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public async Task<bool> KeyExistsAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.KeyExistsAsync(redisKey);
        }

        /// <summary>
        /// 重命名 Key
        /// Rename Key
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisNewKey"></param>
        /// <returns></returns>
        public async Task<bool> KeyRenameAsync(string redisKey, string redisNewKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.KeyRenameAsync(redisKey, redisNewKey);
        }

        /// <summary>
        /// 设置 Key 的时间
        /// Set the key time
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task<bool> KeyExpireAsync(string redisKey, TimeSpan? expiry)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.KeyExpireAsync(redisKey, expiry);
        }

        #endregion

        #endregion

        #region 发布订阅 (Publish and subscribe)

        /// <summary>
        /// 订阅
        /// subscribe
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        public void Subscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handle)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            sub.Subscribe(channel, handle);
        }

        /// <summary>
        /// 发布
        /// Publish
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public long Publish(RedisChannel channel, RedisValue message)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            return sub.Publish(channel, message);
        }

        /// <summary>
        /// 发布（使用序列化）
        /// Publish
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public long Publish<T>(RedisChannel channel, T message)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            return sub.Publish(channel, RedisFactory.Serialize(message));
        }

        #region 发布订阅-async (Publish and subscribe - async)

        /// <summary>
        /// 订阅
        /// subscribe
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        public async Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handle)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            await sub.SubscribeAsync(channel, handle);
        }

        /// <summary>
        /// 发布
        /// Publish
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<long> PublishAsync(RedisChannel channel, RedisValue message)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            return await sub.PublishAsync(channel, message);
        }

        /// <summary>
        /// 发布（使用序列化）
        /// Publish
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<long> PublishAsync<T>(RedisChannel channel, T message)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            return await sub.PublishAsync(channel, RedisFactory.Serialize(message));
        }

        #endregion

        #endregion

    }
}
