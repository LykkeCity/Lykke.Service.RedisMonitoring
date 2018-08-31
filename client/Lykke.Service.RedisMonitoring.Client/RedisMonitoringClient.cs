using Lykke.HttpClientGenerator;

namespace Lykke.Service.RedisMonitoring.Client
{
    /// <summary>
    /// RedisMonitoring API aggregating interface.
    /// </summary>
    public class RedisMonitoringClient : IRedisMonitoringClient
    {
        // Note: Add similar Api properties for each new service controller

        /// <summary>Inerface to RedisMonitoring Api.</summary>
        public IRedisMonitoringApi Api { get; private set; }

        /// <summary>C-tor</summary>
        public RedisMonitoringClient(IHttpClientGenerator httpClientGenerator)
        {
            Api = httpClientGenerator.Generate<IRedisMonitoringApi>();
        }
    }
}
