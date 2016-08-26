using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;

namespace ApplicationInsights.TelemetryReplication.ElasticSearch
{
    /// <summary>
    /// ElasticSearchTelemetryReplicator options.
    /// </summary>
    public class ElasticSearchTelemetryReplicatorOptions
    {
        /// <summary>
        /// End-point of the destination ElasticSearch bulk API.
        /// </summary>
        public Uri BulkEndPoint { get; set; }
        /// <summary>
        /// A selector that generate index definition by single JObject.
        /// </summary>
        public Func<JObject, IndexDefinition> IndexSelector { get; set; }
        /// <summary>
        /// A factory to create HttpClient that sends bulk indexes to the destination host.
        /// </summary>
        public Func<HttpClient> HttpClientFactory { get; set; }
        /// <summary>
        /// Serializer settings that used in Json serialization.
        /// </summary>
        public JsonSerializerSettings JsonSerializerSettings { get; set; }
    }
}