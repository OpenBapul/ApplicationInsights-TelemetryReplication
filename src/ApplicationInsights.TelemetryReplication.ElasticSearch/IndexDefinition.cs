using Newtonsoft.Json;

namespace ApplicationInsights.TelemetryReplication.ElasticSearch
{
    /// <summary>
    /// An Index definition that used in bulk operation.
    /// </summary>
    public class IndexDefinition
    {
        /// <summary>
        /// Name of index(_index field).
        /// </summary>
        [JsonProperty(PropertyName = "_index")]
        public string Index { get; set; }
        /// <summary>
        /// Name of type(_type field).
        /// </summary>
        [JsonProperty(PropertyName = "_type")]
        public string Type { get; set; }
        /// <summary>
        /// Optional Id(_id field).
        /// You should set this field if there are any chances to update exist index.
        /// </summary>
        [JsonProperty(PropertyName = "_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; set; }
    }
}
