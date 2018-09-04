using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Service.RedisMonitoring.Client.Models
{
    public class RedisHealth
    {
        public string Name { get; set; }

        public List<PingInfo> HealthChecks { get; set; }

        public DateTime? LastResponseTime { get; set; }

        public bool IsAlive => LastResponseTime.HasValue
                               && HealthChecks != null
                               && LastResponseTime.Value == HealthChecks.Max(i => i.Timestamp);
    }
}
