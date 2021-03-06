using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GlobalExceptionHandler.Tests.Exceptions;
using GlobalExceptionHandler.Tests.Fixtures;
using GlobalExceptionHandler.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace GlobalExceptionHandler.Tests.WebApi.StatusCodeTests
{
    public class StatusCodeAtRunTimeWithInt : IClassFixture<WebApiServerFixture>, IAsyncLifetime
    {
        private const string ApiProductNotFound = "/api/productnotfound";
        private readonly HttpClient _client;
        private HttpResponseMessage _response;

        public StatusCodeAtRunTimeWithInt(WebApiServerFixture fixture)
        {
            // Arrange
            var webHost = fixture.CreateWebHostWithMvc();
            webHost.Configure(app =>
            {
                app.UseGlobalExceptionHandler(x =>
                {
                    x.ContentType = "application/json";
                    x.Map<HttpNotFoundException>().ToStatusCode(ex => ex.StatusCodeInt);
                    x.ResponseBody(c => JsonConvert.SerializeObject(new TestResponse
                    {
                        Message = c.Message
                    }));
                });

                app.Map(ApiProductNotFound, config => { config.Run(context => throw new HttpNotFoundException("Record not found")); });
            });

            _client = new TestServer(webHost).CreateClient();
        }
        
        public async Task InitializeAsync()
        {
            _response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), ApiProductNotFound));
        }

        [Fact]
        public void Returns_correct_response_type()
        {
            _response.Content.Headers.ContentType.MediaType.ShouldBe("application/json");
        }

        [Fact]
        public async Task Returns_correct_status_code()
        {
            var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), ApiProductNotFound));
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        public Task DisposeAsync()
            => Task.CompletedTask;
    }
}