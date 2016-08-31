using Microsoft.ApplicationInsights;
using System;
using Xunit;

namespace ApplicationInsights.TelemetryReplication.Logging.Tests
{
    public class AiLoggerProviderTest
    {
        [Fact]
        public void Has_GuardClause()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AiLoggerProvider(null));
            Assert.Throws<ArgumentNullException>(
                () => new AiLoggerProvider(null, null));
        }

        [Fact]
        public void CreateLogger_returns_AiLogger()
        {
            var sut = new AiLoggerProvider(() => new TelemetryClient());
            var logger = sut.CreateLogger("abc");
            Assert.IsType<AiLogger>(logger);
        }

        [Fact]
        public void CreateLogger_always_returns_new_logger_instance()
        {
            var sut = new AiLoggerProvider(() => new TelemetryClient());
            var logger1 = sut.CreateLogger("abc");
            var logger2 = sut.CreateLogger("abc");
            Assert.NotEqual(logger1, logger2);
            Assert.NotEqual(logger1.GetHashCode(), logger2.GetHashCode());
        }
    }
}
