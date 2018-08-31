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
        private const string _instanceName = "RedisMonitoring";
        private const string _allObjectsRecordsKeyPattern = "{0}:*";
        private const string _objectHealthCheckKeyPattern = "{0}:{1}:time:{2}";
        private const string _objectAllRecordsKeyPattern = "{0}:{1}:time:*";

        private readonly IRedisHealthRepository _redisHealthRepository;
        private readonly IDatabase _db;
        private readonly TimeSpan _historyDuration;

        public CachedRedisHealthRepository(
            IRedisHealthRepository redisHealthRepository,
            IConnectionMultiplexer connectionMultiplexer,
            TimeSpan historyDuration)
        {
            _redisHealthRepository = redisHealthRepository;
            _db = connectionMultiplexer.GetDatabase();
            _historyDuration = historyDuration;
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

                    var key = string.Format(_objectHealthCheckKeyPattern, _instanceName, redisHealth.Name, pingInfo.Timestamp.ToString(_dataFormat));
                    var cached = await _db.StringGetAsync(key);
                    if (cached.HasValue)
                        continue;

                    var ttl = pingInfo.Timestamp.Add(_historyDuration).Subtract(DateTime.UtcNow);
                    if (ttl.Ticks < 0)
                        continue;

                    var item = new CacheRedisHealthModel
                    {
                        Name = redisHealth.Name,
                        PingInfo = pingInfo,
                    };

                    await _db.StringSetAsync(key, item.ToJson(), ttl);
                }
            }
        }

        public async Task SaveAsync(PingInfo pingInfo, string redisName)
        {
            var key = string.Format(_objectHealthCheckKeyPattern, _instanceName, redisName, pingInfo.Timestamp.ToString(_dataFormat));
            var item = new CacheRedisHealthModel
            {
                Name = redisName,
                PingInfo = pingInfo,
            };
            bool result = await _db.StringSetAsync(key, item.ToJson(), _historyDuration);
            if (!result)
                throw new InvalidOperationException($"Error during adding ping info for redis {redisName}");

            var keysPattern = string.Format(_objectAllRecordsKeyPattern, _instanceName, redisName);
            var data = await _db.ScriptEvaluateAsync($"return redis.call('keys', '{keysPattern}')");
            if (!data.IsNull)
            {
                var keys = (string[])data;
                if (keys.Length > 0)
                {
                    var infoJsons = await _db.StringGetAsync(keys.Select(k => (RedisKey)k).ToArray());
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
            }
        }

        public async Task<List<RedisHealth>> GetAll()
        {
            var keysPattern = string.Format(_allObjectsRecordsKeyPattern, _instanceName);
            var data = await _db.ScriptEvaluateAsync($"return redis.call('keys', '{keysPattern}')");
            if (data.IsNull)
                return await GetHealthChecksFromTableAsync();

            var keys = (string[])data;
            if (keys.Length <= 0)
                return await GetHealthChecksFromTableAsync();

            var redisesHealthJsons = await _db.StringGetAsync(keys.Select(k => (RedisKey)k).ToArray());
            return redisesHealthJsons
                .Where(r => r.HasValue)
                .Select(i => i.ToString().DeserializeJson<CacheRedisHealthModel>())
                .GroupBy(i => i.Name)
                .Select(g => new RedisHealth
                {
                    Name = g.Key,
                    HealthChecks = g.Select(x => x.PingInfo).ToList(),
                })
                .ToList();
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
