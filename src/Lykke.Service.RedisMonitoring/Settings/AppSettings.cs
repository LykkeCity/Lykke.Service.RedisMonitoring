using JetBrains.Annotations;
using Lykke.Sdk.Settings;

namespace Lykke.Service.RedisMonitoring.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public RedisMonitoringSettings RedisMonitoringService { get; set; }
    }
}
