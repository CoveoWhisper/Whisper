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
using WhisperAPI.Services.MLAPI.LastClickAnalytics;
using WhisperAPI.Tests.Data.Builders;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class LastClickAnalyticsTest
    {
        private const string BaseAddress = "http://localhost:5000";

        private ILastClickAnalytics _lastClickAnalytics;

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
            var lastClickAnalyticsResults = new List<LastClickAnalyticsResult>
            {
                LastClickAnalyticsResultsBuilder.Build.Instance
            };

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._lastClickAnalytics = new LastClickAnalytics(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(lastClickAnalyticsResults, HttpStatusCode.OK);

            var result = this._lastClickAnalytics.GetLastClickAnalyticsResults(new HashSet<string>()).Result;

            result.Should().BeEquivalentTo(lastClickAnalyticsResults);
        }

        [Test]
        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.NotFound)]
        public void When_receive_not_ok_response_from_post_then_throws_exception(HttpStatusCode status)
        {
            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._lastClickAnalytics = new LastClickAnalytics(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(null, status);

            try
            {
                var result = this._lastClickAnalytics.GetLastClickAnalyticsResults(new HashSet<string>()).Result;
                Assert.Fail();
            }
            catch (Exception)
            {
            }
        }

        [Test]
        public void When_receive_ok_response_with_empty_content_from_post_then_returns_null()
        {
            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._lastClickAnalytics = new LastClickAnalytics(this._httpClient, BaseAddress);

            this.HttpMessageHandlerMock(null, HttpStatusCode.OK);

            var result = this._lastClickAnalytics.GetLastClickAnalyticsResults(new HashSet<string>()).Result;
            result.Should().BeEquivalentTo((List<LastClickAnalyticsResult>)null);
        }

        private void HttpMessageHandlerMock(List<LastClickAnalyticsResult> lastClickAnalyticsResults, HttpStatusCode httpStatusCode)
        {
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = httpStatusCode,
                    Content = new StringContent(JsonConvert.SerializeObject(lastClickAnalyticsResults))
                }));
        }
    }
}
