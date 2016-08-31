using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace ApplicationInsights.TelemetryReplication
{
    public static class TelemetryConfigurationExtensions
    {
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
