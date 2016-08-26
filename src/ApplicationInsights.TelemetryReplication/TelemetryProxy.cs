using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationInsights.TelemetryReplication
{
    /// <summary>
    /// A Telemetry processor that replicates source telemetry message to original endpoint address and additional replicators.
    /// </summary>
    public class TelemetryProxy
    {
        private readonly TelemetryProxyOptions options;
        private readonly HttpClient httpClient;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes new TelemetryProxy instance with default options.
        /// </summary>
        public TelemetryProxy() : this(new TelemetryProxyOptions()) { }
        /// <summary>
        /// Initializes new TelemetryProxy instance with given options.
        /// </summary>
        /// <param name="options">TelemetryProxy options.</param>
        public TelemetryProxy(TelemetryProxyOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.DestinationUri == null)
            {
                throw new ArgumentException("DestinationUri is required.");
            }
            if (false == options.DestinationUri.IsAbsoluteUri)
            {
                throw new ArgumentException("DestinationUri must be an absolute uri.");
            }
            if (options.LoggerFactory == null)
            {
                throw new ArgumentException("LoggerFactory is required.");
            }
            if (options.Replicators == null)
            {
                options.Replicators = Enumerable.Empty<ITelemetryReplicator>();
            }
            if (options.HttpClientFactory == null)
            {
                options.HttpClientFactory = CreateDefaultHttpClient;
            }
            this.options = options;
            logger = options.LoggerFactory.CreateLogger<TelemetryProxy>();
            httpClient = options.HttpClientFactory();
            DestinationUri = options.DestinationUri;
            Replicators = options.Replicators.ToList();
        }

        /// <summary>
        /// Gets destination uri option.
        /// </summary>
        public Uri DestinationUri { get; private set; }
        /// <summary>
        /// Gets registered replicators.
        /// </summary>
        public IEnumerable<ITelemetryReplicator> Replicators { get; private set; }

        /// <summary>
        /// Processes original telemetry stream with headers.
        /// It sends stream and headers to original destination as it is,
        /// and also replicates stream and headers via replicators.
        /// If there was no errors, it returns HTTP response from the original destination.
        /// </summary>
        /// <param name="body">Source stream of HTTP Request body.</param>
        /// <param name="headers">Source HTTP Request headers.</param>
        /// <returns>The HTTP response from the original destination.</returns>
        public async Task<HttpResponseMessage> ProcessAsync(
            Stream body, 
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }
            var length = headers
                .Where(header => header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                .Select(header => header.Value.Select(v => int.Parse(v)).FirstOrDefault())
                .FirstOrDefault();
            if (length < 1 || length > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(headers), 
                    $"headers must have Content-Length with positive integer value.");
            }
            var buffer = ReadAll(body);
            if (buffer.Length < 1)
            {
                throw new InvalidOperationException(
                    $"HTTP body must be greater than 1 byte.");
            }

            var message = new HttpRequestMessage(HttpMethod.Post, DestinationUri);
            var filteredHeaders = headers
                .Where(header => HostHeadersFilter(header) && ContentHeadersFilter(header));
            foreach (var header in filteredHeaders)
            {
                message.Headers.Add(header.Key, header.Value);
            }
            message.Content = new ByteArrayContent(buffer);
            foreach (var header in headers.Where(header => !ContentHeadersFilter(header)))
            {
                message.Content.Headers.Add(header.Key, header.Value);
            }
            var response = await httpClient.SendAsync(message, CancellationToken.None);

            if (Replicators.Any())
            {
                using (var stream = new MemoryStream(buffer))
                {
                    await ReplicateAsync(stream, headers.Where(header => HostHeadersFilter(header)));
                }
            }

            return response;
        }

        private byte[] ReadAll(Stream body)
        {
            var stream = new MemoryStream();
            body.CopyTo(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            return buffer;
        }

        private static HttpClient CreateDefaultHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            return httpClient;
        }
        private static Func<KeyValuePair<string, IEnumerable<string>>, bool> HostHeadersFilter
            => header => !header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)
                && !header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase);
        private static Func<KeyValuePair<string, IEnumerable<string>>, bool> ContentHeadersFilter
            => header => !header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase);

        private Task ReplicateAsync(Stream body, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            var stream = body;
            IEnumerable<string> encodings = headers
                .Where(header => header.Key.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase))
                .Select(header => header.Value)
                .FirstOrDefault();
            if (encodings?.Contains("gzip", StringComparer.OrdinalIgnoreCase) ?? false)
            {
                var gzipStream = new GZipStream(body, CompressionMode.Decompress);
                stream = new MemoryStream();
                gzipStream.CopyTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
            }
            var jarray = DeserializeBody(stream);
            return Task.WhenAll(Replicators.Select(r => r.ReplicateAsync(jarray, headers)));
        }

        private JArray DeserializeBody(Stream body)
        {
            // Currently telemetries are sent as 'line-delimited json stream'(https://en.wikipedia.org/wiki/JSON_Streaming#Line_delimited_JSON).
            // You need to parse line-by-line or just replace new-line as comma so that it can be deserialized as JArray.
            using (var sr = new StreamReader(body))
            {
                var replaced = sr.ReadToEnd().Replace("\r\n", ",").Replace("\n", ",");
                var arrayString = "[" + replaced + "]";
                return JsonConvert.DeserializeObject<JArray>(arrayString);
            }
        }
    }
}
