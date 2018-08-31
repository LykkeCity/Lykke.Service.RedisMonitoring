using System.Collections.Generic;

namespace Lykke.Service.RedisMonitoring.Client.Models
{
    public class RedisHealth
    {
        public string Name { get; set; }

        public List<PingInfo> HealthChecks { get; set; }
    }
}
