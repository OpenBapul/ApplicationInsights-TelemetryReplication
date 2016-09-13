using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace ApplicationInsights.TelemetryReplication
{
    /// <summary>
    /// Options for TelemetryProxy.
    /// </summary>
    public class TelemetryProxyOptions
    {
        /// <summary>
        /// The original endpoint address of TelemetryChannel.
        /// </summary>
        public string EndpointAddress { get; set; }
        /// <summary>
        /// The factory that creates HttpClient to send telemetries to DestinationUri.
        /// </summary>
        public Func<HttpClient> HttpClientFactory { get; set; }
        /// <summary>
        /// The factory that creates telemetry replicators.
        /// </summary>
        public ITelemetryReplicatorsFactory TelemetryReplicatorFactory { get; set; }
        /// <summary>
        /// The logger factory.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; set; }

        /// <summary>
        /// Gets the default telemetry proxy options.
        /// </summary>
        public static TelemetryProxyOptions Default => new TelemetryProxyOptions
        {
            EndpointAddress = "https://dc.services.visualstudio.com/v2/track",
        };
    }
}
