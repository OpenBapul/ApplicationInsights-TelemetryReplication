using System.Collections.Generic;

namespace ApplicationInsights.TelemetryReplication
{
    /// <summary>
    /// An interface that represents the collection of ITelemetryReplicator factory.
    /// </summary>
    public interface ITelemetryReplicatorsFactory
    {
        /// <summary>
        /// Create a collection of ITelemetryReplicator instances.
        /// </summary>
        /// <returns>A collection of ITelemetryReplicator instances.</returns>
        IEnumerable<ITelemetryReplicator> Create();
    }
}
