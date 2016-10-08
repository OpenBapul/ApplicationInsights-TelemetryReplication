using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

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
                if (state is IEnumerable<KeyValuePair<string, object>>)
                {
                    var properties = state as IEnumerable<KeyValuePair<string, object>>;
                    var logType = properties
                        .Where(item => item.Key.Equals("LogType", StringComparison.OrdinalIgnoreCase))
                        .Select(item => item.Value?.ToString() ?? "")
                        .FirstOrDefault() ?? "";
                    if (logType.Equals(nameof(MetricLog), StringComparison.OrdinalIgnoreCase))
                    {
                        var metric = MetricLog.Create(properties);
                        if (metric == null)
                        {
                            TrackTrace(logLevel, eventId, message, properties);
                        }
                        else
                        {
                            TrackMetric(logLevel, eventId, message, metric);
                        }
                    }
                    else if (logType.Equals(nameof(EventLog), StringComparison.OrdinalIgnoreCase))
                    {
                        var @event = EventLog.Create(properties);
                        if (@event == null)
                        {
                            TrackTrace(logLevel, eventId, message, properties);
                        }
                        else
                        {
                            TrackEvent(logLevel, eventId, message, @event);
                        }
                    }
                    else
                    {
                        TrackTrace(logLevel, eventId, message, properties);
                    }
                }
                else if (state is EventLog)
                {
                    TrackEvent(logLevel, eventId, message, state as EventLog);
                }
                else if (state is MetricLog)
                {
                    TrackMetric(logLevel, eventId, message, state as MetricLog);
                }
                else
                {
                    TrackTrace(logLevel, eventId, message, Enumerable.Empty<KeyValuePair<string, object>>());
                }
            }
            else
            {
                TrackException(eventId, message, exception, Enumerable.Empty<KeyValuePair<string, object>>());
            }
        }

        private void TrackTrace(LogLevel logLevel, EventId eventId, string message, IEnumerable<KeyValuePair<string, object>> properties)
        {
            var telemetry = new TraceTelemetry(message, ToSeverityLevel(logLevel));
            if (properties != null)
            {
                foreach (var item in properties)
                {
                    telemetry.Properties[item.Key] = item.Value.ToString();
                }
            }
            telemetry.Properties["logger"] = categoryName;
            telemetry.Properties["eventName"] = eventId.Name;
            telemetry.Properties["eventId"] = eventId.Id.ToString();
            telemetryClient.TrackTrace(telemetry);
        }

        private void TrackMetric(LogLevel logLevel, EventId eventId, string message, MetricLog metric)
        {
            var telemetry = new MetricTelemetry(metric.Name, metric.Value);
            telemetry.Count = metric.Count;
            telemetry.Min = metric.Min;
            telemetry.Max = metric.Max;
            telemetry.StandardDeviation = metric.StandardDeviation;
            if (metric.Properties != null)
            {
                foreach (var item in metric.Properties)
                {
                    telemetry.Properties[item.Key] = item.Value.ToString();
                }
            }
            telemetry.Properties["logger"] = categoryName;
            telemetry.Properties["eventName"] = eventId.Name;
            telemetry.Properties["eventId"] = eventId.Id.ToString();
            telemetry.Properties["remark"] = metric.Remark;
            telemetryClient.TrackMetric(telemetry);
        }

        private void TrackEvent(LogLevel logLevel, EventId eventId, string message, EventLog @event)
        {
            var telemetry = new EventTelemetry(@event.Name);
            if (@event.Properties != null)
            {
                foreach (var item in @event.Properties)
                {
                    telemetry.Properties[item.Key] = item.Value.ToString();
                }
            }
            if (@event.Metrics != null)
            {
                foreach (var item in @event.Metrics)
                {
                    telemetry.Metrics[item.Key] = item.Value;
                }
            }
            telemetry.Properties["logger"] = categoryName;
            telemetry.Properties["eventName"] = eventId.Name;
            telemetry.Properties["eventId"] = eventId.Id.ToString();
            telemetryClient.TrackEvent(telemetry);
        }

        private void TrackException(EventId eventId, string message, Exception exception, IEnumerable<KeyValuePair<string, object>> properties)
        {
            var telemetry = new ExceptionTelemetry(exception);
            if (properties != null)
            {
                foreach (var item in properties)
                {
                    telemetry.Properties[item.Key] = item.Value.ToString();
                }
            }
            telemetry.Properties["logger"] = categoryName;
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
