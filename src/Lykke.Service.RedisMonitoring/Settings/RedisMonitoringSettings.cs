using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Lykke.Service.RedisMonitoring.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class RedisMonitoringSettings
    {
        public DbSettings Db { get; set; }

        public Dictionary<string, string> RedisConnStrings { get; set; }
        public string OwnRedisCacheConnString { get; set; }
        public TimeSpan CheckFrequency { get; set; }
        public TimeSpan HistoryDuration { get; set; }
    }
}
