using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.RedisMonitoring.Client 
{
    /// <summary>
    /// RedisMonitoring client settings.
    /// </summary>
    public class RedisMonitoringServiceClientSettings 
    {
        /// <summary>Service url.</summary>
        [HttpCheck("api/isalive")]
        public string ServiceUrl {get; set;}
    }
}
