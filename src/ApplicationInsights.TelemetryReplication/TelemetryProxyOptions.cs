using ApplicationInsights.TelemetryReplication.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace ApplicationInsights.TelemetryReplication
{
    /// <summary>
    /// Options for TelemetryProxy.
    /// </summary>
    public class TelemetryProxyOptions
    {
        /// <summary>
        /// Original endpoint address of TelemetryChannel.
        /// </summary>
        public Uri DestinationUri { get; set; } = new Uri("https://dc.services.visualstudio.com/v2/track", UriKind.Absolute);
        /// <summary>
        /// A factory that creates HttpClient to send telemetries to DestinationUri.
        /// </summary>
        public Func<HttpClient> HttpClientFactory { get; set; }
        /// <summary>
        /// Telemetry replicators.
        /// </summary>
        public IEnumerable<ITelemetryReplicator> Replicators { get; set; } = Enumerable.Empty<ITelemetryReplicator>();
        /// <summary>
        /// Logger factory.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; set; }
    }
}
