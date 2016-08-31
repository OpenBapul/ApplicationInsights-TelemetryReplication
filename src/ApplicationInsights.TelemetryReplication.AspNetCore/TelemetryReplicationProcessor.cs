using Microsoft.ApplicationInsights.Extensibility;
using System;
using Microsoft.ApplicationInsights.Channel;

namespace ApplicationInsights.TelemetryReplication.AspNetCore
{
    public class TelemetryReplicationProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor next;
        private readonly string appName;
        public TelemetryReplicationProcessor(ITelemetryProcessor next, string appName)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (string.IsNullOrWhiteSpace(appName))
            {
                throw new ArgumentNullException(nameof(appName));
            }
            this.next = next;
            this.appName = appName;
        }

        public void Process(ITelemetry item)
        {
            item.Context.Properties["appName"] = appName;
            next.Process(item);
        }
    }
}
