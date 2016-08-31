using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System;

namespace ApplicationInsights.TelemetryReplication.Logging
{
    public delegate bool LogFilter(string categoryName, LogLevel logLevel);

    public class AiLogger : ILogger
    {
        private readonly string categoryName;
        private readonly TelemetryClient telemetryClient;
        private readonly LogFilter logFilter;
        public AiLogger(
            string categoryName,
            TelemetryClient telemetryClient,
            LogFilter logFilter)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                throw new ArgumentNullException(nameof(categoryName));
            }
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }
            if (logFilter == null)
            {
                throw new ArgumentNullException(nameof(logFilter));
            }
            this.categoryName = categoryName;
            this.telemetryClient = telemetryClient;
            this.logFilter = logFilter;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return Noop.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // It'll filter any ApplicationInsights.TelemetryReplication related log by force.
            return 
                !categoryName.StartsWith("ApplicationInsights.TelemetryReplication", StringComparison.OrdinalIgnoreCase)
                && !logFilter(categoryName, logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                formatter = (TState s, Exception e) =>
                    e == null
                    ? $"{s}"
                    : $"{s}\n{e}";
            }

            var message = formatter(state, exception) ?? "";
            if (exception == null)
            {
                TrackTrace(logLevel, eventId, message, state);
            }
            else
            {
                TrackException(eventId, message, state, exception);
            }
        }

        private void TrackTrace<TState>(LogLevel logLevel, EventId eventId, string message, TState state)
        {
            var telemetry = new TraceTelemetry(message, ToSeverityLevel(logLevel));
            telemetry.Properties["eventName"] = eventId.Name;
            telemetry.Properties["eventId"] = eventId.Id.ToString();
            telemetryClient.TrackTrace(telemetry);
        }

        private void TrackException<TState>(EventId eventId, string message, TState state, Exception exception)
        {
            var telemetry = new ExceptionTelemetry(exception);
            telemetry.Properties["eventName"] = eventId.Name;
            telemetry.Properties["eventId"] = eventId.Id.ToString();
            telemetry.Properties["message"] = message;
            telemetryClient.TrackException(telemetry);
        }

        private SeverityLevel ToSeverityLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug: return SeverityLevel.Verbose;
                case LogLevel.Trace: return SeverityLevel.Verbose;
                case LogLevel.Information: return SeverityLevel.Information;
                case LogLevel.Warning: return SeverityLevel.Warning;
                case LogLevel.Error: return SeverityLevel.Error;
                case LogLevel.Critical: return SeverityLevel.Critical;
            }
            return SeverityLevel.Verbose;
        }

        private class Noop : IDisposable
        {
            public static Noop Instance = new Noop();

            public void Dispose()
            {
            }
        }
    }
}
