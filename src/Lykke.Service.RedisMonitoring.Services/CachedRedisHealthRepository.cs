using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Service.RedisMonitoring.Client.Models;
using Lykke.Service.RedisMonitoring.Core.Repositories;
using Lykke.Service.RedisMonitoring.Core.Services;
using StackExchange.Redis;

namespace Lykke.Service.RedisMonitoring.Services
{
    public class CachedRedisHealthRepository : ICachedRedisHealthRepository
    {
        private const string _dataFormat = "yyyy-MM-dd-HH-mm-ss-fffffff";
        private const string _instanceSetKeyPattern = "RedisMonitoring:{0}";
        private const string _objKeySuffix = "time:{0}";

        private readonly IRedisHealthRepository _redisHealthRepository;
        private readonly IDatabase _db;
        private readonly TimeSpan _historyDuration;
        private readonly IEnumerable<string> _redises;

        public CachedRedisHealthRepository(
            IRedisHealthRepository redisHealthRepository,
            IConnectionMultiplexer connectionMultiplexer,
            TimeSpan historyDuration,
            IEnumerable<string> redises)
        {
            _redisHealthRepository = redisHealthRepository;
            _db = connectionMultiplexer.GetDatabase();
            _historyDuration = historyDuration;
            _redises = redises;
        }

        public async Task InitCacheAsync()
        {
            var redisHealths = await _redisHealthRepository.GetAllAsync();

            foreach (var redisHealth in redisHealths)
            {
                foreach (var pingInfo in redisHealth.HealthChecks)
                {
                    if (pingInfo.Timestamp.Add(_historyDuration) < DateTime.UtcNow)
                        continue;

                    var ttl = pingInfo.Timestamp.Add(_historyDuration).Subtract(DateTime.UtcNow);
                    if (ttl.Ticks < 0)
                        continue;

                    var item = new CacheRedisHealthModel
                    {
                        Name = redisHealth.Name,
                        PingInfo = pingInfo,
                    };
                    var instanceKey = string.Format(_instanceSetKeyPattern, redisHealth.Name);
                    var objSuffix = string.Format(_objKeySuffix, pingInfo.Timestamp.ToString(_dataFormat));
                    var objKey = $"{instanceKey}:{objSuffix}";

                    var tx = _db.CreateTransaction();
                    tx.AddCondition(Condition.KeyNotExists(objKey));
                    var tasks = new List<Task>
                    {
                        tx.SortedSetAddAsync(instanceKey, objSuffix, DateTime.UtcNow.Ticks)
                    };
                    var setTask = tx.StringSetAsync(objKey, item.ToJson(), ttl);
                    tasks.Add(setTask);
                    if (await tx.ExecuteAsync())
                        await Task.WhenAll(tasks);
                }
            }
        }

        public async Task SaveAsync(PingInfo pingInfo, string redisName)
        {
            var instanceKey = string.Format(_instanceSetKeyPattern, redisName);
            var objSuffix = string.Format(_objKeySuffix, pingInfo.Timestamp.ToString(_dataFormat));
            var item = new CacheRedisHealthModel
            {
                Name = redisName,
                PingInfo = pingInfo,
            };
            var key = $"{instanceKey}:{objSuffix}";

            var tx = _db.CreateTransaction();
            var tasks = new List<Task>
            {
                tx.SortedSetAddAsync(instanceKey, objSuffix, DateTime.UtcNow.Ticks)
            };
            var setTask = tx.StringSetAsync(key, item.ToJson(), _historyDuration);
            tasks.Add(setTask);
            if (!await tx.ExecuteAsync())
                throw new InvalidOperationException($"Error during adding ping info for redis {redisName}");

            await Task.WhenAll(tasks);
            if (!setTask.Result)
                throw new InvalidOperationException($"Error during adding ping info for redis {redisName}");

            var keys = await GetInstanceKeysAsync(redisName);
            var infoJsons = await _db.StringGetAsync(keys);
            var redisHealth = new RedisHealth
            {
                Name = redisName,
                HealthChecks = infoJsons
                    .Where(a => a.HasValue)
                    .Select(a => a.ToString().DeserializeJson<PingInfo>())
                    .ToList(),
            };
            await _redisHealthRepository.SaveAsync(redisHealth);
        }

        public async Task<List<RedisHealth>> GetAll()
        {
            var result = new List<RedisHealth>();

            foreach (string redis in _redises)
            {
                var keys = await GetInstanceKeysAsync(redis);
                if (keys.Length == 0)
                    continue;

                var redisesHealthJsons = await _db.StringGetAsync(keys);
                result.AddRange(
                    redisesHealthJsons
                        .Where(r => r.HasValue)
                        .Select(i => i.ToString().DeserializeJson<CacheRedisHealthModel>())
                        .GroupBy(i => i.Name)
                        .Select(g => new RedisHealth
                        {
                            Name = g.Key,
                            HealthChecks = g.Select(x => x.PingInfo).ToList(),
                        }));
            }

            if (result.Count == 0)
                return await GetHealthChecksFromTableAsync();

            return result;
        }

        private async Task<RedisKey[]> GetInstanceKeysAsync(string redisName)
        {
            var instanceKey = string.Format(_instanceSetKeyPattern, redisName);
            var historyStartScore = DateTime.UtcNow.Subtract(_historyDuration).Ticks;

            var tx = _db.CreateTransaction();
            tx.AddCondition(Condition.KeyExists(instanceKey));
            var tasks = new List<Task>
            {
                tx.SortedSetRemoveRangeByScoreAsync(instanceKey, 0, historyStartScore)
            };
            var getSetTask = tx.SortedSetRangeByScoreAsync(instanceKey, historyStartScore, double.MaxValue);
            tasks.Add(getSetTask);
            if (await tx.ExecuteAsync())
            {
                await Task.WhenAll(tasks);
                return getSetTask.Result
                    .Select(i => (RedisKey)$"{instanceKey}:{i}")
                    .ToArray();
            }
            return new RedisKey[0];
        }

        private async Task<List<RedisHealth>> GetHealthChecksFromTableAsync()
        {
            var historyStart = DateTime.UtcNow.Subtract(_historyDuration);
            return (await _redisHealthRepository.GetAllAsync())
                .Select(i => new RedisHealth
                {
                    Name = i.Name,
                    HealthChecks = i.HealthChecks.Where(c => c.Timestamp >= historyStart).ToList(),
                })
                .ToList();
        }
    }
}
