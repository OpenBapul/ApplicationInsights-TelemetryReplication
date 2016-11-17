using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ApplicationInsights.TelemetryReplication.ElasticSearch
{
    /// <summary>
    /// TelemetryReplicator that replicates telemetries to ElasticSearch as bulk index.
    /// </summary>
    public class ElasticSearchTelemetryReplicator : ITelemetryReplicator
    {
        private readonly ElasticSearchTelemetryReplicatorOptions options;
        private readonly HttpClient httpClient;
        private readonly ILogger logger;
        public ElasticSearchTelemetryReplicator(
            ElasticSearchTelemetryReplicatorOptions options,
            ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            if (options.BulkEndPoint == null)
            {
                throw new ArgumentException("EndPoint is required.");
            }
            if (false == options.BulkEndPoint.IsAbsoluteUri)
            {
                throw new ArgumentException("EndPoint must be an absolute uri.");
            }
            if (options.IndexSelector == null)
            {
                throw new ArgumentException("IndexSelector is required.");
            }
            if (options.HttpClientFactory == null)
            {
                options.HttpClientFactory = CreateDefaultHttpClient;
            }
            if (options.JsonSerializerSettings == null)
            {
                options.JsonSerializerSettings = GetDefaultSerializationSettings();
            }
            this.options = options;
            httpClient = options.HttpClientFactory();
            logger = loggerFactory.CreateLogger<ElasticSearchTelemetryReplicator>();
        }

        public Task ReplicateAsync(JArray body, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            logger.LogInformation($"Replicates {body.Count} item(s).");
            var data = GetBulkBody(body)
                .Select(item => JsonConvert.SerializeObject(item, options.JsonSerializerSettings) + "\n")
                .Join("");
            var message = new HttpRequestMessage(HttpMethod.Post,
                options.BulkEndPoint);
            message.Content = new StringContent(data, Encoding.UTF8);

            return httpClient.SendAsync(message, CancellationToken.None);
        }

        private static JsonSerializerSettings GetDefaultSerializationSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.Converters.Add(new StringEnumConverter(false));
            return settings;
        }

        private static HttpClient CreateDefaultHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            return httpClient;
        }

        private IEnumerable<JObject> GetBulkBody(JArray body)
        {
            foreach (JObject item in body)
            {
                var serializer = JsonSerializer.Create(options.JsonSerializerSettings);
                // create index
                yield return JObject.FromObject(new IndexEnvelope(options.IndexSelector(item)), serializer);
                // index source
                // we need to replace '.' in the field name to '_'.
                yield return EnsureProperNames(item);
            }
        }

        private T EnsureProperNames<T>(T jtoken) where T : JToken
        {
            if (jtoken.Type == JTokenType.Array)
            {
                var jarray = jtoken as JArray;
                var count = jarray.Count;
                for (int i = 0; i < count; i++)
                {
                    jarray[i] = EnsureProperNames(jarray[i]);
                }
                return jtoken;
            }
            else if (jtoken.Type == JTokenType.Object)
            {
                var properties = (jtoken as JObject).Properties().ToArray();
                var jobject = new JObject();
                foreach (var item in properties)
                {
                    jobject.Add(
                        item.Name.Replace(".", "_"), 
                        EnsureProperNames(item.Value));
                }
                return jobject as T;
            }
            else if (jtoken.Type == JTokenType.Property)
            {
                var jproperty = jtoken as JProperty;
                return new JProperty(jproperty.Name.Replace(".", "_"), jproperty.Value) as T;
            }
            else
            {
                return jtoken;
            }
        }

        private class IndexEnvelope
        {
            public IndexEnvelope(IndexDefinition index)
            {
                Index = index;
            }
            public IndexDefinition Index { get; private set; }
        }
    }
}
