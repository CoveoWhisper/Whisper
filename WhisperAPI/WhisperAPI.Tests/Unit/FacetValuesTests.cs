using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using WhisperAPI.Services.MLAPI.Facets;
using WhisperAPI.Tests.Data.Builders;
using FacetValues = WhisperAPI.Services.MLAPI.Facets.FacetValues;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class FacetValuesTests
    {
        private const string BaseAddress = "http://localhost:5000";

        private IFacetValues _facetValues;

        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;

        [SetUp]
        public void Setup()
        {
            this._httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        }

        [Test]
        public void When_receive_ok_response_from_post_then_returns_result_correctly()
        {
            var values = new List<Models.MLAPI.FacetValues>
            {
                FacetValuesBuilder.Build.Instance
            };

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._facetValues = new FacetValues(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(values, HttpStatusCode.OK);

            var result = this._facetValues.GetFacetValues(new List<string>());

            result.Should().BeEquivalentTo(values);
        }

        [Test]
        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.NotFound)]
        public void When_receive_not_ok_response_from_post_then_throws_exception(HttpStatusCode status)
        {
            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._facetValues = new FacetValues(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(null, status);

            Assert.Throws<HttpRequestException>(() => this._facetValues.GetFacetValues(new List<string>()));
        }

        [Test]
        public void When_receive_ok_response_with_empty_content_from_post_then_returns_null()
        {
            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._facetValues = new FacetValues(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(null, HttpStatusCode.OK);

            var result = this._facetValues.GetFacetValues(new List<string>());
            result.Should().BeEquivalentTo((List<FacetValues>)null);
        }

        private void HttpMessageHandlerMock(List<Models.MLAPI.FacetValues> facetValues, HttpStatusCode httpStatusCode)
        {
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = httpStatusCode,
                    Content = new StringContent(JsonConvert.SerializeObject(facetValues))
                }));
        }
    }
}
