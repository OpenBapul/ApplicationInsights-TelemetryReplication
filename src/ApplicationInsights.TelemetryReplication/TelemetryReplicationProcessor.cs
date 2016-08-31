using Microsoft.ApplicationInsights.Extensibility;
using System;
using Microsoft.ApplicationInsights.Channel;

namespace ApplicationInsights.TelemetryReplication
{
    public class TelemetryReplicationProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor next;
        private readonly AppId appId;
        public TelemetryReplicationProcessor(ITelemetryProcessor next, AppId appId)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (appId == null)
            {
                throw new ArgumentNullException(nameof(appId));
            }
            this.next = next;
            this.appId = appId;
        }

        public void Process(ITelemetry item)
        {
            item.Context.Properties["appName"] = appId.Name;
            item.Context.Properties["appVersion"] = appId.Version.ToString();
            item.Context.Properties["appEnvironment"] = appId.Environment;
            next.Process(item);
        }
    }
}
