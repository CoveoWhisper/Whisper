using System;
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
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Services.MLAPI.NearestDocuments;
using WhisperAPI.Tests.Data.Builders;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class NearestDocumentsTest
    {
        private const string BaseAddress = "http://localhost:5000";

        private INearestDocuments _nearestDocuments;

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
            var nearestDocumentsResults = new List<NearestDocumentsResult>
            {
                NearestDocumentsResultBuilder.Build.Instance
            };

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nearestDocuments = new NearestDocuments(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(nearestDocumentsResults, HttpStatusCode.OK);

            var entities = new HashSet<string>
            {
                "Entity1",
                "Entity2"
            };

            var result = this._nearestDocuments.GetNearestDocumentsResults(entities).Result;

            result.Should().BeEquivalentTo(nearestDocumentsResults);
        }

        [Test]
        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.NotFound)]
        public void When_receive_not_ok_response_from_post_then_throws_exception(HttpStatusCode status)
        {
            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nearestDocuments = new NearestDocuments(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(null, status);

            var entities = new HashSet<string>
            {
                "Entity1",
                "Entity2"
            };
            Assert.Throws<AggregateException>(() => this._nearestDocuments.GetNearestDocumentsResults(entities).Wait());
        }

        [Test]
        public void When_receive_ok_response_with_empty_content_from_post_then_returns_null()
        {
            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nearestDocuments = new NearestDocuments(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(null, HttpStatusCode.OK);

            var entities = new HashSet<string>
            {
                "Entity1",
                "Entity2"
            };
            var result = this._nearestDocuments.GetNearestDocumentsResults(entities).Result;
            result.Should().BeEquivalentTo((HashSet<string>)null);
        }

        private void HttpMessageHandlerMock(List<NearestDocumentsResult> nearestDocumentsResults, HttpStatusCode httpStatusCode)
        {
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = httpStatusCode,
                    Content = new StringContent(JsonConvert.SerializeObject(nearestDocumentsResults))
                }));
        }
    }
}
