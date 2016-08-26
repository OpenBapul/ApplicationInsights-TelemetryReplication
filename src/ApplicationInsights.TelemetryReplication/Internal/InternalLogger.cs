using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace ApplicationInsights.TelemetryReplication.Internal
{
    internal class LoggerFactory : ILoggerFactory
    {
        private readonly LogLevel minimumLogLevel;
        public LoggerFactory(LogLevel minimumLogLevel)
        {
            this.minimumLogLevel = minimumLogLevel;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            // noop.
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new Logger(categoryName, minimumLogLevel);
        }

        public void Dispose()
        {
            // noop.
        }
    }
    
    internal class Logger : ILogger
    {
        private readonly string name;
        private readonly LogLevel minimumLogLevel;
        public Logger(string name, LogLevel minimumLogLevel)
        {
            this.name = name;
            this.minimumLogLevel = minimumLogLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new NoopDisposable();
        }
        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= minimumLogLevel;
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
                    $"{s}\n{e}";
            }

            var message = formatter(state, exception);

            if (!string.IsNullOrEmpty(message))
            {
                WriteMessage(logLevel, name, message);
            }
        }

        private void WriteMessage(LogLevel logLevel, string logName, string message)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    Trace.WriteLine(message, logName);
                    break;
                case LogLevel.Information:
                    Trace.TraceInformation($"[{logLevel.ToString()}]{message}");
                    break;
                case LogLevel.Warning:
                    Trace.TraceWarning($"[{logLevel.ToString()}]{message}");
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Trace.TraceError($"[{logLevel.ToString()}]{message}");
                    break;
            }
        }
    }
}
