using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.RedisMonitoring.Client.Models;

namespace Lykke.Service.RedisMonitoring.Core.Services
{
    public interface ICachedRedisHealthRepository
    {
        Task SaveAsync(PingInfo pingInfo, string redisName);
        Task<List<RedisHealth>> GetAll();
        Task InitCacheAsync();
    }
}
