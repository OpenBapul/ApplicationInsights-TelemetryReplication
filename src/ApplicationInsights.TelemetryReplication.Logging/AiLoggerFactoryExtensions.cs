using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using System;

namespace ApplicationInsights.TelemetryReplication.Logging
{
    public static class AiLoggerFactoryExtensions
    {
        /// <summary>
        /// Add an AiLoggerProvider with given parameters to the logging system.
        /// The minimum level of log will be LogLevel.Information.
        /// </summary>
        /// <param name="factory">The logger factory.</param>
        /// <param name="telemetryClientFactory">A telemetry client factory to track telemetry.</param>
        /// <returns>The logger factory given.</returns>
        public static ILoggerFactory AddAi(
            this ILoggerFactory factory,
            Func<TelemetryClient> telemetryClientFactory)
        {
            return AddAi(
                factory,
                telemetryClientFactory,
                (name, level) => level < LogLevel.Information);
        }

        /// <summary>
        /// Add an AiLoggerProvider with given parameters to the logging system.
        /// </summary>
        /// <param name="factory">The logger factory.</param>
        /// <param name="telemetryClientFactory">A telemetry client factory to track telemetry.</param>
        /// <param name="minimumLogLevel">The minimum level of log.</param>
        /// <returns>The logger factory given.</returns>
        public static ILoggerFactory AddAi(
            this ILoggerFactory factory,
            Func<TelemetryClient> telemetryClientFactory,
            LogLevel minimumLogLevel)
        {
            return AddAi(
                factory,
                telemetryClientFactory,
                (name, level) => level < minimumLogLevel);
        }

        /// <summary>
        /// Add an AiLoggerProvider with given parameters to the logging system.
        /// </summary>
        /// <param name="factory">The logger factory.</param>
        /// <param name="telemetryClientFactory">A telemetry client factory to track telemetry.</param>
        /// <param name="logFilter">Specific log filter that ignores tracking telemetry.</param>
        /// <returns>The logger factory given.</returns>
        public static ILoggerFactory AddAi(
            this ILoggerFactory factory, 
            Func<TelemetryClient> telemetryClientFactory,
            LogFilter logFilter)
        {
            factory.AddProvider(new AiLoggerProvider(telemetryClientFactory, logFilter));
            return factory;
        }
    }
}