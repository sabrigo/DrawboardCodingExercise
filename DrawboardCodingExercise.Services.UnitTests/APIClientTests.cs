using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using NSubstitute;

using Serilog.Core;

using Shouldly;

using Xunit;

namespace DrawboardCodingExercise.Services.UnitTests
{
    // These spin up a real loopback HTTP listener rather than mocking HttpClient, because
    // APIClient owns a private static HttpClient with no seam to inject a fake handler.
    public class APIClientTests
    {
        private static APIClient CreateSubject(string serverAddress)
        {
            var settings = Substitute.For<IAPISettings>();
            settings.ServerAddress.Returns(serverAddress);

            return new APIClient(settings, new JsonSerializerSettings(), Logger.None);
        }

        [Fact]
        public async Task GetAsync_ReturnsTheDeserializedResponse()
        {
            using var server = new FakeHttpServer();
            var responseTask = CreateSubject(server.BaseAddress).GetAsync<TestPayload>("api/thing");

            await server.RespondOnceAsync(HttpStatusCode.OK, "{\"Name\":\"value\"}");

            (await responseTask).Name.ShouldBe("value");
        }

        [Fact]
        public async Task GetAsync_JoinsThePathToTheServerAddressWithASingleSlash()
        {
            using var server = new FakeHttpServer();
            var responseTask = CreateSubject(server.BaseAddress + "/").GetAsync<TestPayload>("/api/thing");

            await server.RespondOnceAsync(HttpStatusCode.OK, "{}");
            await responseTask;

            server.LastRequest.Url.AbsolutePath.ShouldBe("/api/thing");
        }

        [Fact]
        public async Task GetAsync_SendsACorrelationIdHeader()
        {
            using var server = new FakeHttpServer();
            var responseTask = CreateSubject(server.BaseAddress).GetAsync<TestPayload>("api/thing");

            await server.RespondOnceAsync(HttpStatusCode.OK, "{}");
            await responseTask;

            Guid.TryParse(server.LastRequest.Headers["X-Correlation-Id"], out _).ShouldBeTrue();
        }

        [Fact]
        public async Task PostAsync_SendsTheRequestAsJsonAndReturnsTheDeserializedResponse()
        {
            using var server = new FakeHttpServer();
            var responseTask = CreateSubject(server.BaseAddress)
                .PostAsync<TestPayload, TestPayload>("api/thing", new TestPayload { Name = "input" });

            await server.RespondOnceAsync(HttpStatusCode.OK, "{\"Name\":\"output\"}");
            var response = await responseTask;

            server.LastRequest.HttpMethod.ShouldBe("POST");
            server.LastRequest.ContentType.ShouldStartWith("application/json");
            server.LastRequestBody.ShouldBe("{\"Name\":\"input\"}");
            response.Name.ShouldBe("output");
        }

        [Fact]
        public async Task GetImageAsync_ReturnsTheResponseBodyAsAStream()
        {
            using var server = new FakeHttpServer();
            var responseTask = CreateSubject(server.BaseAddress).GetImageAsync("api/image");

            await server.RespondOnceAsync(HttpStatusCode.OK, "image-bytes", "application/octet-stream");
            using var stream = await responseTask;

            using var reader = new StreamReader(stream);
            (await reader.ReadToEndAsync()).ShouldBe("image-bytes");
        }

        [Fact]
        public async Task GetAsync_ThrowsHttpStatusExceptionWithTheStatusCode_WhenTheServerRespondsWithAnError()
        {
            using var server = new FakeHttpServer();
            var responseTask = CreateSubject(server.BaseAddress).GetAsync<TestPayload>("api/thing");

            await server.RespondOnceAsync(HttpStatusCode.NotFound);

            var exception = await Should.ThrowAsync<HttpStatusException>(() => responseTask);
            exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        public class TestPayload
        {
            public string Name { get; set; }
        }

        // ponytail: real loopback listener instead of an HttpClient-mocking library; APIClient's HttpClient
        // is a private static field with no injection seam, so there's nothing else to mock against.
        private sealed class FakeHttpServer : IDisposable
        {
            private readonly HttpListener _listener = new();

            public FakeHttpServer()
            {
                BaseAddress = $"http://localhost:{GetFreePort()}";
                _listener.Prefixes.Add(BaseAddress + "/");
                _listener.Start();
            }

            public string BaseAddress { get; }
            public HttpListenerRequest LastRequest { get; private set; }
            public string LastRequestBody { get; private set; }

            public async Task RespondOnceAsync(HttpStatusCode statusCode, string body = null, string contentType = "application/json")
            {
                var context = await _listener.GetContextAsync();
                LastRequest = context.Request;
                using (var reader = new StreamReader(context.Request.InputStream))
                {
                    LastRequestBody = await reader.ReadToEndAsync();
                }

                context.Response.StatusCode = (int)statusCode;
                var bytes = Encoding.UTF8.GetBytes(body ?? string.Empty);
                context.Response.ContentType = contentType;
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                context.Response.OutputStream.Close();
            }

            private static int GetFreePort()
            {
                var tcpListener = new TcpListener(IPAddress.Loopback, 0);
                tcpListener.Start();
                var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
                tcpListener.Stop();
                return port;
            }

            public void Dispose()
            {
                _listener.Stop();
                _listener.Close();
            }
        }
    }
}
