using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights;
using ApplicationInsights.TelemetryReplication.AspNetCore;
using ApplicationInsights.TelemetryReplication.ElasticSearch;
using ApplicationInsights.TelemetryReplication.Logging;
using ApplicationInsights.TelemetryReplication;

namespace Samples.AspNetCore
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile($"appsettings.hidden.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddElasticSearchTelemetryReplicator(options =>
            {
                options.BulkEndPoint = new Uri(Configuration["ElasticSearch:BulkEndPoint"], UriKind.Absolute);
                options.IndexSelector = jobject => new IndexDefinition
                {
                    Index = Configuration["ElasticSearch:Index"],
                    Type = Configuration["ElasticSearch:Type"],
                };
            });
            services.AddApplicationInsightsTelemetryReplication();
            services.AddMvcCore();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app, 
            IHostingEnvironment env, 
            ILoggerFactory loggerFactory)
        {
            loggerFactory
                .AddAi(() => app.ApplicationServices.GetService<TelemetryClient>(), LogLevel.Trace)
                .AddDebug(LogLevel.Trace);
            app.UseApplicationInsightsTelemetryReplication(
                env.GetAppId(typeof(Startup)),
                new Uri(Configuration["ApplicationInsights:ProxyUri"], UriKind.Absolute));
            app.UseApplicationInsightsRequestTelemetry();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseExceptionHandler();
            }

            app.UseApplicationInsightsExceptionTelemetry();

            app.Run(async (context) =>
            {
                var client = context.RequestServices.GetService<TelemetryClient>();
                client.TrackEvent("Hello World Event");
                var logger = context.RequestServices.GetService<ILoggerFactory>().CreateLogger("SampleApp");
                logger.LogInformation("Hello World Event by logger");
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
