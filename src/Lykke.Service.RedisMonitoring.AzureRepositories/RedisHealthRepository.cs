using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Lykke.Service.RedisMonitoring.Client.Models;
using Lykke.Service.RedisMonitoring.Core.Repositories;

namespace Lykke.Service.RedisMonitoring.AzureRepositories
{
    public class RedisHealthRepository : IRedisHealthRepository
    {
        private readonly INoSQLTableStorage<RedisHealthEntity> _storage;

        public RedisHealthRepository(INoSQLTableStorage<RedisHealthEntity> storage)
        {
            _storage = storage;
        }

        public async Task SaveAsync(RedisHealth redisHealth)
        {
            await _storage.InsertOrReplaceAsync(RedisHealthEntity.FromModel(redisHealth));
        }

        public async Task<RedisHealth> GetAsync(string redisName)
        {
            var redisHealthEntity = await _storage.GetDataAsync(RedisHealthEntity.GeneratePartitionKey(),RedisHealthEntity.GenerateRowKey(redisName));
            return new RedisHealth
            {
                Name = redisName,
                HealthChecks = redisHealthEntity.HealthData.DeserializeJson<List<PingInfo>>(),
            };
        }

        public async Task<List<RedisHealth>> GetAllAsync()
        {
            var items = await _storage.GetDataAsync(RedisHealthEntity.GeneratePartitionKey());
            var result = new List<RedisHealth>();

            foreach (var item in items)
            {
                var pingInfo = item.HealthData.DeserializeJson<List<PingInfo>>();
                result.Add(new RedisHealth { Name = item.RowKey, HealthChecks = pingInfo });
            }

            return result;
        }
    }
}
