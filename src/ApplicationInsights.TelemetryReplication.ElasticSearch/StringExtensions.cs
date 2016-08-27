using System.Collections.Generic;

namespace ApplicationInsights.TelemetryReplication.ElasticSearch
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Join string sequense with separator.
        /// </summary>
        /// <param name="source">the string sequence.</param>
        /// <param name="separator">the separator.</param>
        /// <returns>joined string.</returns>
        public static string Join(this IEnumerable<string> source, string separator)
            => string.Join(separator, source);
    }
}
