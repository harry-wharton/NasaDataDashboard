using Moq;
using Moq.Protected;
using NasaDataDashboard.Data;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NasaDataDashboard.Tests.ServiceTests
{
    [TestFixture]
    public class NasaApiServiceTests
    {

        /* This test is testing that we actually get a valid response, we don't
         * really need to test that the full json response is returned because I can't
         * imagine they change the response object much, hence why we just have an
         * empty array in the fake.
        */

        private NasaApiService _service;

        [SetUp]
        public void Setup()
        {
            // fakes one neo with no data about it
            var fakeJson = "{\"near_earth_objects\": {\"2021-09-07\": []}}";

            var handlerMock = new Mock<HttpMessageHandler>();

            // Sets up the mock handler we just created
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeJson, Encoding.UTF8, "application/json")
                });

            // Use the mock just set up to create the client
            var httpClient = new HttpClient(handlerMock.Object);

            _service = new NasaApiService(httpClient);
        }

        [Test]
        public async Task TheApiResponseIsNotNull()
        {
            var res = await _service.GetNeoAsync();
            Assert.That(res, Is.Not.Null, "Returned null, JsonDocument expected");
        }
    }
}
