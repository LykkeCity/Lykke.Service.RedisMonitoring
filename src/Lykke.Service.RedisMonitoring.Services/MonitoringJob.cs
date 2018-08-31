using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.RedisMonitoring.Client.Models;
using Lykke.Service.RedisMonitoring.Core.Services;

namespace Lykke.Service.RedisMonitoring.Services
{
    public class MonitoringJob : TimerPeriod, IStartStop
    {
        private readonly ILog _log;
        private readonly IHealthNotifier _healthNotifier;
        private readonly IRedisHealthChecker _redisHealthChecker;
        private readonly ICachedRedisHealthRepository _redisHealthRepository;
        private readonly Dictionary<string, string> _redisesInfo;

        public MonitoringJob(
            ILogFactory logFactory,
            IHealthNotifier healthNotifier,
            IRedisHealthChecker redisHealthChecker,
            ICachedRedisHealthRepository redisHealthRepository,
            TimeSpan checkFrequency,
            Dictionary<string, string> redisesInfo)
            : base(checkFrequency, logFactory)
        {
            _log = logFactory.CreateLog(this);
            _healthNotifier = healthNotifier;
            _redisHealthChecker = redisHealthChecker;
            _redisHealthRepository = redisHealthRepository;
            _redisesInfo = redisesInfo;
        }

        public override void Start()
        {
            _redisHealthRepository.InitCacheAsync().GetAwaiter().GetResult();

            base.Start();
        }

        public override async Task Execute()
        {
            foreach (var redisInfo in _redisesInfo)
            {
                try
                {
                    var watch = new Stopwatch();
                    watch.Start();
                    bool isHealthy = await _redisHealthChecker.CheckAsync(redisInfo.Key, redisInfo.Value);
                    watch.Stop();
                    if (!isHealthy)
                        _healthNotifier.Notify($"Redis instace '{redisInfo.Key}' is not responding");
                    await _redisHealthRepository.SaveAsync(
                        new PingInfo {Duration = isHealthy ? watch.Elapsed : (TimeSpan?) null, Timestamp = DateTime.UtcNow},
                        redisInfo.Key);
                }
                catch (Exception e)
                {
                    _log.Error(e);
                    throw;
                }
            }
        }
    }
}
