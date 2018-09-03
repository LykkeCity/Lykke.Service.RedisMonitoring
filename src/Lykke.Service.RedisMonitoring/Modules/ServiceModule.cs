using Autofac;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Sdk;
using Lykke.Service.RedisMonitoring.AzureRepositories;
using Lykke.Service.RedisMonitoring.Core.Repositories;
using Lykke.Service.RedisMonitoring.Core.Services;
using Lykke.Service.RedisMonitoring.Services;
using Lykke.Service.RedisMonitoring.Settings;
using Lykke.SettingsReader;
using StackExchange.Redis;

namespace Lykke.Service.RedisMonitoring.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var settings = _appSettings.CurrentValue;

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterType<RedisHealthChecker>()
                .As<IRedisHealthChecker>()
                .SingleInstance();

            builder.RegisterType<CachedRedisHealthRepository>()
                .As<ICachedRedisHealthRepository>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(settings.RedisMonitoringService.HistoryDuration))
                .WithParameter(TypedParameter.From(settings.RedisMonitoringService.RedisConnStrings.Keys));

            builder.Register(context => ConnectionMultiplexer.Connect(settings.RedisMonitoringService.OwnRedisCacheConnString))
                .As<IConnectionMultiplexer>()
                .SingleInstance();

            builder.Register(ctx =>
                {
                    var logFactory = ctx.Resolve<ILogFactory>();
                    var storage = AzureTableStorage<RedisHealthEntity>.Create(
                        _appSettings.ConnectionString(s => s.RedisMonitoringService.Db.DataConnString),
                        "RedisHealthMonitoring",
                        logFactory);
                    return new RedisHealthRepository(storage);
                })
                .As<IRedisHealthRepository>()
                .SingleInstance();

            builder.RegisterType<MonitoringJob>()
                .As<IStartStop>()
                .SingleInstance()
                .WithParameter("checkFrequency", settings.RedisMonitoringService.CheckFrequency)
                .WithParameter("redisesInfo", settings.RedisMonitoringService.RedisConnStrings);
        }
    }
}
