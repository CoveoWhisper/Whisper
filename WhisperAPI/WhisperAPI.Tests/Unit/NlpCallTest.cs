﻿using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Services;
using WhisperAPI.Tests.Data.Builders;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class NlpCallTest
    {
        private INlpCall _nlpCall;

        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;

        [SetUp]
        public void Setup()
        {
            this._httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        }

        [Test]
        [TestCase("Test")]
        public void When_receive_ok_response_from_post_then_returns_result_correctly(string sentence)
        {
            var intents = new List<Intent>
            {
                new IntentBuilder().WithName("Need Help").Build()
            };

            var nlpAnalysis = new NlpAnalysisBuilder().WithIntents(intents).Build();
            const string baseAdress = "http://localhost:5000";

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nlpCall = new NlpCall(this._httpClient, baseAdress);

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(nlpAnalysis))
                }));

            var result = this._nlpCall.GetNlpAnalyses(sentence);

            result.Intents.Should().BeEquivalentTo(nlpAnalysis.Intents);
            result.Entities.Should().BeEquivalentTo(nlpAnalysis.Entities);
        }

        [Test]
        [TestCase("Test")]
        public void When_receive_not_found_response_from_post_then_throws_exception(string sentence)
        {
            const string baseAdress = "http://localhost:5000";

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nlpCall = new NlpCall(this._httpClient, baseAdress);

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Content = new StringContent(string.Empty)
                }));

            Assert.Throws<HttpRequestException>(() => this._nlpCall.GetNlpAnalyses(sentence));
        }

        [Test]
        [TestCase("Test")]
        public void When_receive_InternalServerError_response_from_post_then_throws_exception(string sentence)
        {
            const string baseAdress = "http://localhost:5000";

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nlpCall = new NlpCall(this._httpClient, baseAdress);

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.InternalServerError,
                    Content = new StringContent(string.Empty)
                }));

            Assert.Throws<HttpRequestException>(() => this._nlpCall.GetNlpAnalyses(sentence));
        }

        [Test]
        [TestCase("Test")]
        public void When_receive_ok_response_with_empty_content_from_post_then_returns_null(string sentence)
        {
            const string baseAdress = "http://localhost:5000";

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nlpCall = new NlpCall(this._httpClient, baseAdress);

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(string.Empty))
                }));

            var result = this._nlpCall.GetNlpAnalyses(sentence);
            result.Should().BeEquivalentTo((NlpAnalysis)null);
        }
    }
}