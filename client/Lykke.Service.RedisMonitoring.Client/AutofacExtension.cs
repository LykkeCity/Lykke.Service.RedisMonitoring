using Autofac;
using JetBrains.Annotations;
using Lykke.HttpClientGenerator;
using Lykke.HttpClientGenerator.Infrastructure;
using System;

namespace Lykke.Service.RedisMonitoring.Client
{
    [PublicAPI]
    public static class AutofacExtension
    {
        /// <summary>
        /// Registers <see cref="IRedisMonitoringClient"/> in Autofac container using <see cref="RedisMonitoringServiceClientSettings"/>.
        /// </summary>
        /// <param name="builder">Autofac container builder.</param>
        /// <param name="settings">RedisMonitoring client settings.</param>
        /// <param name="builderConfigure">Optional <see cref="HttpClientGeneratorBuilder"/> configure handler.</param>
        public static void RegisterRedisMonitoringClient(
            [NotNull] this ContainerBuilder builder,
            [NotNull] RedisMonitoringServiceClientSettings settings,
            [CanBeNull] Func<HttpClientGeneratorBuilder, HttpClientGeneratorBuilder> builderConfigure)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.ServiceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(RedisMonitoringServiceClientSettings.ServiceUrl));

            var clientBuilder = HttpClientGenerator.HttpClientGenerator.BuildForUrl(settings.ServiceUrl)
                .WithAdditionalCallsWrapper(new ExceptionHandlerCallsWrapper());

            clientBuilder = builderConfigure?.Invoke(clientBuilder) ?? clientBuilder.WithoutRetries();

            builder.RegisterInstance(new RedisMonitoringClient(clientBuilder.Create()))
                .As<IRedisMonitoringClient>()
                .SingleInstance();
        }
    }
}
