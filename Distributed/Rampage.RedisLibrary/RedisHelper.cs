using StackExchange.Redis;
using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// Redis 操作助手：封装 String/Hash/List/SortedSet/Key 与发布订阅的常用操作。
    /// Redis helper wrapping common String/Hash/List/SortedSet/Key and pub-sub operations.
    /// </summary>
    /// <remarks>
    /// 所有键操作自动拼接 "PrefixKey:" 前缀（发布订阅频道名不加前缀）；不提供 Set（集合）类型操作。
    /// Every key operation prepends the "PrefixKey:" prefix automatically (pub-sub channel names are not prefixed); Set (collection) type operations are not provided.
    /// 泛型重载通过 <see cref="RedisFactory.Serialize"/>/<see cref="RedisFactory.Deserialize"/> 以 JSON 存取。
    /// Generic overloads store values as JSON via <see cref="RedisFactory.Serialize"/>/<see cref="RedisFactory.Deserialize"/>.
    /// </remarks>
    public class RedisHelper
    {

        /// <summary>
        /// 当前 Redis 逻辑库 / The current Redis logical database
        /// </summary>
        public IDatabase Db
        {
            get;
            private set;
        }

        /// <summary>
        /// 键前缀，所有键自动拼接 "PrefixKey:" / Key prefix prepended to every key
        /// </summary>
        private string? PrefixKey
        {
            get;
            set;
        }

        /// <summary>
        /// 当前连接复用器 / The current connection multiplexer
        /// </summary>
        public IConnectionMultiplexer ConnMultiplexer
        {
            get;
            private set;
        }

        /// <summary>
        /// 按客户端名构建助手；name 为空时使用第一个客户端配置。
        /// Construct the helper by client name; an empty name uses the first configured client.
        /// </summary>
        /// <param name="name">RedisFactory:Clients 中的客户端名称 / Client name in RedisFactory:Clients</param>
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
        /// 为键拼接 "PrefixKey:" 前缀；未配置前缀时原样返回 / Prepend the "PrefixKey:" prefix; returns the key unchanged when no prefix is configured
        /// </summary>
        /// <param name="key">原始键 / The original key</param>
        /// <returns>带前缀的键 / The prefixed key</returns>
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
        /// 获取带 Prefix 的完整键（供脚本类操作使用）/ Get the fully prefixed key (for script-style operations)
        /// </summary>
        /// <param name="key">原始键 / The original key</param>
        /// <returns>带 Prefix 的完整键 / The fully prefixed key</returns>
        public string GetFullKey(string key)
        {
            return AddKeyPrefix(key);
        }

        #region String

        /// <summary>
        /// 写入字符串（覆盖已有值）/ Set a string value (overwrites any existing value)
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <param name="expiry">过期时间，null 表示永不过期 / Expiry; null means no expiry</param>
        /// <returns>是否写入成功 / Whether the set succeeded</returns>
        public bool StringSet(string redisKey, string redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.StringSet(redisKey, redisValue, expiry, When.Always);
        }

        /// <summary>
        /// 仅当键不存在时写入（SET NX），可用于幂等占位 / Set only when the key does not exist (SET NX); usable as an idempotency marker
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <param name="expiry">过期时间，null 表示永不过期 / Expiry; null means no expiry</param>
        /// <returns>true=占位成功；false=键已存在 / true when set; false when the key already exists</returns>
        public bool StringSetNx(string redisKey, string redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.StringSet(redisKey, redisValue, expiry, When.NotExists);
        }

        /// <summary>
        /// 批量写入键值对（已有键被覆盖）/ Set multiple key-value pairs in one call (existing keys are overwritten)
        /// </summary>
        /// <param name="keyValuePairs">键值对集合，键自动加前缀 / The pairs to set; keys are prefixed automatically</param>
        /// <returns>是否全部成功 / Whether the batch succeeded</returns>
        public bool StringSet(IEnumerable<KeyValuePair<RedisKey, RedisValue>> keyValuePairs)
        {
            keyValuePairs = keyValuePairs.Select(x => new KeyValuePair<RedisKey, RedisValue>(AddKeyPrefix(x.Key!), x.Value));
            return Db.StringSet(keyValuePairs.ToArray());
        }

        /// <summary>
        /// 读取字符串 / Get a string value
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>键值；键不存在返回 null / The value; null when the key does not exist</returns>
        public string? StringGet(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.StringGet(redisKey);
        }

        /// <summary>
        /// 写入对象（JSON 序列化后存储）/ Set an object (stored as serialized JSON)
        /// </summary>
        /// <typeparam name="T">值类型 / Value type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <param name="expiry">过期时间，null 表示永不过期 / Expiry; null means no expiry</param>
        /// <returns>是否写入成功 / Whether the set succeeded</returns>
        public bool StringSet<T>(string redisKey, T redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            var json = RedisFactory.Serialize(redisValue);
            return Db.StringSet(redisKey, json, expiry, When.Always);
        }

        /// <summary>
        /// 读取对象（JSON 反序列化）/ Get an object (deserialized from JSON)
        /// </summary>
        /// <typeparam name="T">目标类型 / Target type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>反序列化结果；键不存在或内容为空返回 default(T) / The deserialized object; default(T) when missing or empty</returns>
        public T? StringGet<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = Db.StringGet(redisKey);
            return string.IsNullOrEmpty(json) ? default(T) : RedisFactory.Deserialize<T>(json);
        }

        #region Async

        /// <summary>
        /// 异步写入字符串（覆盖已有值）/ Asynchronously set a string value (overwrites any existing value)
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <param name="expiry">过期时间，null 表示永不过期 / Expiry; null means no expiry</param>
        /// <returns>是否写入成功 / Whether the set succeeded</returns>
        public async Task<bool> StringSetAsync(string redisKey, string redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.StringSetAsync(redisKey, redisValue, expiry, When.Always);
        }

        /// <summary>
        /// 异步批量写入键值对（已有键被覆盖）/ Asynchronously set multiple key-value pairs in one call (existing keys are overwritten)
        /// </summary>
        /// <param name="keyValuePairs">键值对集合，键自动加前缀 / The pairs to set; keys are prefixed automatically</param>
        /// <returns>是否全部成功 / Whether the batch succeeded</returns>
        public async Task<bool> StringSetAsync(IEnumerable<KeyValuePair<RedisKey, RedisValue>> keyValuePairs)
        {
            keyValuePairs = keyValuePairs.Select(x => new KeyValuePair<RedisKey, RedisValue>(AddKeyPrefix(x.Key!), x.Value));
            return await Db.StringSetAsync(keyValuePairs.ToArray());
        }

        /// <summary>
        /// 异步读取字符串 / Asynchronously get a string value
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>值；键不存在时为 null / The value; null when the key does not exist</returns>
        public async Task<string?> StringGetAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.StringGetAsync(redisKey);
        }

        /// <summary>
        /// 异步写入对象（JSON 序列化后存储）/ Asynchronously set an object (stored as serialized JSON)
        /// </summary>
        /// <typeparam name="T">值类型 / Value type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <param name="expiry">过期时间，null 表示永不过期 / Expiry; null means no expiry</param>
        /// <returns>是否写入成功 / Whether the set succeeded</returns>
        public async Task<bool> StringSetAsync<T>(string redisKey, T redisValue, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            var json = RedisFactory.Serialize(redisValue);
            return await Db.StringSetAsync(redisKey, json, expiry, When.Always);
        }

        /// <summary>
        /// 异步读取对象（JSON 反序列化）/ Asynchronously get an object (deserialized from JSON)
        /// </summary>
        /// <typeparam name="T">目标类型 / Target type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="expiry">命中后刷新的过期时间（滑动过期）；null 不刷新 / When set, refreshes the TTL after a hit (sliding expiry); null leaves the TTL untouched</param>
        /// <returns>反序列化结果；键不存在或内容为空返回 default(T) / The deserialized object; default(T) when missing or empty</returns>
        public async Task<T?> StringGetAsync<T>(string redisKey, TimeSpan? expiry = null)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = await Db.StringGetAsync(redisKey);
            if (expiry != null)
            {
                await Db.KeyExpireAsync(redisKey, expiry);
            }
            return string.IsNullOrEmpty(json) ? default(T) : RedisFactory.Deserialize<T>(json);
        }

        #endregion

        #endregion

        #region Hash

        /// <summary>
        /// 判断 Hash 字段是否存在 / Check whether a hash field exists
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <returns>存在返回 true / true when the field exists</returns>
        public bool HashExists(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashExists(redisKey, hashField);
        }

        /// <summary>
        /// 删除单个 Hash 字段 / Delete a single hash field
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <returns>true=字段存在并已删除；false=字段不存在 / true when the field existed and was removed; false when absent</returns>
        public bool HashDelete(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashDelete(redisKey, hashField);
        }

        /// <summary>
        /// 批量删除 Hash 字段 / Delete multiple hash fields
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名集合 / Field names</param>
        /// <returns>实际删除的字段数 / The number of fields actually removed</returns>
        public long HashDelete(string redisKey, IEnumerable<RedisValue> hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashDelete(redisKey, hashField.ToArray());
        }

        /// <summary>
        /// 写入 Hash 字段 / Set a hash field
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <param name="value">字段值 / Field value</param>
        /// <returns>true=新增字段；false=更新已有字段 / true when the field is new; false when an existing field was updated</returns>
        public bool HashSet(string redisKey, string hashField, string value)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashSet(redisKey, hashField, value);
        }

        /// <summary>
        /// 批量写入 Hash 字段 / Set multiple hash fields
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashFields">字段与值的集合 / Fields and values</param>
        public void HashSet(string redisKey, IEnumerable<HashEntry> hashFields)
        {
            redisKey = AddKeyPrefix(redisKey);
            Db.HashSet(redisKey, hashFields.ToArray());
        }

        /// <summary>
        /// 读取 Hash 字段值 / Get a hash field value
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <returns>字段值；字段不存在返回 Null / The value; Null when the field does not exist</returns>
        public RedisValue HashGet(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashGet(redisKey, hashField);
        }

        /// <summary>
        /// 批量读取 Hash 字段值 / Get multiple hash field values
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名数组 / Field names</param>
        /// <returns>与字段顺序对应的值，不存在的字段为 Null / Values in field order; missing fields are Null</returns>
        public RedisValue[] HashGet(string redisKey, RedisValue[] hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashGet(redisKey, hashField);
        }

        /// <summary>
        /// 读取 Hash 全部字段名 / Get all hash field names
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>字段名集合；键不存在返回空 / Field names; empty when the key does not exist</returns>
        public IEnumerable<RedisValue> HashKeys(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashKeys(redisKey);
        }

        /// <summary>
        /// 读取 Hash 全部字段值 / Get all hash field values
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>字段值数组；键不存在返回空数组 / Values; an empty array when the key does not exist</returns>
        public RedisValue[] HashValues(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.HashValues(redisKey);
        }

        /// <summary>
        /// 写入 Hash 字段（值 JSON 序列化）/ Set a hash field (value stored as JSON)
        /// </summary>
        /// <typeparam name="T">值类型 / Value type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <param name="value">值 / Value</param>
        /// <returns>true=新增字段；false=更新已有字段 / true when the field is new; false when an existing field was updated</returns>
        public bool HashSet<T>(string redisKey, string hashField, T value)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = RedisFactory.Serialize(value);
            return Db.HashSet(redisKey, hashField, json);
        }

        /// <summary>
        /// 读取 Hash 字段并反序列化 / Get and deserialize a hash field
        /// </summary>
        /// <typeparam name="T">目标类型 / Target type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <returns>反序列化结果；字段不存在或为空返回 default(T) / The deserialized object; default(T) when missing or empty</returns>
        public T? HashGet<T>(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = Db.HashGet(redisKey, hashField);
            return string.IsNullOrEmpty(json) ? default(T) : RedisFactory.Deserialize<T>(json);
        }

        #region async

        /// <summary>
        /// 异步判断 Hash 字段是否存在 / Asynchronously check whether a hash field exists
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <returns>存在返回 true / true when the field exists</returns>
        public async Task<bool> HashExistsAsync(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashExistsAsync(redisKey, hashField);
        }

        /// <summary>
        /// 异步删除单个 Hash 字段 / Asynchronously delete a single hash field
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <returns>true=字段存在并已删除；false=字段不存在 / true when the field existed and was removed; false when absent</returns>
        public async Task<bool> HashDeleteAsync(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashDeleteAsync(redisKey, hashField);
        }

        /// <summary>
        /// 异步批量删除 Hash 字段 / Asynchronously delete multiple hash fields
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名集合 / Field names</param>
        /// <returns>实际删除的字段数 / The number of fields actually removed</returns>
        public async Task<long> HashDeleteAsync(string redisKey, IEnumerable<RedisValue> hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashDeleteAsync(redisKey, hashField.ToArray());
        }

        /// <summary>
        /// 异步写入 Hash 字段 / Asynchronously set a hash field
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <param name="value">字段值 / Field value</param>
        /// <returns>true=新增字段；false=更新已有字段 / true when the field is new; false when an existing field was updated</returns>
        public async Task<bool> HashSetAsync(string redisKey, string hashField, string value)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashSetAsync(redisKey, hashField, value);
        }

        /// <summary>
        /// 异步批量写入 Hash 字段 / Asynchronously set multiple hash fields
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashFields">字段与值的集合 / Fields and values</param>
        public async Task HashSetAsync(string redisKey, IEnumerable<HashEntry> hashFields)
        {
            redisKey = AddKeyPrefix(redisKey);
            await Db.HashSetAsync(redisKey, hashFields.ToArray());
        }

        /// <summary>
        /// 异步读取 Hash 字段值 / Asynchronously get a hash field value
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <returns>字段值；字段不存在返回 Null / The value; Null when the field does not exist</returns>
        public async Task<RedisValue> HashGetAsync(string redisKey, string hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashGetAsync(redisKey, hashField);
        }

        /// <summary>
        /// 异步批量读取 Hash 字段值 / Asynchronously get multiple hash field values
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名数组 / Field names</param>
        /// <returns>与字段顺序对应的值，不存在的字段为 Null / Values in field order; missing fields are Null</returns>
        public async Task<IEnumerable<RedisValue>> HashGetAsync(string redisKey, RedisValue[] hashField)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashGetAsync(redisKey, hashField);
        }

        /// <summary>
        /// 异步读取 Hash 全部字段名 / Asynchronously get all hash field names
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>字段名集合；键不存在返回空 / Field names; empty when the key does not exist</returns>
        public async Task<IEnumerable<RedisValue>> HashKeysAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashKeysAsync(redisKey);
        }

        /// <summary>
        /// 异步读取 Hash 全部字段值 / Asynchronously get all hash field values
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>字段值集合；键不存在返回空 / Values; empty when the key does not exist</returns>
        public async Task<IEnumerable<RedisValue>> HashValuesAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.HashValuesAsync(redisKey);
        }

        /// <summary>
        /// 异步写入 Hash 字段（值 JSON 序列化）/ Asynchronously set a hash field (value stored as JSON)
        /// </summary>
        /// <typeparam name="T">值类型 / Value type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <param name="value">值 / Value</param>
        /// <returns>true=新增字段；false=更新已有字段 / true when the field is new; false when an existing field was updated</returns>
        public async Task<bool> HashSetAsync<T>(string redisKey, string hashField, T value)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = RedisFactory.Serialize(value);
            return await Db.HashSetAsync(redisKey, hashField, json);
        }

        /// <summary>
        /// 异步读取 Hash 字段并反序列化 / Asynchronously get and deserialize a hash field
        /// </summary>
        /// <typeparam name="T">目标类型 / Target type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="hashField">字段名 / Field name</param>
        /// <returns>反序列化结果；字段不存在或为空返回 default(T) / The deserialized object; default(T) when missing or empty</returns>
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
        /// 移除并返回列表头元素 / Pop and return the head element of the list
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>头元素；列表为空或不存在返回 null / The head element; null when the list is empty or missing</returns>
        public string? ListLeftPop(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListLeftPop(redisKey);
        }

        /// <summary>
        /// 移除并返回列表尾元素 / Pop and return the tail element of the list
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>尾元素；列表为空或不存在返回 null / The tail element; null when the list is empty or missing</returns>
        public string? ListRightPop(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListRightPop(redisKey);
        }

        /// <summary>
        /// 移除列表中与该值相等的元素 / Remove elements equal to the given value from the list
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">要移除的值 / The value to remove</param>
        /// <returns>实际移除的元素数 / The number of elements removed</returns>
        public long ListRemove(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListRemove(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表尾部插入值；键不存在时先创建 / Insert a value at the tail of the list; creates the key first when absent
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <returns>插入后的列表长度 / The list length after the push</returns>
        public long ListRightPush(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListRightPush(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表头部插入值；键不存在时先创建 / Insert a value at the head of the list; creates the key first when absent
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <returns>插入后的列表长度 / The list length after the push</returns>
        public long ListLeftPush(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListLeftPush(redisKey, redisValue);
        }

        /// <summary>
        /// 返回列表长度，键不存在返回 0 / Return the list length; 0 when the key does not exist
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>列表长度 / The list length</returns>
        public long ListLength(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListLength(redisKey);
        }

        /// <summary>
        /// 返回列表全部元素 / Return all elements of the list
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>全部元素；键不存在返回空数组 / All elements; an empty array when the key does not exist</returns>
        public IEnumerable<RedisValue> ListRange(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListRange(redisKey);
        }

        /// <summary>
        /// 移除并返回列表头元素并反序列化 / Pop, return and deserialize the head element of the list
        /// </summary>
        /// <typeparam name="T">目标类型 / Target type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>反序列化结果；列表为空返回 default(T) / The deserialized element; default(T) when the list is empty</returns>
        public T? ListLeftPop<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = Db.ListLeftPop(redisKey);
            return string.IsNullOrEmpty(json) ? default(T) : RedisFactory.Deserialize<T>(json);
        }

        /// <summary>
        /// 移除并返回列表尾元素并反序列化 / Pop, return and deserialize the tail element of the list
        /// </summary>
        /// <typeparam name="T">目标类型 / Target type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>反序列化结果；列表为空返回 default(T) / The deserialized element; default(T) when the list is empty</returns>
        public T? ListRightPop<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = Db.ListRightPop(redisKey);
            return string.IsNullOrEmpty(json) ? default(T) : RedisFactory.Deserialize<T>(json);
        }

        /// <summary>
        /// 在列表尾部插入对象（JSON 序列化）；键不存在时先创建 / Insert an object at the tail (JSON serialized); creates the key first when absent
        /// </summary>
        /// <typeparam name="T">值类型 / Value type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <returns>插入后的列表长度 / The list length after the push</returns>
        public long ListRightPush<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListRightPush(redisKey, RedisFactory.Serialize(redisValue));
        }

        /// <summary>
        /// 在列表头部插入对象（JSON 序列化）；键不存在时先创建 / Insert an object at the head (JSON serialized); creates the key first when absent
        /// </summary>
        /// <typeparam name="T">值类型 / Value type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <returns>插入后的列表长度 / The list length after the push</returns>
        public long ListLeftPush<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.ListLeftPush(redisKey, RedisFactory.Serialize(redisValue));
        }

        #region List-async

        /// <summary>
        /// 异步移除并返回列表头元素 / Asynchronously pop and return the head element of the list
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>头元素；列表为空或不存在返回 null / The head element; null when the list is empty or missing</returns>
        public async Task<string?> ListLeftPopAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListLeftPopAsync(redisKey);
        }

        /// <summary>
        /// 异步移除并返回列表尾元素 / Asynchronously pop and return the tail element of the list
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>尾元素；列表为空或不存在返回 null / The tail element; null when the list is empty or missing</returns>
        public async Task<string?> ListRightPopAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListRightPopAsync(redisKey);
        }

        /// <summary>
        /// 异步移除列表中与该值相等的元素 / Asynchronously remove elements equal to the given value from the list
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">要移除的值 / The value to remove</param>
        /// <returns>实际移除的元素数 / The number of elements removed</returns>
        public async Task<long> ListRemoveAsync(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListRemoveAsync(redisKey, redisValue);
        }

        /// <summary>
        /// 异步在列表尾部插入值；键不存在时先创建 / Asynchronously insert a value at the tail of the list; creates the key first when absent
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <returns>插入后的列表长度 / The list length after the push</returns>
        public async Task<long> ListRightPushAsync(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListRightPushAsync(redisKey, redisValue);
        }

        /// <summary>
        /// 异步在列表头部插入值；键不存在时先创建 / Asynchronously insert a value at the head of the list; creates the key first when absent
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <returns>插入后的列表长度 / The list length after the push</returns>
        public async Task<long> ListLeftPushAsync(string redisKey, string redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListLeftPushAsync(redisKey, redisValue);
        }

        /// <summary>
        /// 异步返回列表长度，键不存在返回 0 / Asynchronously return the list length; 0 when the key does not exist
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>列表长度 / The list length</returns>
        public async Task<long> ListLengthAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListLengthAsync(redisKey);
        }

        /// <summary>
        /// 异步返回列表全部元素 / Asynchronously return all elements of the list
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>全部元素；键不存在返回空数组 / All elements; an empty array when the key does not exist</returns>
        public async Task<IEnumerable<RedisValue>> ListRangeAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListRangeAsync(redisKey);
        }

        /// <summary>
        /// 异步移除并返回列表头元素并反序列化 / Asynchronously pop, return and deserialize the head element of the list
        /// </summary>
        /// <typeparam name="T">目标类型 / Target type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>反序列化结果；列表为空返回 default(T) / The deserialized element; default(T) when the list is empty</returns>
        public async Task<T?> ListLeftPopAsync<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = await Db.ListLeftPopAsync(redisKey);
            return string.IsNullOrEmpty(json) ? default : RedisFactory.Deserialize<T>(json);
        }

        /// <summary>
        /// 异步移除并返回列表尾元素并反序列化 / Asynchronously pop, return and deserialize the tail element of the list
        /// </summary>
        /// <typeparam name="T">目标类型 / Target type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>反序列化结果；列表为空返回 default(T) / The deserialized element; default(T) when the list is empty</returns>
        public async Task<T?> ListRightPopAsync<T>(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = await Db.ListRightPopAsync(redisKey);
            return string.IsNullOrEmpty(json) ? default : RedisFactory.Deserialize<T>(json);
        }

        /// <summary>
        /// 异步在列表尾部插入对象（JSON 序列化）；键不存在时先创建 / Asynchronously insert an object at the tail (JSON serialized); creates the key first when absent
        /// </summary>
        /// <typeparam name="T">值类型 / Value type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <returns>插入后的列表长度 / The list length after the push</returns>
        public async Task<long> ListRightPushAsync<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListRightPushAsync(redisKey, RedisFactory.Serialize(redisValue));
        }

        /// <summary>
        /// 异步在列表头部插入对象（JSON 序列化）；键不存在时先创建 / Asynchronously insert an object at the head (JSON serialized); creates the key first when absent
        /// </summary>
        /// <typeparam name="T">值类型 / Value type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="redisValue">值 / Value</param>
        /// <returns>插入后的列表长度 / The list length after the push</returns>
        public async Task<long> ListLeftPushAsync<T>(string redisKey, T redisValue)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.ListLeftPushAsync(redisKey, RedisFactory.Serialize(redisValue));
        }

        #endregion

        #endregion

        #region SortedSet

        /// <summary>
        /// 向有序集合添加成员，已存在则更新分数 / Add a member to the sorted set; updates the score when it already exists
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="member">成员 / Member</param>
        /// <param name="score">分数 / Score</param>
        /// <returns>true=新增成员；false=更新已有成员分数 / true when added; false when an existing member's score was updated</returns>
        public bool SortedSetAdd(string redisKey, string member, double score)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.SortedSetAdd(redisKey, member, score);
        }

        /// <summary>
        /// 按排名返回有序集合全部元素，默认分数从低到高 / Return all sorted-set members by rank; score low to high by default
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>元素集合 / The members</returns>
        public IEnumerable<RedisValue> SortedSetRangeByRank(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.SortedSetRangeByRank(redisKey);
        }

        /// <summary>
        /// 返回有序集合的元素个数 / Return the number of members in the sorted set
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>元素个数，键不存在返回 0 / The member count; 0 when the key does not exist</returns>
        public long SortedSetLength(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.SortedSetLength(redisKey);
        }

        /// <summary>
        /// 从有序集合移除成员 / Remove a member from the sorted set
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="member">成员 / The member</param>
        /// <returns>是否移除了成员（成员不存在返回 false）/ Whether a member was removed (false when absent)</returns>
        public bool SortedSetRemove(string redisKey, string member)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.SortedSetRemove(redisKey, member);
        }

        /// <summary>
        /// 向有序集合添加对象成员（JSON 序列化），已存在则更新分数 / Add an object member (JSON serialized); updates the score when it already exists
        /// </summary>
        /// <typeparam name="T">成员类型 / Member type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="member">成员 / Member</param>
        /// <param name="score">分数 / Score</param>
        /// <returns>true=新增成员；false=更新已有成员分数 / true when added; false when an existing member's score was updated</returns>
        public bool SortedSetAdd<T>(string redisKey, T member, double score)
        {
            redisKey = AddKeyPrefix(redisKey);
            string? json = RedisFactory.Serialize(member);

            return Db.SortedSetAdd(redisKey, json, score);
        }

        #region SortedSet-Async

        /// <summary>
        /// 异步向有序集合添加成员，已存在则更新分数 / Asynchronously add a member to the sorted set; updates the score when it already exists
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="member">成员 / Member</param>
        /// <param name="score">分数 / Score</param>
        /// <returns>true=新增成员；false=更新已有成员分数 / true when added; false when an existing member's score was updated</returns>
        public async Task<bool> SortedSetAddAsync(string redisKey, string member, double score)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.SortedSetAddAsync(redisKey, member, score);
        }

        /// <summary>
        /// 异步按排名返回有序集合全部元素，默认分数从低到高 / Asynchronously return all sorted-set members by rank; score low to high by default
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>元素集合 / The members</returns>
        public async Task<IEnumerable<RedisValue>> SortedSetRangeByRankAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.SortedSetRangeByRankAsync(redisKey);
        }

        /// <summary>
        /// 异步返回有序集合的元素个数 / Asynchronously return the number of members in the sorted set
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>元素个数，键不存在返回 0 / The member count; 0 when the key does not exist</returns>
        public async Task<long> SortedSetLengthAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.SortedSetLengthAsync(redisKey);
        }

        /// <summary>
        /// 从有序集合移除成员 / Remove a member from the sorted set
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="memebr">成员 / Member</param>
        /// <returns>true=移除成功；false=成员不存在 / true when removed; false when the member is absent</returns>
        public async Task<bool> SortedSetRemoveAsync(string redisKey, string memebr)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.SortedSetRemoveAsync(redisKey, memebr);
        }

        /// <summary>
        /// 异步向有序集合添加对象成员（JSON 序列化），已存在则更新分数 / Asynchronously add an object member (JSON serialized); updates the score when it already exists
        /// </summary>
        /// <typeparam name="T">成员类型 / Member type</typeparam>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="member">成员 / Member</param>
        /// <param name="score">分数 / Score</param>
        /// <returns>true=新增成员；false=更新已有成员分数 / true when added; false when an existing member's score was updated</returns>
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
        /// Try to take the distributed lock (SET NX + TTL); the stored value is the holder token required for release.
        /// </summary>
        /// <param name="redisKey">锁键（自动加 Prefix）/ Lock key (prefix applied automatically)</param>
        /// <param name="token">持有者唯一标识 / Unique holder token</param>
        /// <param name="expiry">锁过期时间，必须大于业务临界区 / Lock expiry; must exceed the critical section</param>
        /// <returns>是否成功获取 / Whether the lock was acquired</returns>
        /// <exception cref="ArgumentException">token 为空 / token is empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">expiry 非正数 / expiry is not positive</exception>
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
        /// Release the distributed lock: deletes only when the stored value matches the token (atomic Lua comparison).
        /// </summary>
        /// <param name="redisKey">锁键（自动加 Prefix）/ Lock key (prefix applied automatically)</param>
        /// <param name="token">获取锁时的 token / The token used to acquire the lock</param>
        /// <returns>是否成功释放（false 表示已过期或非本人持有）/ Whether released (false means expired or held by someone else)</returns>
        public bool LockRelease(string redisKey, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            redisKey = AddKeyPrefix(redisKey);
            // KEYS[1]=key, ARGV[1]=token；匹配则 DEL / delete only on token match
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
        /// Extend the distributed lock: renews the TTL only when the stored value matches the token (used by the watchdog).
        /// </summary>
        /// <param name="redisKey">锁键（自动加 Prefix）/ Lock key (prefix applied automatically)</param>
        /// <param name="token">持有者唯一标识 / Unique holder token</param>
        /// <param name="expiry">新的过期时间 / The new expiry</param>
        /// <returns>true=续期成功；false=锁已过期或非本人持有 / true when renewed; false when expired or held by someone else</returns>
        /// <exception cref="ArgumentOutOfRangeException">expiry 非正数 / expiry is not positive</exception>
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
            // KEYS[1]=key, ARGV[1]=token, ARGV[2]=毫秒；匹配则 PEXPIRE / pexpire only on token match
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
        /// 异步续期分布式锁：仅当 value 与 token 一致时延长 TTL（看门狗用）。
        /// Asynchronously extend the distributed lock: renews the TTL only when the stored value matches the token (used by the watchdog).
        /// </summary>
        /// <param name="redisKey">锁键（自动加 Prefix）/ Lock key (prefix applied automatically)</param>
        /// <param name="token">持有者唯一标识 / Unique holder token</param>
        /// <param name="expiry">新的过期时间 / The new expiry</param>
        /// <returns>true=续期成功；false=锁已过期或非本人持有 / true when renewed; false when expired or held by someone else</returns>
        /// <exception cref="ArgumentOutOfRangeException">expiry 非正数 / expiry is not positive</exception>
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
            // KEYS[1]=key, ARGV[1]=token, ARGV[2]=毫秒；匹配则 PEXPIRE / pexpire only on token match
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
        /// 异步尝试获取分布式锁（SET NX + TTL）。value 为持有者 token，释放时必须匹配。
        /// Asynchronously try to take the distributed lock (SET NX + TTL); the stored value is the holder token required for release.
        /// </summary>
        /// <param name="redisKey">锁键（自动加 Prefix）/ Lock key (prefix applied automatically)</param>
        /// <param name="token">持有者唯一标识 / Unique holder token</param>
        /// <param name="expiry">锁过期时间，必须大于业务临界区 / Lock expiry; must exceed the critical section</param>
        /// <returns>是否成功获取 / Whether the lock was acquired</returns>
        /// <exception cref="ArgumentException">token 为空 / token is empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">expiry 非正数 / expiry is not positive</exception>
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
        /// 异步释放分布式锁：仅当 value 与 token 一致时删除（Lua 原子比对）。
        /// Asynchronously release the distributed lock: deletes only when the stored value matches the token (atomic Lua comparison).
        /// </summary>
        /// <param name="redisKey">锁键（自动加 Prefix）/ Lock key (prefix applied automatically)</param>
        /// <param name="token">获取锁时的 token / The token used to acquire the lock</param>
        /// <returns>是否成功释放（false 表示已过期或非本人持有）/ Whether released (false means expired or held by someone else)</returns>
        public async Task<bool> LockReleaseAsync(string redisKey, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            redisKey = AddKeyPrefix(redisKey);
            // KEYS[1]=key, ARGV[1]=token；匹配则 DEL / delete only on token match
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
        /// 删除指定键 / Delete the specified key
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>true=删除成功；false=键不存在 / true when deleted; false when absent</returns>
        public bool KeyDelete(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.KeyDelete(redisKey);
        }

        /// <summary>
        /// 批量删除指定键 / Delete the specified keys in bulk
        /// </summary>
        /// <param name="redisKeys">键集合（自动加 Prefix）/ Keys (prefix applied automatically)</param>
        /// <returns>实际删除的键数 / The number of keys actually deleted</returns>
        public long KeyDelete(IEnumerable<string> redisKeys)
        {
            var keys = redisKeys.Select(x => (RedisKey)AddKeyPrefix(x));
            return Db.KeyDelete(keys.ToArray());
        }

        /// <summary>
        /// 校验键是否存在 / Check whether the key exists
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>存在返回 true / true when the key exists</returns>
        public bool KeyExists(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.KeyExists(redisKey);
        }

        /// <summary>
        /// 重命名键 / Rename a key
        /// </summary>
        /// <param name="redisKey">原键（自动加 Prefix）/ The original key (prefix applied automatically)</param>
        /// <param name="redisNewKey">新键名（不自动加 Prefix）/ The new key name (NOT prefixed automatically)</param>
        /// <returns>true=重命名成功；false=原键不存在 / true when renamed; false when the original key is absent</returns>
        public bool KeyRename(string redisKey, string redisNewKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.KeyRename(redisKey, redisNewKey);
        }

        /// <summary>
        /// 设置键的过期时间 / Set the key's expiry
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="expiry">过期时间；null 表示移除过期时间（持久化）/ Expiry; null removes the expiry (persist)</param>
        /// <returns>true=设置成功；false=键不存在 / true when set; false when the key is absent</returns>
        public bool KeyExpire(string redisKey, TimeSpan? expiry)
        {
            redisKey = AddKeyPrefix(redisKey);
            return Db.KeyExpire(redisKey, expiry);
        }

        #region key-async

        /// <summary>
        /// 异步删除指定键 / Asynchronously delete the specified key
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>true=删除成功；false=键不存在 / true when deleted; false when absent</returns>
        public async Task<bool> KeyDeleteAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.KeyDeleteAsync(redisKey);
        }

        /// <summary>
        /// 异步批量删除指定键 / Asynchronously delete the specified keys in bulk
        /// </summary>
        /// <param name="redisKeys">键集合（自动加 Prefix）/ Keys (prefix applied automatically)</param>
        /// <returns>实际删除的键数 / The number of keys actually deleted</returns>
        public async Task<long> KeyDeleteAsync(IEnumerable<string> redisKeys)
        {
            var keys = redisKeys.Select(x => (RedisKey)AddKeyPrefix(x));
            return await Db.KeyDeleteAsync(keys.ToArray());
        }

        /// <summary>
        /// 异步校验键是否存在 / Asynchronously check whether the key exists
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <returns>存在返回 true / true when the key exists</returns>
        public async Task<bool> KeyExistsAsync(string redisKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.KeyExistsAsync(redisKey);
        }

        /// <summary>
        /// 异步重命名键 / Asynchronously rename a key
        /// </summary>
        /// <param name="redisKey">原键（自动加 Prefix）/ The original key (prefix applied automatically)</param>
        /// <param name="redisNewKey">新键名（不自动加 Prefix）/ The new key name (NOT prefixed automatically)</param>
        /// <returns>true=重命名成功；false=原键不存在 / true when renamed; false when the original key is absent</returns>
        public async Task<bool> KeyRenameAsync(string redisKey, string redisNewKey)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.KeyRenameAsync(redisKey, redisNewKey);
        }

        /// <summary>
        /// 异步设置键的过期时间 / Asynchronously set the key's expiry
        /// </summary>
        /// <param name="redisKey">键（自动加 Prefix）/ Key (prefix applied automatically)</param>
        /// <param name="expiry">过期时间；null 表示移除过期时间（持久化）/ Expiry; null removes the expiry (persist)</param>
        /// <returns>true=设置成功；false=键不存在 / true when set; false when the key is absent</returns>
        public async Task<bool> KeyExpireAsync(string redisKey, TimeSpan? expiry)
        {
            redisKey = AddKeyPrefix(redisKey);
            return await Db.KeyExpireAsync(redisKey, expiry);
        }

        #endregion

        #endregion

        #region 发布订阅 (Publish and subscribe)

        /// <summary>
        /// 订阅频道（频道名不加 Prefix 前缀）/ Subscribe to a channel (channel names are not prefixed)
        /// </summary>
        /// <param name="channel">频道 / Channel</param>
        /// <param name="handle">消息回调 / Message handler</param>
        public void Subscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handle)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            sub.Subscribe(channel, handle);
        }

        /// <summary>
        /// 发布消息（频道名不加 Prefix 前缀）/ Publish a message (channel names are not prefixed)
        /// </summary>
        /// <param name="channel">频道 / Channel</param>
        /// <param name="message">消息 / Message</param>
        /// <returns>接收到该消息的客户端数 / The number of clients that received the message</returns>
        public long Publish(RedisChannel channel, RedisValue message)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            return sub.Publish(channel, message);
        }

        /// <summary>
        /// 发布对象（JSON 序列化后发送，频道名不加 Prefix 前缀）/ Publish an object (sent as JSON; channel names are not prefixed)
        /// </summary>
        /// <typeparam name="T">消息类型 / Message type</typeparam>
        /// <param name="channel">频道 / Channel</param>
        /// <param name="message">消息 / Message</param>
        /// <returns>接收到该消息的客户端数 / The number of clients that received the message</returns>
        public long Publish<T>(RedisChannel channel, T message)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            return sub.Publish(channel, RedisFactory.Serialize(message));
        }

        #region 发布订阅-async (Publish and subscribe - async)

        /// <summary>
        /// 异步订阅频道（频道名不加 Prefix 前缀）/ Asynchronously subscribe to a channel (channel names are not prefixed)
        /// </summary>
        /// <param name="channel">频道 / Channel</param>
        /// <param name="handle">消息回调 / Message handler</param>
        public async Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handle)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            await sub.SubscribeAsync(channel, handle);
        }

        /// <summary>
        /// 异步发布消息（频道名不加 Prefix 前缀）/ Asynchronously publish a message (channel names are not prefixed)
        /// </summary>
        /// <param name="channel">频道 / Channel</param>
        /// <param name="message">消息 / Message</param>
        /// <returns>接收到该消息的客户端数 / The number of clients that received the message</returns>
        public async Task<long> PublishAsync(RedisChannel channel, RedisValue message)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            return await sub.PublishAsync(channel, message);
        }

        /// <summary>
        /// 异步发布对象（JSON 序列化后发送，频道名不加 Prefix 前缀）/ Asynchronously publish an object (sent as JSON; channel names are not prefixed)
        /// </summary>
        /// <typeparam name="T">消息类型 / Message type</typeparam>
        /// <param name="channel">频道 / Channel</param>
        /// <param name="message">消息 / Message</param>
        /// <returns>接收到该消息的客户端数 / The number of clients that received the message</returns>
        public async Task<long> PublishAsync<T>(RedisChannel channel, T message)
        {
            ISubscriber sub = ConnMultiplexer.GetSubscriber();
            return await sub.PublishAsync(channel, RedisFactory.Serialize(message));
        }

        #endregion

        #endregion

    }
}
