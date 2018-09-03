using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.RedisMonitoring.Client.Models;

namespace Lykke.Service.RedisMonitoring.Core.Repositories
{
    public interface IRedisHealthRepository
    {
        Task SaveAsync(RedisHealth redisHealth);
        Task<RedisHealth> GetAsync(string redisName);
        Task<List<RedisHealth>> GetAllAsync();
    }
}
