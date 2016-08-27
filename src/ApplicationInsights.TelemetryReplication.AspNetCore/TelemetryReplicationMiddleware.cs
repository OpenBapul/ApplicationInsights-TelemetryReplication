using ApplicationInsights.TelemetryReplication;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TelemetryReplication.AspNetCore
{
    public class TelemetryReplicationMiddleware
    {
        private readonly RequestDelegate next;
        private readonly string proxyPath;
        private readonly TelemetryConfiguration telemetryConfiguration;
        private readonly ILogger logger;
        public TelemetryReplicationMiddleware(
            RequestDelegate next,
            string proxyPath,
            TelemetryConfiguration telemetryConfiguration,
            ILoggerFactory loggerFactory)
        {
            if (proxyPath == null)
            {
                throw new ArgumentNullException(nameof(proxyPath));
            }
            if (false == proxyPath.StartsWith("/"))
            {
                throw new ArgumentException("proxyPath must be started with '/'");
            }
            if (telemetryConfiguration == null)
            {
                telemetryConfiguration = TelemetryConfiguration.Active;
            }
            if (loggerFactory == null)
            {
                logger = new NoopLogger(nameof(TelemetryReplicationMiddleware));
            }
            else
            {
                logger = loggerFactory.CreateLogger<TelemetryReplicationMiddleware>();
            }

            this.next = next;
            this.proxyPath = proxyPath;
            this.telemetryConfiguration = telemetryConfiguration;
        }

        private int checker = 0;
        public async Task Invoke(HttpContext context)
        {
            if (Interlocked.CompareExchange(ref checker, 1, 0) == 0)
            {
                telemetryConfiguration.TelemetryChannel.EndpointAddress
                = $"{context.Request.Scheme}://{context.Request.Host}{proxyPath}";
                logger.LogInformation($"The end-point of Telemetry proxy has been determinated. {telemetryConfiguration.TelemetryChannel.EndpointAddress}");
            }
            if (context.Request.Path.Equals(proxyPath))
            {
                logger.LogDebug($"A telemetry transmission({context.TraceIdentifier}) is being processed by the TelemetryProxy.");
                var proxy = context.RequestServices.GetService<TelemetryProxy>();
                var headers = context.Request.Headers
                    .Select(header => new KeyValuePair<string, IEnumerable<string>>(
                        header.Key,
                        header.Value));
                var response = await proxy.ProcessAsync(context.Request.Body, headers);
                foreach (var header in response.Headers)
                {
                    context.Response.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));
                }
                context.Response.StatusCode = (int)response.StatusCode;
                await context.Response.WriteAsync(await response.Content.ReadAsStringAsync(), Encoding.UTF8);
                logger.LogDebug($"A telemetry transmission({context.TraceIdentifier}) is completed.");
            }
            else
            {
                await next(context);
            }
        }

        private class NoopLogger : ILogger
        {
            private readonly string logName;
            public NoopLogger(string logName)
            {
                this.logName = logName;
            }
            public IDisposable BeginScope<TState>(TState state)
            {
                return Noop.Instance;
            }

            private class Noop : IDisposable
            {
                public static Noop Instance = new Noop();
                public void Dispose()
                {
                    // noop.
                }
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (formatter == null)
                {
                    formatter = (s, ex) => $"{s}" + (ex == null ? "" : $"\n[exception]\n{ex}");
                }
                Trace.WriteLine($"[{logLevel}][{eventId}][{logName}]{formatter(state, exception)}");
            }
        }
    }
}
