using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace ApplicationInsights.TelemetryReplication
{
    public static class TelemetryConfigurationExtensions
    {
        /// <summary>
        /// Uses given proxy uri as telemetry channel endpoint address.
        /// </summary>
        /// <param name="configuration">The telemetry configuration.</param>
        /// <param name="proxyUri">The uri to send telemetries.</param>
        /// <returns>The telemetry configuration.</returns>
        public static TelemetryConfiguration UseTelemetryProxy(
            this TelemetryConfiguration configuration,
            string proxyUri)
        {
            configuration.TelemetryChannel.EndpointAddress = proxyUri;
            return configuration;
        }

        /// <summary>
        /// Uses AppId information into the telemetry.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="appId">The AppId identify the application.</param>
        /// <returns>The builder.</returns>
        public static TelemetryProcessorChainBuilder UseAppId(
            this TelemetryProcessorChainBuilder builder,
            AppId appId)
        {
            builder
                .Use((next) => new TelemetryReplicationProcessor(next, appId))
                .Build();
            return builder;
        }
    }
}
