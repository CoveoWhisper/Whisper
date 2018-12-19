using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using WhisperAPI.Controllers;
using WhisperAPI.Models;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;
using WhisperAPI.Services.Context;
using WhisperAPI.Services.MLAPI.Facets;
using WhisperAPI.Services.MLAPI.LastClickAnalytics;
using WhisperAPI.Services.MLAPI.NearestDocuments;
using WhisperAPI.Services.NLPAPI;
using WhisperAPI.Services.Questions;
using WhisperAPI.Services.Search;
using WhisperAPI.Services.Suggestions;
using WhisperAPI.Settings;
using WhisperAPI.Tests.Data.Builders;

namespace WhisperAPI.Tests.Integration
{
    [TestFixture]
    public class SuggestionsControllerTest
    {
        private readonly int _numberOfResults = 1000;
        private SuggestionsController _suggestionController;
        private RecommenderSettings _recommenderSettings;

        private Mock<HttpMessageHandler> _indexSearchHttpMessageHandleMock;
        private Mock<HttpMessageHandler> _lastClickAnalyticsHttpMessageHandleMock;
        private Mock<HttpMessageHandler> _nearestDocumentsHttpMessageHandleMock;
        private Mock<HttpMessageHandler> _nlpCallHttpMessageHandleMock;
        private Mock<HttpMessageHandler> _documentFacetsHttpMessageHandleMock;
        private Mock<HttpMessageHandler> _filterDocumentsHttpMessageHandleMock;

        [SetUp]
        public void SetUp()
        {
            this._indexSearchHttpMessageHandleMock = new Mock<HttpMessageHandler>();
            this._lastClickAnalyticsHttpMessageHandleMock = new Mock<HttpMessageHandler>();
            this._nearestDocumentsHttpMessageHandleMock = new Mock<HttpMessageHandler>();
            this._nlpCallHttpMessageHandleMock = new Mock<HttpMessageHandler>();
            this._documentFacetsHttpMessageHandleMock = new Mock<HttpMessageHandler>();
            this._filterDocumentsHttpMessageHandleMock = new Mock<HttpMessageHandler>();

            var indexSearchHttpClient = new HttpClient(this._indexSearchHttpMessageHandleMock.Object);
            var lastClickAnalyticsHttpClient = new HttpClient(this._lastClickAnalyticsHttpMessageHandleMock.Object);
            var nearestDocumentsHttpClient = new HttpClient(this._nearestDocumentsHttpMessageHandleMock.Object);
            var nlpCallHttpClient = new HttpClient(this._nlpCallHttpMessageHandleMock.Object);
            var documentFacetHttpClient = new HttpClient(this._documentFacetsHttpMessageHandleMock.Object);
            var filterDocumentsHttpClient = new HttpClient(this._filterDocumentsHttpMessageHandleMock.Object);

            var indexSearch = new IndexSearch(null, this._numberOfResults, indexSearchHttpClient, "https://localhost:5000", null);
            var lastClickAnalytics = new LastClickAnalytics(lastClickAnalyticsHttpClient, "https://localhost:5000");
            var nearestDocuments = new NearestDocuments(nearestDocumentsHttpClient, "https://localhost:5000");
            var nlpCall = new NlpCall(nlpCallHttpClient, this.GetIrrelevantIntents(), "https://localhost:5000");
            var documentFacets = new DocumentFacets(documentFacetHttpClient, "https://localhost:5000");
            var filterDocuments = new FilterDocuments(filterDocumentsHttpClient, "https://localhost:5000");

            this._recommenderSettings = new RecommenderSettings
            {
                UseAnalyticsSearchRecommender = false,
                UseFacetQuestionRecommender = true,
                UseLongQuerySearchRecommender = true,
                UsePreprocessedQuerySearchRecommender = false,
                UseNearestDocumentsRecommender = false
            };

            var suggestionsService = new SuggestionsService(indexSearch, lastClickAnalytics, documentFacets, nearestDocuments, filterDocuments, 7, 0.5, this._recommenderSettings);

            var contexts = new InMemoryContexts(new TimeSpan(1, 0, 0, 0));
            var questionsService = new QuestionsService();
            this._suggestionController = new SuggestionsController(suggestionsService, questionsService, nlpCall, contexts);
        }

