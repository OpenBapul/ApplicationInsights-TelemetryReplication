using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using System;

namespace ApplicationInsights.TelemetryReplication.Logging
{
    public class AiLoggerProvider : ILoggerProvider
    {
        public const string UnknownCategoryName = "Unknown";
        private readonly Func<TelemetryClient> telemetryClientFactory;
        private readonly LogFilter logFilter;
        public AiLoggerProvider(Func<TelemetryClient> telemetryClientFactory)
            : this(telemetryClientFactory, null)
        {
        }
        public AiLoggerProvider(
            Func<TelemetryClient> telemetryClientFactory,
            LogFilter logFilter)
        {
            if (telemetryClientFactory == null)
            {
                throw new ArgumentNullException(nameof(telemetryClientFactory));
            }
            if (logFilter == null)
            {
                logFilter = (name, level) => false;
            }
            this.telemetryClientFactory = telemetryClientFactory;
            this.logFilter = logFilter;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var telemetryClient = telemetryClientFactory();
            var name = string.IsNullOrWhiteSpace(categoryName)
                ? UnknownCategoryName : categoryName;
            return new AiLogger(name, telemetryClient, logFilter);
        }

        public void Dispose()
        {
            // noop.
        }
    }
}
