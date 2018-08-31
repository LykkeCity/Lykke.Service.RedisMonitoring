using Lykke.Service.RedisMonitoring.Client.Models;

namespace Lykke.Service.RedisMonitoring.Services
{
    public class CacheRedisHealthModel
    {
        public string Name { get; set; }
        public PingInfo PingInfo { get; set; }
    }
}
