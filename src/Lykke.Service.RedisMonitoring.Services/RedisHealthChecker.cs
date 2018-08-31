using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.RedisMonitoring.Core.Services;
using StackExchange.Redis;

namespace Lykke.Service.RedisMonitoring.Services
{
    public class RedisHealthChecker : IRedisHealthChecker
    {
        private readonly ILog _log;

        public RedisHealthChecker(ILogFactory logFactory)
        {
            _log = logFactory.CreateLog(this);
        }

        public async Task<bool> CheckAsync(string name, string connectionString)
        {
            try
            {
                using (var redis = await ConnectionMultiplexer.ConnectAsync(connectionString))
                {
                    return redis.IsConnected;
                }
            }
            catch (Exception e)
            {
                _log.Info(e.Message, context: name);
                return false;
            }
        }
    }
}
