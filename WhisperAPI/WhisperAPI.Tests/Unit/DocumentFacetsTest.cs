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
using WhisperAPI.Models;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Services.MLAPI.Facets;
using WhisperAPI.Tests.Data.Builders;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class DocumentFacetsTest
    {
        private const string BaseAddress = "http://localhost:5000";

        private IDocumentFacets _documentFacets;

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
            var questions = new List<FacetQuestionResult>
            {
                FacetQuestionResultBuilder.Build.Instance
            };

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._documentFacets = new DocumentFacets(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(questions, HttpStatusCode.OK);

            var result = this._documentFacets.GetQuestions(new List<string>());

            result.Should().BeEquivalentTo(questions);
        }

        [Test]
        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.NotFound)]
        public void When_receive_not_ok_response_from_post_then_throws_exception(HttpStatusCode status)
        {
            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._documentFacets = new DocumentFacets(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(null, status);

            Assert.Throws<HttpRequestException>(() => this._documentFacets.GetQuestions(new List<string>()));
        }

        [Test]
        public void When_receive_ok_response_with_empty_content_from_post_then_returns_null()
        {
            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._documentFacets = new DocumentFacets(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(null, HttpStatusCode.OK);

            var result = this._documentFacets.GetQuestions(new List<string>());
            result.Should().BeEquivalentTo((List<Question>)null);
        }

        private void HttpMessageHandlerMock(List<FacetQuestionResult> questions, HttpStatusCode httpStatusCode)
        {
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = httpStatusCode,
                    Content = new StringContent(JsonConvert.SerializeObject(questions))
                }));
        }
    }
}
