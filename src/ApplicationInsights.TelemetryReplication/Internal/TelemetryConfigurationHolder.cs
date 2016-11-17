namespace ApplicationInsights.TelemetryReplication.Internal
{
    internal static class TelemetryConfigurationHolder
    {
        /// <summary>
        /// The original endpoint address of the telemetry channel.
        /// </summary>
        internal static string OriginalEndpointAddress { get; set; }
    }
}
