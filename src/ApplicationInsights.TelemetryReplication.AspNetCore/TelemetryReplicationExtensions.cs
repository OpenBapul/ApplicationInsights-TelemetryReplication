﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace ApplicationInsights.TelemetryReplication.AspNetCore
{
    public static class TelemetryReplicationExtensions
    {
        private static string OriginalTelemetryChannelEndpointAddress { get; set; }

        public static IServiceCollection AddApplicationInsightsTelemetryReplication(
            this IServiceCollection services)
        {
            return AddApplicationInsightsTelemetryReplication(services, null);
        }
        public static IServiceCollection AddApplicationInsightsTelemetryReplication(
            this IServiceCollection services,
            Action<TelemetryProxyOptions> configure)
        {
            services.AddSingleton(provider =>
            {
                var options = new TelemetryProxyOptions();
                configure?.Invoke(options);
                if (OriginalTelemetryChannelEndpointAddress != null)
                {
                    options.DestinationUri = new Uri(
                        OriginalTelemetryChannelEndpointAddress, 
                        UriKind.Absolute);
                }
                return options;
            });
            services.AddSingleton<TelemetryProxy>();
            return services;
        }

        /// <summary>
        /// Use ApplicationInsights TelemetryReplication app.
        /// The telemetry proxy will be determined by the first request's scheme and host.
        /// </summary>
        /// <param name="app">The App builder.</param>
        /// <param name="appName">The Application name.</param>
        /// <param name="proxyPath">Telemetry proxy path. It must be started with '/'.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseApplicationInsightsTelemetryReplication(
            this IApplicationBuilder app,
            string appName,
            string proxyPath)
        {
            var configuration = app.ApplicationServices.GetService<TelemetryConfiguration>();
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
            OriginalTelemetryChannelEndpointAddress =
                configuration.TelemetryChannel.EndpointAddress;
            app.UseMiddleware<TelemetryReplicationMiddleware>(
                appName,
                proxyPath, 
                configuration, 
                loggerFactory);
            return app;
        }
    }
}
