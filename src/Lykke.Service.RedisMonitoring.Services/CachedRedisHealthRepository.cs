using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.RedisMonitoring.Client.Models;
using Lykke.Service.RedisMonitoring.Core.Repositories;
using Lykke.Service.RedisMonitoring.Core.Services;
using StackExchange.Redis;

namespace Lykke.Service.RedisMonitoring.Services
{
    public class CachedRedisHealthRepository : ICachedRedisHealthRepository
    {
        private const string _dateFormat = "yyyy-MM-dd-HH-mm-ss-fffffff";
        private const string _instanceSetKeyPattern = "RedisMonitoring:{0}";
        private const string _objKeySuffix = "time:{0}";
        private const string _instanceLastAliveTimeKeyPattern = "RedisMonitoring:LastAliveTime:{0}";

        private readonly IRedisHealthRepository _redisHealthRepository;
        private readonly IDatabase _db;
        private readonly ILog _log;
        private readonly TimeSpan _historyDuration;
        private readonly IEnumerable<string> _redises;

        public CachedRedisHealthRepository(
            IRedisHealthRepository redisHealthRepository,
            IConnectionMultiplexer connectionMultiplexer,
            ILogFactory logFactory,
            TimeSpan historyDuration,
            IEnumerable<string> redises)
        {
            _redisHealthRepository = redisHealthRepository;
            _db = connectionMultiplexer.GetDatabase();
            _log = logFactory.CreateLog(this);
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
                    var objSuffix = string.Format(_objKeySuffix, pingInfo.Timestamp.ToString(_dateFormat));
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

                if (redisHealth.LastResponseTime.HasValue)
                {
                    var lastAliveKey = string.Format(_instanceLastAliveTimeKeyPattern, redisHealth.Name);
                    await _db.StringSetAsync(lastAliveKey, redisHealth.LastResponseTime.Value.ToString(_dateFormat));
                }
            }
        }

        public async Task SaveAsync(PingInfo pingInfo, string redisName)
        {
            var instanceKey = string.Format(_instanceSetKeyPattern, redisName);
            var objSuffix = string.Format(_objKeySuffix, pingInfo.Timestamp.ToString(_dateFormat));
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
            if (pingInfo.Duration.HasValue)
            {
                var lastAliveKey = string.Format(_instanceLastAliveTimeKeyPattern, redisName);
                tasks.Add(tx.StringSetAsync(lastAliveKey, pingInfo.Timestamp.ToString(_dateFormat)));
            }
            if (await tx.ExecuteAsync())
            {
                await Task.WhenAll(tasks);
                if (!setTask.Result)
                    _log.Error($"Error during adding ping info for redis {redisName}");
            }
            else
            {
                _log.Error($"Error during adding ping info for redis {redisName}");
            }

            var keys = await GetInstanceKeysAsync(redisName);
            RedisHealth redisHealth;
            if (keys.Length == 0)
            {
                redisHealth = await _redisHealthRepository.GetAsync(redisName);
                var historyStart = DateTime.UtcNow.Subtract(_historyDuration);
                redisHealth.HealthChecks = redisHealth.HealthChecks.Where(c => c.Timestamp >= historyStart).ToList();
            }
            else
            {
                var infoJsons = await _db.StringGetAsync(keys);
                redisHealth = new RedisHealth
                {
                    Name = redisName,
                    HealthChecks = infoJsons
                        .Where(a => a.HasValue)
                        .Select(a => a.ToString().DeserializeJson<PingInfo>())
                        .ToList(),
                };
                if (pingInfo.Duration.HasValue)
                {
                    redisHealth.LastResponseTime = pingInfo.Timestamp;
                }
                else
                {
                    var lastAliveKey = string.Format(_instanceLastAliveTimeKeyPattern, redisName);
                    var lastAliveStr = await _db.StringGetAsync(lastAliveKey);
                    if (lastAliveStr.HasValue)
                        redisHealth.LastResponseTime = DateTime.ParseExact(lastAliveStr, _dateFormat, CultureInfo.InvariantCulture);
                }
            }
            if (redisHealth.HealthChecks.All(i => i.Timestamp != pingInfo.Timestamp))
                redisHealth.HealthChecks.Add(pingInfo);
            await _redisHealthRepository.SaveAsync(redisHealth);
        }

        public async Task<List<RedisHealth>> GetAllAsync()
        {
            var result = new List<RedisHealth>();

            foreach (string redisName in _redises)
            {
                var keys = await GetInstanceKeysAsync(redisName);
                if (keys.Length == 0)
                    continue;

                var redisHealthJsons = await _db.StringGetAsync(keys);
                var healthChecks = redisHealthJsons
                    .Where(r => r.HasValue)
                    .Select(i => i.ToString().DeserializeJson<CacheRedisHealthModel>().PingInfo)
                    .ToList();
                var redisHealth = new RedisHealth
                {
                    Name = redisName,
                    HealthChecks = healthChecks,
                };
                var lastAliveKey = string.Format(_instanceLastAliveTimeKeyPattern, redisName);
                var lastAliveStr = await _db.StringGetAsync(lastAliveKey);
                if (lastAliveStr.HasValue)
                    redisHealth.LastResponseTime = DateTime.ParseExact(lastAliveStr, _dateFormat, CultureInfo.InvariantCulture);
                result.Add(redisHealth);
            }

            if (result.Count == 0)
                return await GetHealthChecksFromTableAsync();

            return result;
        }

        public async Task<RedisHealth> GetAsync(string redisName)
        {
            var keys = await GetInstanceKeysAsync(redisName);
            if (keys.Length == 0)
                return await _redisHealthRepository.GetAsync(redisName);

            var infoJsons = await _db.StringGetAsync(keys);
            var healthChecks = infoJsons
                .Where(a => a.HasValue)
                .Select(a => a.ToString().DeserializeJson<PingInfo>())
                .ToList();
            var result = new RedisHealth
            {
                Name = redisName,
                HealthChecks = healthChecks,
            };
            var lastAliveKey = string.Format(_instanceLastAliveTimeKeyPattern, redisName);
            var lastAliveStr = await _db.StringGetAsync(lastAliveKey);
            if (lastAliveStr.HasValue)
                result.LastResponseTime = DateTime.ParseExact(lastAliveStr, _dateFormat, CultureInfo.InvariantCulture);
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
                    LastResponseTime = i.LastResponseTime,
                })
                .ToList();
        }
    }
}
