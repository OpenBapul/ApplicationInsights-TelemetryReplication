using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.IO.Compression;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using System.Text;
using System.Threading;

namespace ApplicationInsights.TelemetryReplication.Tests
{
    public class TelemetryProxyTest
    {
        [Fact]
        public void Has_GuardClause()
        {
            Assert.Throws<ArgumentNullException>(
                () => new TelemetryProxy(null));
        }

        [Theory, ClassData(typeof(InvalidTelemetryProxyOptions))]
        public void Throws_ArgumentException_when_options_are_not_valid(TelemetryProxyOptions options)
        {
            Assert.Throws<ArgumentException>(
                () => new TelemetryProxy(options));
        }

		[Fact]
		public void Replicators_are_empty_when_null_replicators_options()
        {
            var options = new TelemetryProxyOptions { Replicators = null };
            var sut = new TelemetryProxy(options);
            Assert.False(sut.Replicators.Any());
        }

		[Theory, ClassData(typeof(NullProcessAsyncParameters))]
		public async Task ProcessAsync_has_GuardClause(Stream body, HttpRequestHeaders headers)
        {
            var sut = new TelemetryProxy();
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => sut.ProcessAsync(body, headers));
        }

		[Fact]
		public async Task ProcessAsync_sends_telemetries_to_original_destination()
        {
            var httpClientMock = new Mock<HttpClient>();
            var options = new TelemetryProxyOptions
            {
                HttpClientFactory = () => httpClientMock.Object,
            };
            var sut = new TelemetryProxy(options);
            await sut.ProcessAsync(GetSampleGzipBody(), SampleHeaders);

            httpClientMock
                .Verify(httpClient => 
					httpClient.SendAsync(
						It.Is<HttpRequestMessage>(message =>
							message.RequestUri.Equals(options.DestinationUri)),
						It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_returns_response_from_original_destination()
        {
            var httpClientMock = new Mock<HttpClient>();
            httpClientMock
                .Setup(httpClient =>
                    httpClient.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            var options = new TelemetryProxyOptions
            {
                HttpClientFactory = () => httpClientMock.Object,
            };
            var sut = new TelemetryProxy(options);
            var response = await sut.ProcessAsync(GetSampleGzipBody(), SampleHeaders);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ProcessAsync_sends_telemetries_to_all_replicators()
        {
            var httpClientMock = new Mock<HttpClient>();
            var options = new TelemetryProxyOptions
            {
                HttpClientFactory = () => httpClientMock.Object,
				Replicators = new []
                {
					GetTelemetryReplicatorMock().Object,
                    GetTelemetryReplicatorMock().Object,
                    GetTelemetryReplicatorMock().Object,
                },
            };
            var sut = new TelemetryProxy(options);
            await sut.ProcessAsync(GetSampleGzipBody(), SampleHeaders);

            Assert.All(options.Replicators, replicator =>
            {
                var mock = Mock.Get(replicator);
                mock.Verify(x =>
                    x.ReplicateAsync(
                        It.Is<JArray>(array => array.Count == 2),
                        It.Is<IEnumerable<KeyValuePair<string, IEnumerable<string>>>>(headers => headers.Count() == 7)),
                    Times.Once);
            });
        }

		private Mock<ITelemetryReplicator> GetTelemetryReplicatorMock()
        {
            var mock = new Mock<ITelemetryReplicator>();
            mock.Setup(x => x.ReplicateAsync(
                It.IsAny<JArray>(),
                It.IsAny<IEnumerable<KeyValuePair<string, IEnumerable<string>>>>()))
                .Returns(Task.CompletedTask);
            return mock;
        }

        private Stream GetSampleGzipBody()
        {
            var target = new MemoryStream();
            using (var source = new MemoryStream(Encoding.UTF8.GetBytes(SampleBodyString)))
            {
                var stream = new GZipStream(target, CompressionMode.Compress);
                source.CopyTo(stream);
                stream.Flush();
            }
            target.Seek(0, SeekOrigin.Begin);
            return target;
        }

        private const string SampleBodyString =
@"{""name"":""Microsoft.ApplicationInsights.00000000000000000000000000000000.Request"",""time"":""2016-08-24T03:37:29.6219639Z"",""iKey"":""00000000-0000-0000-0000-000000000000"",""tags"":{""ai.device.roleInstance"":""gongdo-pc"",""ai.internal.sdkVersion"":""aspnet5c:1.0.0"",""ai.session.id"":""XMiur"",""ai.user.id"":""YVWRF"",""ai.operation.name"":""GET Home/Index"",""ai.operation.id"":""ll26wbY34hE="",""ai.user.userAgent"":""Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36""},""data"":{""baseType"":""RequestData"",""baseData"":{""ver"":2,""id"":""ll26wbY34hE="",""name"":""GET Home/Index"",""startTime"":""2016-08-24T03:37:29.6219639+00:00"",""duration"":""00:00:04.2357964"",""success"":true,""responseCode"":""200"",""url"":""http://localhost:25222/"",""httpMethod"":""GET""}}}
{""name"":""Microsoft.ApplicationInsights.00000000000000000000000000000000.Request"",""time"":""2016-08-24T03:37:37.4214402Z"",""iKey"":""00000000-0000-0000-0000-000000000000"",""tags"":{""ai.device.roleInstance"":""gongdo-pc"",""ai.internal.sdkVersion"":""aspnet5c:1.0.0"",""ai.session.id"":""XMiur"",""ai.user.id"":""YVWRF"",""ai.operation.name"":""GET Home/Index"",""ai.operation.id"":""yF4r33nLmlM="",""ai.user.userAgent"":""Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36""},""data"":{""baseType"":""RequestData"",""baseData"":{""ver"":2,""id"":""yF4r33nLmlM="",""name"":""GET Home/Index"",""startTime"":""2016-08-24T03:37:37.4214402+00:00"",""duration"":""00:00:00.0416279"",""success"":true,""responseCode"":""200"",""url"":""http://localhost:25222/"",""httpMethod"":""GET""}}}";

        private static readonly Dictionary<string, IEnumerable<string>> SampleHeaders
            = new Dictionary<string, IEnumerable<string>>
			{
				{ "Connection", new string[] { "Keep-Alive" } },
                { "Content-Length", new string[] { "514" } },
				{ "Content-Type", new string[] { "application/x-json-stream" } },
                { "Content-Encoding", new string[] { "gzip" } },
                { "Accept-Encoding", new string[] { "gzip, deflate" } },
                { "Host", new string[] { "localhost:25222" } },
				{ "MS-ASPNETCORE-TOKEN", new string[] { "00000000-0000-0000-0000-000f7633c4b1" } },
                { "X-Original-Proto", new string[] { "http" } },
                { "X-Original-For", new string[] { "127.0.0.1:5746" } },
            };

        private class InvalidTelemetryProxyOptions : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new[] { new TelemetryProxyOptions { DestinationUri = null } };
                yield return new[] { new TelemetryProxyOptions { DestinationUri = new Uri("relativeUri", UriKind.Relative) } };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class NullProcessAsyncParameters : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { (Stream)null, (HttpRequestHeaders)null };
                yield return new object[] { new MemoryStream(), (HttpRequestHeaders)null };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
