using Microsoft.Extensions.DependencyInjection;
using System;

namespace ApplicationInsights.TelemetryReplication.ElasticSearch
{
    public static class TelemetryReplicationExtensions
    {
        public static IServiceCollection AddElasticSearchTelemetryReplicator(
            this IServiceCollection services)
        {
            return AddElasticSearchTelemetryReplicator(services, null);
        }
        public static IServiceCollection AddElasticSearchTelemetryReplicator(
            this IServiceCollection services,
            Action<ElasticSearchTelemetryReplicatorOptions> configure)
        {
            services.AddSingleton(provider =>
            {
                var options = new ElasticSearchTelemetryReplicatorOptions();
                configure?.Invoke(options);
                return options;
            });
            services.AddSingleton<ElasticSearchTelemetryReplicator>();
            services.AddSingleton<ITelemetryReplicator, ElasticSearchTelemetryReplicator>();
            return services;
        }
    }
}
