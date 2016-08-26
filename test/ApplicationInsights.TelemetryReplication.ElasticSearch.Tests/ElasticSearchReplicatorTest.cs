using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Collections;

namespace ApplicationInsights.TelemetryReplication.ElasticSearch.Tests
{
    public class ElasticSearchReplicatorTest
    {
        [Fact]
        public void Has_GuardClause()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ElasticSearchTelemetryReplicator(null));
        }
        [Theory, ClassData(typeof(InvalidOptions))]
        public void Throws_ArgumentException_with_invalid_options(ElasticSearchTelemetryReplicatorOptions options)
        {
            Assert.Throws<ArgumentException>(
                () => new ElasticSearchTelemetryReplicator(options));
        }

        [Fact]
        public async Task ComplexObject()
        {
            var httpClientMock = new Mock<HttpClient>();
            HttpRequestMessage recordedMessage = null;
            httpClientMock
                .Setup(httpClient => httpClient.SendAsync(
                    It.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK))
                .Callback<HttpRequestMessage, CancellationToken>((message, token) => recordedMessage = message);
            var options = new ElasticSearchTelemetryReplicatorOptions
            {
                BulkEndPoint = new Uri("http://localhost:12345", UriKind.Absolute),
                HttpClientFactory = () => httpClientMock.Object,
                IndexSelector = _ => new IndexDefinition { Index = "ai", Type = "telemetry" },
            };
            var sut = new ElasticSearchTelemetryReplicator(options);
            var expected = ComplexSampleObjects;
            var jarray = JArray.FromObject(expected);
            await sut.ReplicateAsync(jarray, Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>());

            var body = await recordedMessage.Content.ReadAsStringAsync();
            var list = body.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int odd = 0;
            var json = $"[{string.Join(",", list.Where(item => (++odd % 2) == 0))}]";
            var actual = JsonConvert.DeserializeObject<ComplexSample[]>(json);

            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        private class InvalidOptions : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new[] { new ElasticSearchTelemetryReplicatorOptions() };
                yield return new[] { new ElasticSearchTelemetryReplicatorOptions
                {
                    BulkEndPoint = new Uri("invalid", UriKind.Relative)
                } };
                yield return new[] { new ElasticSearchTelemetryReplicatorOptions
                {
                    BulkEndPoint = new Uri("http://absolute", UriKind.Absolute),
                } };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private static ComplexSample[] ComplexSampleObjects => new[]
        {
            new ComplexSample(),
            new ComplexSample
            {
                NestedObject = new ComplexSample()
            },
            new ComplexSample
            {
                NestedArray = new ComplexSample[]
                {
                    new ComplexSample(),
                    new ComplexSample(),
                }
            },
        };

        private class ComplexSample
            : IEquatable<ComplexSample>
        {
            public string String { get; set; } = Guid.NewGuid().ToString();
            public int Numeric { get; set; } = new Random().Next();
            public DateTime DateTime { get; set; } = DateTime.UtcNow;
            public bool Boolean { get; set; } = true;
            public double Double { get; set; } = new Random().NextDouble();
            public Guid Guid { get; set; } = Guid.NewGuid();
            [JsonProperty(PropertyName = "@valid$property#name")]
            public string ValidSpecialPropertyName { get; set; } = Guid.NewGuid().ToString();
            public ComplexSample NestedObject { get; set; }
            public ComplexSample[] NestedArray { get; set; }

            public bool Equals(ComplexSample other)
            {
                if (other == null)
                {
                    return false;
                }
                var result = other.String == String
                    && other.Numeric == Numeric
                    && other.DateTime == DateTime
                    && other.Boolean == Boolean
                    && other.Guid == Guid
                    && other.ValidSpecialPropertyName == ValidSpecialPropertyName
                    && (other.NestedObject == null && NestedObject == null)
                    || (other.NestedObject != null && other.NestedObject.Equals(NestedObject))
                    && ((other.NestedArray == null && NestedArray == null)
                    || ((other.NestedArray?.Length ?? 0) == (NestedArray?.Length ?? 0)
                        && Enumerable.Range(0, other.NestedArray.Length)
                        .All(i => (other.NestedArray[i] == null && NestedArray[i] == null)
                            || (other.NestedArray[i] != null && other.NestedArray[i].Equals(NestedArray[i])))));
                return result;
            }
        }
    }
}
