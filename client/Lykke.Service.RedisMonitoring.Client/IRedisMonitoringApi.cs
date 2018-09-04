using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.RedisMonitoring.Client.Models;
using Refit;

namespace Lykke.Service.RedisMonitoring.Client
{
    /// <summary>
    /// RedisMonitoring client API interface.
    /// </summary>
    [PublicAPI]
    public interface IRedisMonitoringApi
    {
        /// <summary>
        /// Fetches health checks for monitored redis instances during configured period.
        /// </summary>
        /// <returns>List of redis instance health checks.</returns>
        [Get("/api/redismonitoring/Health")]
        Task<List<RedisHealth>> GetHealth();

        /// <summary>
        /// Fetches health checks for monitored redis instances during configured period.
        /// </summary>
        /// <param name="redisName">Redis instance name.</param>
        /// <returns>Redis instance health status.</returns>
        [Get("/api/redismonitoring/Health/{redisName}")]
        Task<RedisHealth> GetHealth(string redisName);
    }
}
