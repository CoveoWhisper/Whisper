using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;
using WhisperAPI.Services.NLPAPI;
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
                IntentBuilder.Build.WithName("Need Help").Instance
            };

            var analysis = NlpAnalysisBuilder.Build.WithIntents(intents).Instance;
            const string baseAddress = "http://localhost:5000";

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nlpCall = new NlpCall(this._httpClient, this.GetIrrelevantIntents(), baseAddress);

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(analysis))
                }));

            var searchQuery = SearchQueryBuilder.Build.WithQuery(sentence).Instance;
            var nlpAnalysis = this._nlpCall.AnalyseSearchQuery(searchQuery);

            searchQuery.Relevant.Should().BeTrue();
            nlpAnalysis.ParsedQuery.Should().BeEquivalentTo(nlpAnalysis.ParsedQuery);
        }

        [Test]
        [TestCase("Test")]
        public void When_receive_not_found_response_from_post_then_throws_exception(string sentence)
        {
            const string baseAddress = "http://localhost:5000";

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nlpCall = new NlpCall(this._httpClient, this.GetIrrelevantIntents(), baseAddress);

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Content = new StringContent(string.Empty)
                }));

            var searchQuery = SearchQueryBuilder.Build.WithQuery(sentence).Instance;
            Assert.Throws<HttpRequestException>(() => this._nlpCall.AnalyseSearchQuery(searchQuery));
        }

        [Test]
        [TestCase("Test")]
        public void When_receive_InternalServerError_response_from_post_then_throws_exception(string sentence)
        {
            const string baseAddress = "http://localhost:5000";

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nlpCall = new NlpCall(this._httpClient, this.GetIrrelevantIntents(), baseAddress);

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.InternalServerError,
                    Content = new StringContent(string.Empty)
                }));

            var searchQuery = SearchQueryBuilder.Build.WithQuery(sentence).Instance;
            Assert.Throws<HttpRequestException>(() => this._nlpCall.AnalyseSearchQuery(searchQuery));
        }

        [Test]
        [TestCase("Test")]
        public void When_receive_ok_response_with_empty_content_from_post_then_returns_null(string sentence)
        {
            const string baseAddress = "http://localhost:5000";

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nlpCall = new NlpCall(this._httpClient, this.GetIrrelevantIntents(), baseAddress);

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(string.Empty))
                }));

            var searchQuery = SearchQueryBuilder.Build.WithQuery(sentence).Instance;
            var nlpAnalysis = this._nlpCall.AnalyseSearchQuery(searchQuery);
            nlpAnalysis.Should().Be(null);
        }

        [Test]
        [TestCase("*#")]
        [TestCase("I*")]
        [TestCase("*like*")]
        [TestCase("I*i*C*")]
        [TestCase("I like C#")]
        public void When_Intent_With_and_without_Wildcard_is_irrelevant(string wildcardString)
        {
            var searchQuery = SearchQueryBuilder.Build.WithQuery("I like C#").Instance;

            var intentsFromNLP = new List<Intent>
            {
                IntentBuilder.Build.WithName("I like C#").Instance
            };

            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intentsFromNLP).Instance;
            const string baseAddress = "http://localhost:5000";

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nlpCall = new NlpCall(this._httpClient, new List<string> { wildcardString }, baseAddress);

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(nlpAnalysis))
                }));

            this._nlpCall.AnalyseSearchQuery(searchQuery);
            searchQuery.Relevant.Should().BeFalse();
        }

        [Test]
        [TestCase("*#")]
        [TestCase("I*")]
        [TestCase("*like*")]
        [TestCase("I*i*C*")]
        [TestCase("I like C#")]
        public void When_Intent_is_relevant(string wildcardString)
        {
            var searchQuery = SearchQueryBuilder.Build.WithQuery("I like C#").Instance;

            var intentsFromNLP = new List<Intent>
            {
                IntentBuilder.Build.WithName("You prefer JAVA").Instance
            };

            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intentsFromNLP).Instance;
            const string baseAddress = "http://localhost:5000";

            this._httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            this._nlpCall = new NlpCall(this._httpClient, new List<string> { wildcardString }, baseAddress);

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(nlpAnalysis))
                }));

            this._nlpCall.AnalyseSearchQuery(searchQuery);
            searchQuery.Relevant.Should().BeTrue();
        }

        private List<string> GetIrrelevantIntents()
        {
            return new List<string>
            {
                "Greetings"
            };
        }
    }
}