        [Test]
        public void When_getting_suggestions_with_good_query_then_returns_suggestions_correctly()
        {
            var questions = GetQuestions();
            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis())));
            this.IndexSearchHttpMessageHandleMock(HttpStatusCode.OK, this.GetSearchResultStringContent());
            this.DocumentFacetsHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(questions)));
            var searchQuery = SearchQueryBuilder.Build.Instance;

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var result = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            var questionsToClient = questions.Select(q => QuestionToClient.FromQuestion(q.FacetQuestion)).ToList();

            suggestion.Documents.Select(d => d.Value).Should().BeEquivalentTo(GetSuggestedDocuments());
            suggestion.Questions.Select(q => q.Value).Should().BeEquivalentTo(questionsToClient);
        }

        [Test]
        public void When_getting_suggestions_with_preprocessed_then_returns_suggestions_correctly()
        {
            var questions = GetQuestions();
            this._recommenderSettings.UsePreprocessedQuerySearchRecommender = true;

            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis())));
            this.IndexSearchHttpMessageHandleMock(HttpStatusCode.OK, this.GetSearchResultStringContent());
            this.DocumentFacetsHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(questions)));
            var searchQuery = SearchQueryBuilder.Build.Instance;

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var result = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            var questionsToClient = questions.Select(q => QuestionToClient.FromQuestion(q.FacetQuestion)).ToList();

            suggestion.Documents.Select(d => d.Value).Should().BeEquivalentTo(GetSuggestedDocuments());
            suggestion.Questions.Select(q => q.Value).Should().BeEquivalentTo(questionsToClient);

            suggestion.Documents.All(x => x.RecommendedBy.Count == 2).Should().BeTrue();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void When_getting_suggestions_with_nearestDocument_then_returns_suggestions_correctly(bool usePreprocessedQuerySearchRecommender)
        {
            var questions = GetQuestions();
            this._recommenderSettings.UseNearestDocumentsRecommender = true;
            this._recommenderSettings.UsePreprocessedQuerySearchRecommender = usePreprocessedQuerySearchRecommender;

            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis())));
            this.IndexSearchHttpMessageHandleMock(HttpStatusCode.OK, this.GetSearchResultStringContent());
            this.NearestDocumentsHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(GetNearestDocumentResult())));
            this.DocumentFacetsHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(questions)));
            var searchQuery = SearchQueryBuilder.Build.Instance;

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var result = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            var questionsToClient = questions.Select(q => QuestionToClient.FromQuestion(q.FacetQuestion)).ToList();

            suggestion.Documents.Select(d => d.Value).Count().Should().Be(4);
            suggestion.Documents.Select(d => d.Value.Uri).Contains(GetNearestDocumentResult()[0].Document.Uri).Should().BeTrue();
            suggestion.Documents.Select(d => d.Value.Uri).Contains(GetNearestDocumentResult()[1].Document.Uri).Should().BeTrue();
            suggestion.Questions.Select(q => q.Value).Should().BeEquivalentTo(questionsToClient);
        }

        [Test]
        public void When_getting_suggestions_with_null_query_then_returns_bad_request()
        {
            var searchQuery = SearchQueryBuilder.Build.WithQuery(null).Instance;
            var actionContext = this.GetActionExecutingContext(searchQuery);
            this._suggestionController.OnActionExecuting(actionContext);
            actionContext.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public void When_getting_suggestions_with_irrelevant_query_then_returns_empty_list()
        {
            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(this.GetIrrelevantNlpAnalysis())));
            this.DocumentFacetsHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(new List<string>())));
            var searchQuery = SearchQueryBuilder.Build.Instance;
            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var result = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.Documents.Should().BeEquivalentTo(new List<Document>());
        }

        [Test]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.InternalServerError)]
        public void When_getting_suggestions_but_nlp_returns_Error_then_throws_exception(System.Net.HttpStatusCode httpStatusCode)
        {
            var searchQuery = SearchQueryBuilder.Build
                .WithQuery("Hello")
                .Instance;
            this.NlpCallHttpMessageHandleMock(httpStatusCode, new StringContent(string.Empty));
            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            Assert.Throws<HttpRequestException>(() => this._suggestionController.GetSuggestions(searchQuery));
        }

        [Test]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.InternalServerError)]
        public void When_getting_suggestions_but_coveo_returns_Error_then_throws_exception(System.Net.HttpStatusCode httpStatusCode)
        {
            var searchQuery = SearchQueryBuilder.Build
                .WithQuery("I need help with CoveoSearch API")
                .Instance;

            this.NlpCallHttpMessageHandleMock(HttpStatusCode.InternalServerError, new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis())));
            this.IndexSearchHttpMessageHandleMock(httpStatusCode, this.GetSearchResultStringContent());

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            Assert.Throws<HttpRequestException>(() => this._suggestionController.GetSuggestions(searchQuery));
        }

        [Test]
        public void When_getting_suggestions_with_agent_sending_uri_then_suggestion_is_filter_then_returns_suggestions_correctly()
        {
            // Customer says: Hello
            var searchQuery = SearchQueryBuilder.Build
                .WithQuery("Hello")
                .Instance;
            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(this.GetIrrelevantNlpAnalysis())));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var result = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.Documents.Should().BeEquivalentTo(new List<Recommendation<Document>>());
            suggestion.Questions.Should().BeEquivalentTo(new List<Recommendation<Question>>());

            // Customer says: I need help with CoveoSearch API
            searchQuery = SearchQueryBuilder.Build
                .WithQuery("I need help with CoveoSearch API")
                .WithChatKey(searchQuery.ChatKey)
                .Instance;

            var questions = GetQuestions();
            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis())));

            this.IndexSearchHttpMessageHandleMock(HttpStatusCode.OK, this.GetSearchResultStringContent());

            this.DocumentFacetsHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(questions)));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            result = this._suggestionController.GetSuggestions(searchQuery);

            suggestion = result.As<OkObjectResult>().Value as Suggestion;

            var questionsToClient = questions.Select(q => QuestionToClient.FromQuestion(q.FacetQuestion)).ToList();

            suggestion.Documents.Select(d => d.Value).Should().BeEquivalentTo(GetSuggestedDocuments());
            suggestion.Questions.Select(q => q.Value).Should().BeEquivalentTo(questionsToClient);

            // Agent says: Maybe this could help: https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm
            searchQuery = SearchQueryBuilder.Build
                .WithQuery("Maybe this can help: https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                .WithMessageType(SearchQuery.MessageType.Agent)
                .WithChatKey(searchQuery.ChatKey)
                .Instance;

            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis())));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            result = this._suggestionController.GetSuggestions(searchQuery);

            result.Should().NotBeNull();

            suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.Documents.Count.Should().NotBe(0);
            suggestion.Documents.Select(d => d.Value).Select(x => x.Uri)
                .Contains("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm").Should().BeFalse();
        }

        [Test]
        public void When_getting_suggestions_with_customer_sending_uri_then_suggestion_is_filter_then_returns_suggestions_correctly()
        {
            // Customer says: Hello
            var searchQuery = SearchQueryBuilder.Build
                .WithQuery("Hello")
                .Instance;

            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(this.GetIrrelevantNlpAnalysis())));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var result = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.Documents.Should().BeEquivalentTo(new List<Recommendation<Document>>());
            suggestion.Questions.Should().BeEquivalentTo(new List<Recommendation<Question>>());

            // Customer says: I need help with CoveoSearch API
            searchQuery = SearchQueryBuilder.Build
                .WithQuery("I need help with CoveoSearch API and I look at this: https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                .WithChatKey(searchQuery.ChatKey)
                .Instance;

            var questions = GetQuestions();

            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis())));

            this.IndexSearchHttpMessageHandleMock(HttpStatusCode.OK, this.GetSearchResultStringContent());

            this.DocumentFacetsHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(questions)));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            result = this._suggestionController.GetSuggestions(searchQuery);

            result.Should().NotBeNull();

            suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.Documents.Count.Should().NotBe(0);
            suggestion.Documents.Select(d => d.Value).Select(x => x.Uri)
                .Contains("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm").Should().BeFalse();
        }

        [Test]
        public void When_getting_suggestions_with_more_attribute_than_required_then_returns_correctly()
        {
            var jsonSearchQuery = "{\"chatkey\": \"aecaa8db-abc8-4ac9-aa8d-87987da2dbb0\",\"Query\": \"Need help with CoveoSearch API\",\"Type\": 1,\"command\": \"sudo ls\",\"maxDocuments\": \"10\",\"maxQuestions\": \"10\" }";
            var questions = GetQuestions();

            var nlpResult = this.GetRelevantNlpAnalysis();
            nlpResult.ParsedQuery = "Need help with CoveoSearch API";
            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(nlpResult)));
            this.IndexSearchHttpMessageHandleMock(HttpStatusCode.OK, this.GetSearchResultStringContent());
            this.DocumentFacetsHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(questions)));

            var deserializedSearchQuery = JsonConvert.DeserializeObject<SearchQuery>(jsonSearchQuery);
            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(deserializedSearchQuery));
            var result = this._suggestionController.GetSuggestions(deserializedSearchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.Documents.Select(d => d.Value).Should().BeEquivalentTo(GetSuggestedDocuments());
        }

        [Test]
        public void When_getting_suggestions_then_refresh_result_are_the_same()
        {
            var searchQuery = SearchQueryBuilder.Build
                .WithChatKey(new Guid("aecaa8db-abc8-4ac9-aa8d-87987da2dbb0"))
                .Instance;

            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis())));
            this.IndexSearchHttpMessageHandleMock(HttpStatusCode.OK, this.GetSearchResultStringContent());
            this.DocumentFacetsHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(GetQuestions())));
            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));

            var resultSuggestions = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = (Suggestion)resultSuggestions.As<OkObjectResult>().Value;
            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));

            var suggestionQuery = SuggestionQueryBuilder.Build
                .WithChatKey(searchQuery.ChatKey)
                .Instance;

            var resultLastSuggestions = this._suggestionController.GetSuggestions(suggestionQuery);

            var lastSuggestion = (Suggestion)resultLastSuggestions.As<OkObjectResult>().Value;
            lastSuggestion.Documents.Select(d => d.Value).Should().BeEquivalentTo(suggestion.Documents.Select(d => d.Value));
            lastSuggestion.Questions.Select(q => q.Value).Should().BeEquivalentTo(suggestion.Questions.Select(q => q.Value));
            lastSuggestion.ActiveFacets.Should().BeEquivalentTo(suggestion.ActiveFacets);
        }

        [Test]
        public void When_getting_suggestions_and_agent_click_on_question_and_agent_asks_question_to_customer_then_question_is_filter_out_then_facet_is_clear()
        {
            // Customer says: I need help with CoveoSearch API
            var searchQuery = SearchQueryBuilder.Build
                .WithQuery("I need help with CoveoSearch API")
                .Instance;

            var questions = GetQuestions();
            this.NlpCallHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis())));
            this.IndexSearchHttpMessageHandleMock(HttpStatusCode.OK, this.GetSearchResultStringContent());
            this.DocumentFacetsHttpMessageHandleMock(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(questions)));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var result = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            var questionsToClient = questions.Select(q => QuestionToClient.FromQuestion(q.FacetQuestion)).ToList();

            suggestion.Documents.Select(d => d.Value).Should().BeEquivalentTo(GetSuggestedDocuments());
            suggestion.Questions.Select(q => q.Value).Should().BeEquivalentTo(questionsToClient);

            // Agent click on a question in the UI
            var selectQuery = SelectQueryBuilder.Build.WithChatKey(searchQuery.ChatKey).WithId(questions[0].FacetQuestion.Id).Instance;
            this._suggestionController.SelectSuggestion(selectQuery);

            // Agent asks the question he clicked to the custommer
            searchQuery = SearchQueryBuilder.Build
                .WithMessageType(SearchQuery.MessageType.Agent)
            .WithQuery(questions[0].FacetQuestion.Text)
            .Instance;
            result = this._suggestionController.GetSuggestions(searchQuery);
            suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.Documents.Select(d => d.Value).Should().BeEquivalentTo(GetSuggestedDocuments());

            var questionsReceived = suggestion.Questions.Select(q => q.Value);
            questionsReceived.Count().Should().Be(1);
            questionsReceived.Single().Should().BeEquivalentTo(questionsToClient[1]);

            // Client respond to the answer
            var answerFromClient = (questions[0].FacetQuestion as FacetQuestion)?.FacetValues.FirstOrDefault();
            searchQuery.Type = SearchQuery.MessageType.Customer;
            searchQuery.Query = answerFromClient;
            result = this._suggestionController.GetSuggestions(searchQuery);
            suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.ActiveFacets.Should().HaveCount(1);
            suggestion.ActiveFacets[0].Values.Contains(answerFromClient).Should().BeTrue();

            // Agent clear the facet
            result = this._suggestionController.RemoveFacet(suggestion.ActiveFacets[0].Id, searchQuery);
            result.Should().BeOfType<NoContentResult>();

            // Get the suggestion after clear
            var query = SuggestionQueryBuilder.Build.WithChatKey(searchQuery.ChatKey).Instance;
            result = this._suggestionController.GetSuggestions(query);
            suggestion = result.As<OkObjectResult>().Value as Suggestion;
            suggestion.Should().NotBeNull();
            suggestion.ActiveFacets.Should().HaveCount(0);
            suggestion.Documents.Select(d => d.Value).Should().BeEquivalentTo(GetSuggestedDocuments());
        }

        private static List<Document> GetSuggestedDocuments()
        {
            return new List<Document>
            {
                new Document
                {
                    Title = "Available Coveo Cloud V2 Source Types",
                    Uri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                    PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                    Summary = null,
                    Excerpt = null
                },
                new Document
                {
                    Title = "Coveo Cloud Query Syntax Reference",
                    Uri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                    PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                    Summary = null,
                    Excerpt = null
                },
                new Document
                {
                    Title = "Events",
                    Uri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                    PrintableUri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                    Summary = null,
                    Excerpt = null
                },
                new Document
                {
                    Title = "Coveo Facet Component (CoveoFacet)",
                    Uri = "https://coveo.github.io/search-ui/components/facet.html",
                    PrintableUri = "https://coveo.github.io/search-ui/components/facet.html",
                    Summary = null,
                    Excerpt = null
                }
            };
        }

        private static List<FacetQuestionResult> GetQuestions()
        {
            return new List<FacetQuestionResult>
            {
                FacetQuestionResultBuilder.Build
                    .WithFacetQuestion(
                        FacetQuestionBuilder.Build
                            .WithFacetName("Dummy")
                            .WithFacetValues("A", "B", "C")
                            .Instance)
                    .Instance,
                FacetQuestionResultBuilder.Build
                    .WithFacetQuestion(
                        FacetQuestionBuilder.Build
                            .WithFacetName("Dummy")
                            .WithFacetValues("C", "D", "E")
                            .Instance)
                    .Instance
            };
        }

        private static List<NearestDocumentsResult> GetNearestDocumentResult()
        {
            return new List<NearestDocumentsResult>
            {
                NearestDocumentsResultBuilder.Build
                    .WithScore(0.98)
                    .WithDocument(DocumentBuilder.Build.WithUri("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm").Instance).Instance,
                NearestDocumentsResultBuilder.Build
                    .WithScore(0.95)
                    .WithDocument(DocumentBuilder.Build.WithUri("https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm").Instance).Instance
            };
        }

        private void IndexSearchHttpMessageHandleMock(HttpStatusCode statusCode, HttpContent content)
        {
            this._indexSearchHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = content
                }));
        }

        private void NearestDocumentsHttpMessageHandleMock(HttpStatusCode statusCode, HttpContent content)
        {
            this._nearestDocumentsHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = content
                }));
        }

        private void NlpCallHttpMessageHandleMock(HttpStatusCode statusCode, HttpContent content)
        {
            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = content
                }));
        }

        private void DocumentFacetsHttpMessageHandleMock(HttpStatusCode statusCode, HttpContent content)
        {
            this._documentFacetsHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = content
                }));
        }

        // Now unused, but kept in case we choose to use it again
        /*
        private void FilterDocumentHttpMessageHandleMock(HttpStatusCode statusCode, HttpContent content)
        {
            this._filterDocumentsHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = content
                }));
        }
        */

        private ActionExecutingContext GetActionExecutingContext(Query query)
        {
            var actionContext = new ActionContext(
                new Mock<HttpContext>().Object,
                new Mock<RouteData>().Object,
                new Mock<ActionDescriptor>().Object);

            var actionExecutingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                this._suggestionController);
            actionExecutingContext.ActionArguments["query"] = query;
            if (query != null)
            {
                var context = new ValidationContext(query, null, null);
                var results = new List<ValidationResult>();

                if (!Validator.TryValidateObject(query, context, results, true))
                {
                    actionExecutingContext.ModelState.Clear();
                    this._suggestionController.ModelState.Clear();
                    foreach (ValidationResult result in results)
                    {
                        string key = result.MemberNames.FirstOrDefault() ?? string.Empty;
                        actionExecutingContext.ModelState.AddModelError(key, result.ErrorMessage);
                        this._suggestionController.ModelState.AddModelError(key, result.ErrorMessage);
                    }
                }
            }

            return actionExecutingContext;
        }

        private NlpAnalysis GetRelevantNlpAnalysis()
        {
            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Need Help").WithConfidence(98).Instance,
                IntentBuilder.Build.WithName("Greetings").WithConfidence(2).Instance
            };

            return NlpAnalysisBuilder.Build.WithIntents(intents).Instance;
        }

        private NlpAnalysis GetIrrelevantNlpAnalysis()
        {
            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Need Help").WithConfidence(2).Instance,
                IntentBuilder.Build.WithName("Greetings").WithConfidence(98).Instance
            };

            return NlpAnalysisBuilder.Build.WithIntents(intents).Instance;
        }

        private StringContent GetSearchResultStringContent()
        {
            return new StringContent("{\"totalCount\": 4,\"results\": [{\"title\": \"Available Coveo Cloud V2 Source Types\", \"excerpt\": \"This is the excerpt\", \"clickUri\": \"https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm\",\"printableUri\": \"https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm\",\"score\": 4280, \"percentScore\": 75},{\"title\": \"Coveo Cloud Query Syntax Reference\",\"excerpt\": \"This is the excerpt\", \"clickUri\": \"https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm\",\"printableUri\": \"https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm\",\"score\": 3900, \"percentScore\": 75},{\"title\": \"Events\", \"excerpt\": \"This is the excerpt\", \"clickUri\": \"https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573\",\"printableUri\": \"https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573\",\"score\": 2947, \"percentScore\": 75},{\"title\": \"Coveo Facet Component (CoveoFacet)\", \"excerpt\": \"This is the excerpt\", \"clickUri\": \"https://coveo.github.io/search-ui/components/facet.html\",\"printableUri\": \"https://coveo.github.io/search-ui/components/facet.html\",\"score\": 2932, \"percentScore\": 75}]}");
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
