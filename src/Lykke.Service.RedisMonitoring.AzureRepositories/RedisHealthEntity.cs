using Common;
using Lykke.Service.RedisMonitoring.Client.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.RedisMonitoring.AzureRepositories
{
    public class RedisHealthEntity : TableEntity
    {
        public string Name { get; set; }
        public string HealthData { get; set; }

        internal static string GeneratePartitionKey()
        {
            return "RedisHealthMonitoring";
        }

        internal static string GenerateRowKey(string redisName)
        {
            return redisName;
        }

        internal static RedisHealthEntity FromModel(RedisHealth redisHealth)
        {
            return new RedisHealthEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(redisHealth.Name),
                Name = redisHealth.Name,
                HealthData = redisHealth.HealthChecks.ToJson(),
            };
        }
    }
}
