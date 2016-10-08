using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationInsights.TelemetryReplication.Logging
{
    /// <summary>
    /// A log represents event.
    /// </summary>
    public class EventLog
    {
        public EventLog(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            Name = name;
        }

        /// <summary>
        /// The name of this event.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The metrics related with the event.
        /// </summary>
        public IDictionary<string, double> Metrics { get; } = new Dictionary<string, double>();
        /// <summary>
        /// Other properties.
        /// </summary>
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Convert to dictionary.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<string, object> ToDictionary()
        {
            var dic = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in Properties)
            {
                dic[item.Key] = item.Value;
            }
            foreach (var item in Metrics)
            {
                dic[$"Metric.{item.Key}"] = item.Value;
            }
            dic["LogType"] = nameof(EventLog);
            dic[nameof(Name)] = Name;
            return dic;
        }

        /// <summary>
        /// Create a new EventLog from given properties.
        /// </summary>
        /// <param name="properties">Key-value properties.</param>
        /// <remarks>
        /// Name is mandatory property.
        /// Any properties that starts with 'Metric.' will be translated to Metrics.
        /// </remarks>
        /// <returns>Returns a new EventLog if given property have the Name property, or null otherwise.</returns>
        public static EventLog Create(IEnumerable<KeyValuePair<string, object>> properties)
        {
            if (properties == null)
            {
                return null;
            }
            if (false == properties.Any(item =>
                item.Key.Equals("LogType", StringComparison.OrdinalIgnoreCase)
                && (item.Value?.ToString() ?? "").Equals(nameof(EventLog), StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }
            var @event = new EventLog("No name");
            var dic = properties.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
            foreach (var item in dic)
            {
                if (item.Key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    @event.Name = item.Value?.ToString() ?? "";
                }
                else if (item.Key.StartsWith("Metric.", StringComparison.OrdinalIgnoreCase)
                    && item.Key.Length > 7)
                {
                    double value = 0.0;
                    if (double.TryParse((item.Value?.ToString() ?? ""), out value))
                    {
                        @event.Metrics[item.Key.Substring(7)] = value;
                    }
                }
                else
                {
                    @event.Properties[item.Key] = item.Value;
                }
            }
            return @event;
        }
    }
}
