using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using ApplicationInsights.TelemetryReplication.Internal;

namespace ApplicationInsights.TelemetryReplication
{
    public static class TelemetryReplicationExtensions
    {
        /// <summary>
        /// Add telemetry replication features to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddApplicationInsightsTelemetryReplication(
            this IServiceCollection services)
        {
            return AddApplicationInsightsTelemetryReplication(services, null);
        }
        public static IServiceCollection AddApplicationInsightsTelemetryReplication(
            this IServiceCollection services,
            Action<TelemetryProxyOptions> configure)
        {
            services.AddSingleton<ServiceProviderTelemetryReplicatorsFactory>();
            services.AddSingleton(provider =>
            {
                var options = new TelemetryProxyOptions();
                configure?.Invoke(options);
                if (options.TelemetryReplicatorFactory == null)
                {
                    options.TelemetryReplicatorFactory 
                        = provider.GetService<ServiceProviderTelemetryReplicatorsFactory>();
                }
                if (string.IsNullOrEmpty(options.EndpointAddress))
                {
                    var telemetryConfiguration = provider.GetService<TelemetryConfiguration>();
                    options.EndpointAddress = TelemetryConfigurationHolder.OriginalEndpointAddress
                        ?? telemetryConfiguration.TelemetryChannel.EndpointAddress;
                }
                return options;
            });
            services.AddSingleton(provider =>
            {
                var options = provider.GetService<TelemetryProxyOptions>();
                return new TelemetryProxy(options);
            });
            return services;
        }
    }
}
