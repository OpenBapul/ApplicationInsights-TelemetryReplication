using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApplicationInsights.TelemetryReplication
{
    /// <summary>
    /// Represents a type replicates source body and headers to the its own destination.
    /// </summary>
    public interface ITelemetryReplicator
    {
        /// <summary>
        /// Replicates the given body and headers to the its own destination.
        /// </summary>
        /// <param name="body">The body as Json array form.</param>
        /// <param name="headers">The original HTTP request headers.</param>
        /// <returns></returns>
        Task ReplicateAsync(JArray body, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers);
    }
}