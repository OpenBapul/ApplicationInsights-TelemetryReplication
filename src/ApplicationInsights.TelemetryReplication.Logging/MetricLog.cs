using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationInsights.TelemetryReplication.Logging
{
    /// <summary>
    /// A log represents metric.
    /// </summary>
    public class MetricLog
    {
        public MetricLog(string name, double value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            Name = name;
            Value = value;
        }

        /// <summary>
        /// The name of the metric. This is mandatory.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The value of this metric. This is mandatory.
        /// </summary>
        public double Value { get; set; }
        /// <summary>
        /// The standard deviation of this metric.
        /// </summary>
        public double? StandardDeviation { get; set; }
        /// <summary>
        /// The number of samples for this metric.
        /// </summary>
        public int? Count { get; set; }
        /// <summary>
        /// The minimum value of this metric.
        /// </summary>
        public double? Min { get; set; }
        /// <summary>
        /// The Maximum value of this metric.
        /// </summary>
        public double? Max { get; set; }
        /// <summary>
        /// The remark for the metric.
        /// </summary>
        public string Remark { get; set; }
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
            dic["LogType"] = nameof(MetricLog);
            dic[nameof(Value)] = Value;
            dic[nameof(Name)] = Name;
            if (StandardDeviation.HasValue)
            {
                dic[nameof(StandardDeviation)] = StandardDeviation;
            }
            if (Count.HasValue)
            {
                dic[nameof(Count)] = Count;
            }
            if (Min.HasValue)
            {
                dic[nameof(Min)] = Min;
            }
            if (Max.HasValue)
            {
                dic[nameof(Max)] = Max;
            }
            if (false == string.IsNullOrEmpty(Remark))
            {
                dic[nameof(Remark)] = Remark;
            }
            return dic;
        }

        /// <summary>
        /// Create a new MetricLog from given properties.
        /// </summary>
        /// <param name="properties">Key-value properties.</param>
        /// <remarks>
        /// Name and Value are mandatory properties.
        /// </remarks>
        /// <returns>Returns a new MetricLog if given properties have the Name and Value properties, or null otherwise.</returns>
        public static MetricLog Create(IEnumerable<KeyValuePair<string, object>> properties)
        {
            if (properties == null)
            {
                return null;
            }
            if (false == properties.Any(item => 
                item.Key.Equals("LogType", StringComparison.OrdinalIgnoreCase)
                && (item.Value?.ToString() ?? "").Equals(nameof(MetricLog), StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }
            var metric = new MetricLog("No name", 0);
            var dic = properties.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
            foreach (var item in dic.Where(item => 
                false == new string[]
                {
                    nameof(Name),
                    nameof(StandardDeviation),
                    nameof(Count),
                    nameof(Value),
                    nameof(Min),
                    nameof(Max),
                    nameof(Remark),
                }.Contains(item.Key, StringComparer.OrdinalIgnoreCase)))
            {
                metric.Properties[item.Key] = item.Value;
            }
            if (dic.ContainsKey(nameof(Name)))
            {
                metric.Name = dic[nameof(Name)]?.ToString();
            }
            else
            {
                return null;
            }
            if (dic.ContainsKey(nameof(Value)))
            {
                metric.Value = double.Parse(dic[nameof(Value)]?.ToString() ?? "0");
            }
            else
            {
                return null;
            }
            if (dic.ContainsKey(nameof(StandardDeviation)))
            {
                var standardDeviation = dic[nameof(StandardDeviation)];
                if (standardDeviation != null)
                {
                    double value = 0.0;
                    if (double.TryParse(standardDeviation.ToString(), out value))
                    {
                        metric.StandardDeviation = value;
                    }
                }
            }
            if (dic.ContainsKey(nameof(Count)))
            {
                var count = dic[nameof(Count)];
                if (count != null)
                {
                    int value = 0;
                    if (int.TryParse(count.ToString(), out value))
                    {
                        metric.Count = value;
                    }
                }
            }
            if (dic.ContainsKey(nameof(Min)))
            {
                var min = dic[nameof(Min)];
                if (min != null)
                {
                    double value = 0;
                    if (double.TryParse(min.ToString(), out value))
                    {
                        metric.Min = value;
                    }
                }
            }
            if (dic.ContainsKey(nameof(Max)))
            {
                var max = dic[nameof(Max)];
                if (max != null)
                {
                    double value = 0;
                    if (double.TryParse(max.ToString(), out value))
                    {
                        metric.Max = value;
                    }
                }
            }
            if (dic.ContainsKey(nameof(Remark)))
            {
                metric.Remark = dic[nameof(Remark)]?.ToString();
            }
            return metric;
        }
    }
}
