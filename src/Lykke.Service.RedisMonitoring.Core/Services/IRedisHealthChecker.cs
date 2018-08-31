using System.Threading.Tasks;

namespace Lykke.Service.RedisMonitoring.Core.Services
{
    public interface IRedisHealthChecker
    {
        Task<bool> CheckAsync(string name, string connectionString);
    }
}
