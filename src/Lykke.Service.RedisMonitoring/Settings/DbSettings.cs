using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.RedisMonitoring.Settings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }

        [AzureTableCheck]
        public string DataConnString { get; set; }
    }
}
