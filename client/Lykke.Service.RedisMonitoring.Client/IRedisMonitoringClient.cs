using JetBrains.Annotations;

namespace Lykke.Service.RedisMonitoring.Client
{
    /// <summary>
    /// RedisMonitoring client interface.
    /// </summary>
    [PublicAPI]
    public interface IRedisMonitoringClient
    {
        // Make your app's controller interfaces visible by adding corresponding properties here.
        // NO actual methods should be placed here (these go to controller interfaces, for example - IRedisMonitoringApi).
        // ONLY properties for accessing controller interfaces are allowed.

        /// <summary>Application Api interface</summary>
        IRedisMonitoringApi Api { get; }
    }
}
