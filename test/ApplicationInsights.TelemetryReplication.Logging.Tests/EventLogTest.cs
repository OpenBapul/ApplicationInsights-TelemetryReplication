using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace ApplicationInsights.TelemetryReplication.Logging.Tests
{
    public class EventLogTest
    {
        [Fact]
        public void Has_GuardClause()
        {
            Assert.Throws<ArgumentNullException>(() => new EventLog(null));
            Assert.Throws<ArgumentNullException>(() => new EventLog(""));
        }

        [Fact]
        public void ToDictionary_contains_all_metrics_and_properties()
        {
            var now = DateTimeOffset.Now;
            var @event = new EventLog("test");
            @event.Metrics["Duration"] = 123.123;
            @event.Properties["test"] = "test";
            @event.Properties["null"] = null;
            @event.Properties["timestamp"] = now;
            var result = @event.ToDictionary();
            Assert.Equal("test", result["name"].ToString());
            Assert.Equal(123.123, (double)result["Metric.Duration"]);
            Assert.Equal("test", result["test"].ToString());
            Assert.Equal(null, result["null"]);
            Assert.Equal(now, (DateTimeOffset)result["timestamp"]);
        }

        [Fact]
        public void ToDictionary_ignores_case()
        {
            var @event = new EventLog("test");
            @event.Metrics["Duration"] = 123.123;
            var result = @event.ToDictionary();
            Assert.Equal("test", result["name"].ToString());
            Assert.Equal("test", result["Name"].ToString());
            Assert.Equal("test", result["NAME"].ToString());
            Assert.Equal(123.123, (double)result["metric.duration"]);
            Assert.Equal(123.123, (double)result["Metric.Duration"]);
            Assert.Equal(123.123, (double)result["METRIC.DURATION"]);
        }

        [Theory, ClassData(typeof(InvalidProperties))]
        public void Create_returns_null_without_name_nor_value(Dictionary<string, object> properties)
        {
            var result = EventLog.Create(properties);
            Assert.Null(result);
        }

        [Fact]
        public void Creates_contains_all_metrics_and_properties()
        {
            var now = DateTimeOffset.Now;
            var properties = new Dictionary<string, object>
            {
                { "LogType", "EventLog" },
                { "name", "test" },
                { "Metric.Duration", 123.123 },
                { "test", "test" },
                { "null", null },
                { "timestamp", now },
            };
            var result = EventLog.Create(properties);
            Assert.Equal("test", result.Name);
            Assert.Equal(123.123, result.Metrics["Duration"]);
            Assert.Equal("test", result.Properties["test"]);
            Assert.Equal(null, result.Properties["null"]);
            Assert.Equal(now, (DateTimeOffset)result.Properties["timestamp"]);
        }

        [Theory]
        [InlineData("name")]
        [InlineData("Name")]
        [InlineData("NAME")]
        public void Creates_ignores_case(string name)
        {
            var metric = EventLog.Create(new Dictionary<string, object>
            { { "LogType", "EventLog" }, { name, "name" } });
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
