using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace ApplicationInsights.TelemetryReplication.Logging.Tests
{
    public class MetricLogTest
    {
        [Fact]
        public void Has_GuardClause()
        {
            Assert.Throws<ArgumentNullException>(() => new MetricLog(null, 0));
            Assert.Throws<ArgumentNullException>(() => new MetricLog("", 0));
        }

        [Fact]
        public void ToDictionary_contains_all_metrics_and_properties()
        {
            var now = DateTimeOffset.Now;
            var metric = new MetricLog("test", 1)
            {
                Count = 1,
                Max = 1,
                Min = 1,
                Remark = "remark",
                StandardDeviation = 1
            };
            metric.Properties["test"] = "test";
            metric.Properties["timestamp"] = now;
            var result = metric.ToDictionary();
            Assert.Equal("test", result["name"].ToString());
            Assert.Equal(1.0, (double)result["value"]);
            Assert.Equal(1, (int)result["count"]);
            Assert.Equal(1.0, (double)result["min"]);
            Assert.Equal(1.0, (double)result["max"]);
            Assert.Equal(1.0, (double)result["standardDeviation"]);
            Assert.Equal("remark", result["remark"].ToString());
            Assert.Equal("test", result["test"].ToString());
            Assert.Equal(now, (DateTimeOffset)result["timestamp"]);
        }

        [Fact]
        public void ToDictionary_ignores_case()
        {
            var metric = new MetricLog("test", 1);
            var result = metric.ToDictionary();
            Assert.Equal("test", result["name"].ToString());
            Assert.Equal("test", result["Name"].ToString());
            Assert.Equal("test", result["NAME"].ToString());
        }

        [Theory, ClassData(typeof(InvalidProperties))]
        public void Create_returns_null_without_name_nor_value(Dictionary<string, object> properties)
        {
            var result = MetricLog.Create(properties);
            Assert.Null(result);
        }

        [Fact]
        public void Creates_contains_all_metrics_and_properties()
        {
            var now = DateTimeOffset.Now;
            var properties = new Dictionary<string, object>
            {
                { "LogType", "MetricLog" },
                { "name", "test" },
                { "value", 1.0 },
                { "count", 1 },
                { "min", 1.0 },
                { "max", 1.0 },
                { "standardDeviation", 1.0 },
                { "remark", "remark" },
                { "test", "test" },
                { "timestamp", now },
            };
            var result = MetricLog.Create(properties);
            Assert.Equal("test", result.Name);
            Assert.Equal(1.0, result.Value);
            Assert.Equal(1, result.Count);
            Assert.Equal(1.0, result.Min);
            Assert.Equal(1.0, result.Max);
            Assert.Equal(1.0, result.StandardDeviation);
            Assert.Equal("remark", result.Remark);
            Assert.Equal("test", result.Properties["test"]);
            Assert.Equal(now, (DateTimeOffset)result.Properties["timestamp"]);
        }

        [Theory]
        [InlineData("name", "value")]
        [InlineData("Name", "Value")]
        [InlineData("NAME", "VALUE")]
        public void Creates_ignores_case(string name, string value)
        {
            var metric = MetricLog.Create(new Dictionary<string, object>
            { { "LogType", "MetricLog" }, { name, "name" }, { value, 1.0 } });
            Assert.NotNull(metric);
        }

        private class InvalidProperties : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { null };
                yield return new object[] { new Dictionary<string, object>() };
                yield return new object[] { new Dictionary<string, object> { { "name", null } } };
                yield return new object[] { new Dictionary<string, object> { { "name", "" } } };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
