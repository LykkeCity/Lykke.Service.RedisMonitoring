using JetBrains.Annotations;
using Lykke.Logs.Loggers.LykkeSlack;
using Lykke.Sdk;
using Lykke.Service.RedisMonitoring.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Lykke.Service.RedisMonitoring
{
    [UsedImplicitly]
    public class Startup
    {
        private readonly LykkeSwaggerOptions _swaggerOptions = new LykkeSwaggerOptions
        {
            ApiTitle = "RedisMonitoring API",
            ApiVersion = "v1"
        };

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider<AppSettings>(options =>
            {
                options.SwaggerOptions = _swaggerOptions;

                options.Logs = logs =>
                {
                    logs.AzureTableName = "RedisMonitoringLog";
                    logs.AzureTableConnectionStringResolver = settings => settings.RedisMonitoringService.Db.LogsConnString;

                    // TODO: You could add extended logging configuration here:
                    /* 
                    logs.Extended = extendedLogs =>
                    {
                        // For example, you could add additional slack channel like this:
                        extendedLogs.AddAdditionalSlackChannel("RedisMonitoring", channelOptions =>
                        {
                            channelOptions.MinLogLevel = LogLevel.Information;
                        });
                    };
                    */
                };
            });
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseLykkeConfiguration(options =>
            {
                options.SwaggerOptions = _swaggerOptions;
            });
        }
    }
}
