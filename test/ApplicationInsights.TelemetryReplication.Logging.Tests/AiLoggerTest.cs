using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace ApplicationInsights.TelemetryReplication.Logging.Tests
{
    public class AiLoggerTest
    {
        [Theory, ClassData(typeof(InvalidAiLoggerParameters))]
        public void Has_GuardClause(string categoryName, TelemetryClient client, LogFilter logFilter)
        {
            Assert.Throws<ArgumentNullException>(
                () => new AiLogger(categoryName, client, logFilter));
        }

        [Fact]
        public void BeginScope_always_returns_same_instance()
        {
            var sut = new AiLogger("abc", new TelemetryClient(), (a, b) => false);
            var scope1 = sut.BeginScope("");
            var scope2 = sut.BeginScope("");
            Assert.Equal(scope1, scope2);
        }

        [Fact]
        public void IsEnabled_returns_false_when_category_name_starts_with_internal_namespace()
        {
            var sut = new AiLogger("ApplicationInsights.TelemetryReplication.Whatever", new TelemetryClient(), (a, b) => false);
            Assert.False(sut.IsEnabled(LogLevel.Information));
        }

        [Fact]
        public void IsEnabled_returns_true()
        {
            var sut = new AiLogger("abc", new TelemetryClient(), (name, logLevel) => logLevel < LogLevel.Information);
            Assert.True(sut.IsEnabled(LogLevel.Information));
        }

        [Fact]
        public void IsEnabled_returns_false()
        {
            var sut = new AiLogger("abc", new TelemetryClient(), (name, logLevel) => logLevel < LogLevel.Information);
            Assert.False(sut.IsEnabled(LogLevel.Debug));
        }

        // we cannot test TelemetryClient since it is sealed and doesn't have virtual method.
        public void Log_tracks_trace()
        {
        }

        private class InvalidAiLoggerParameters : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { (string)null, (TelemetryClient)null, (LogFilter)null };
                yield return new object[] { "", (TelemetryClient)null, (LogFilter)null };
                yield return new object[] { " ", (TelemetryClient)null, (LogFilter)null };
                yield return new object[] { "abc", (TelemetryClient)null, (LogFilter)null };
                yield return new object[] { "abc", new TelemetryClient(), (LogFilter)null };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
