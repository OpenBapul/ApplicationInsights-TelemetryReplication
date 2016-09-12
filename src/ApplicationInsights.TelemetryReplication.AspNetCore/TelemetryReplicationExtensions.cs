using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;

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
        /// <param name="env">The hosting environment an application running in.</param>
        /// <param name="applicationType">The type of any class in the application.</param>
        /// <param name="proxyUri">Telemetry proxy uri. It must be an absoulte uri.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseApplicationInsightsTelemetryReplication(
            this IApplicationBuilder app,
            IHostingEnvironment env,
            Type applicationType,
            Uri proxyUri)
        {
            return UseApplicationInsightsTelemetryReplication(app, env.GetAppId(applicationType), proxyUri);
        }

        /// <summary>
        /// Use ApplicationInsights TelemetryReplication app.
        /// The telemetry proxy will be determined by the first request's scheme and host.
        /// </summary>
        /// <param name="app">The App builder.</param>
        /// <param name="appId">The application identifier.</param>
        /// <param name="proxyUri">Telemetry proxy uri. It must be an absoulte uri.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseApplicationInsightsTelemetryReplication(
            this IApplicationBuilder app,
            AppId appId,
            Uri proxyUri)
        {
            var configuration = app.ApplicationServices.GetService<TelemetryConfiguration>();
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
            OriginalTelemetryChannelEndpointAddress =
                configuration.TelemetryChannel.EndpointAddress;
            app.UseMiddleware<TelemetryReplicationMiddleware>(
                appId,
                proxyUri, 
                configuration, 
                loggerFactory);
            return app;
        }

        /// <summary>
        /// Get application identifier by given arguments.
        /// </summary>
        /// <param name="env">The hosting environment an application running in.</param>
        /// <param name="applicationType">The type of any class in the application.</param>
        /// <returns></returns>
        public static AppId GetAppId(this IHostingEnvironment env, Type applicationType)
        {
            return new AppId(
                env.ApplicationName, 
                applicationType.GetTypeInfo().Assembly.GetName().Version, 
                env.EnvironmentName);
        }
    }
}
