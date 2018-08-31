using System;

namespace Lykke.Service.RedisMonitoring.Client.Models
{
    public class PingInfo
    {
        public DateTime Timestamp { get; set; }

        public TimeSpan? Duration { get; set; }
    }
}
