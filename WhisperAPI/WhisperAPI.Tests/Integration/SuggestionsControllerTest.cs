using System;
using System.Collections.Generic;
using System.Linq;
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
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Services;
using WhisperAPI.Tests.Data.Builders;
using BadRequestResult = Microsoft.AspNetCore.Mvc.BadRequestResult;

namespace WhisperAPI.Tests.Integration
{
    [TestFixture]
    public class SuggestionsControllerTest
    {
        private SuggestionsController _suggestionController;

        private Mock<HttpMessageHandler> _indexSearchHttpMessageHandleMock;
        private Mock<HttpMessageHandler> _nlpCallHttpMessageHandleMock;

        [SetUp]
        public void SetUp()
        {
            this._indexSearchHttpMessageHandleMock = new Mock<HttpMessageHandler>();
            this._nlpCallHttpMessageHandleMock = new Mock<HttpMessageHandler>();

            var indexSearchHttpClient = new HttpClient(this._indexSearchHttpMessageHandleMock.Object);
            var nlpCallHttpClient = new HttpClient(this._nlpCallHttpMessageHandleMock.Object);

            var indexSearch = new IndexSearch(null, indexSearchHttpClient);
            var nlpCall = new NlpCall(nlpCallHttpClient, "https://localhost:5000");

            var suggestionsService = new SuggestionsService(indexSearch, nlpCall, this.GetIrrelevantIntents());

            var contexts = new InMemoryContexts(new TimeSpan(1, 0, 0, 0));
            this._suggestionController = new SuggestionsController(suggestionsService, contexts);
        }

        [Test]
        public void When_getting_suggestions_with_good_query_then_returns_suggestions_correctly()
        {
            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis()))
                }));

            this._indexSearchHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = this.GetSearchResultStringContent()
                }));

            var searchQuery = new SearchQueryBuilder().Build();

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var result = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.SuggestedDocuments.Should().BeEquivalentTo(this.GetSuggestedDocuments());
        }

        [Test]
        public void When_getting_suggestions_with_null_query_then_returns_bad_request()
        {
            var searchQuery = new SearchQueryBuilder().WithQuery(null).Build();

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var suggestions = this._suggestionController.GetSuggestions(searchQuery);

            suggestions.Should().BeEquivalentTo(new BadRequestResult());
        }

        [Test]
        public void When_getting_suggestions_with_irrelevant_query_then_returns_empty_list()
        {
            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(this.GetIrrelevantNlpAnalysis()))
                }));

            var searchQuery = new SearchQueryBuilder().Build();
            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var result = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.SuggestedDocuments.Should().BeEquivalentTo(new List<SuggestedDocument>());
        }

        [Test]
        [TestCase(System.Net.HttpStatusCode.NotFound)]
        [TestCase(System.Net.HttpStatusCode.BadRequest)]
        [TestCase(System.Net.HttpStatusCode.InternalServerError)]
        public void When_getting_suggestions_but_nlp_returns_Error_then_throws_exception(System.Net.HttpStatusCode httpStatusCode)
        {
            var searchQuery = new SearchQueryBuilder()
                .WithQuery("Hello")
                .Build();

            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = httpStatusCode,
                    Content = new StringContent(string.Empty)
                }));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            Assert.Throws<HttpRequestException>(() => this._suggestionController.GetSuggestions(searchQuery));
        }

        [Test]
        [TestCase(System.Net.HttpStatusCode.NotFound)]
        [TestCase(System.Net.HttpStatusCode.BadRequest)]
        [TestCase(System.Net.HttpStatusCode.InternalServerError)]
        public void When_getting_suggestions_but_coveo_returns_Error_then_throws_exception(System.Net.HttpStatusCode httpStatusCode)
        {
            var searchQuery = new SearchQueryBuilder()
                .WithQuery("I need help with CoveoSearch API")
                .Build();

            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.InternalServerError,
                    Content = new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis()))
                }));

            this._indexSearchHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = httpStatusCode,
                    Content = this.GetSearchResultStringContent()
                }));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            Assert.Throws<HttpRequestException>(() => this._suggestionController.GetSuggestions(searchQuery));
        }

        [Test]
        public void When_getting_suggestions_with_agent_sending_uri_then_suggestion_is_filter_then_returns_suggestions_correctly()
        {
            // Customer says: Hello
            var searchQuery = new SearchQueryBuilder()
                .WithQuery("Hello")
                .Build();

            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(this.GetIrrelevantNlpAnalysis()))
                }));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var result = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.SuggestedDocuments.Should().BeEquivalentTo(new List<SuggestedDocument>());

            // Customer says: I need help with CoveoSearch API
            searchQuery = new SearchQueryBuilder()
                .WithQuery("I need help with CoveoSearch API")
                .WithChatKey(searchQuery.ChatKey)
                .Build();

            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis()))
                }));

            this._indexSearchHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = this.GetSearchResultStringContent()
                }));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            result = this._suggestionController.GetSuggestions(searchQuery);

            suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.SuggestedDocuments.Should().BeEquivalentTo(this.GetSuggestedDocuments());

            // Agent says: Maybe this could help: https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm
            searchQuery = new SearchQueryBuilder()
                .WithQuery("Maybe this can help: https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                .WithMessageType(SearchQuery.MessageType.Agent)
                .WithChatKey(searchQuery.ChatKey)
                .Build();

            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis()))
                }));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            result = this._suggestionController.GetSuggestions(searchQuery);

            result.Should().NotBeNull();

            suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.SuggestedDocuments.Count.Should().NotBe(0);
            suggestion.SuggestedDocuments.Select(x => x.Uri)
                .Contains("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm").Should().BeFalse();
        }

        [Test]
        public void When_getting_suggestions_with_customer_sending_uri_then_suggestion_is_filter_then_returns_suggestions_correctly()
        {
            // Customer says: Hello
            var searchQuery = new SearchQueryBuilder()
                .WithQuery("Hello")
                .Build();

            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(this.GetIrrelevantNlpAnalysis()))
                }));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            var result = this._suggestionController.GetSuggestions(searchQuery);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.SuggestedDocuments.Should().BeEquivalentTo(new List<SuggestedDocument>());

            // Customer says: I need help with CoveoSearch API
            searchQuery = new SearchQueryBuilder()
                .WithQuery("I need help with CoveoSearch API and I look at this: https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                .WithChatKey(searchQuery.ChatKey)
                .Build();

            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis()))
                }));

            this._indexSearchHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = this.GetSearchResultStringContent()
                }));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(searchQuery));
            result = this._suggestionController.GetSuggestions(searchQuery);

            result.Should().NotBeNull();

            suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.SuggestedDocuments.Count.Should().NotBe(0);
            suggestion.SuggestedDocuments.Select(x => x.Uri)
                .Contains("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm").Should().BeFalse();
        }

        [Test]
        public void When_getting_suggestions_with_more_attribute_then_required_then_returns_correctly()
        {
            var jsonSearchQuery = "{\"chatkey\": \"aecaa8db-abc8-4ac9-aa8d-87987da2dbb0\",\"Query\": \"Need help with CoveoSearch API\",\"Type\": 1,\"command\": \"sudo ls\"}";

            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis()))
                }));

            this._indexSearchHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = this.GetSearchResultStringContent()
                }));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(JsonConvert.DeserializeObject<SearchQuery>(jsonSearchQuery)));
            var result = this._suggestionController.GetSuggestions(JsonConvert.DeserializeObject<SearchQuery>(jsonSearchQuery));

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.SuggestedDocuments.Should().BeEquivalentTo(this.GetSuggestedDocuments());
        }

        [Test]
        public void When_getting_suggestions_then_refresh_result_are_the_same()
        {
            var jsonSearchQuery = "{\"chatkey\": \"aecaa8db-abc8-4ac9-aa8d-87987da2dbb0\",\"Query\": \"Need help with CoveoSearch API\",\"Type\": 1,\"command\": \"sudo ls\"}";
            var queryChatkeyRefresh = new Guid("aecaa8db-abc8-4ac9-aa8d-87987da2dbb0");

            this._nlpCallHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(this.GetRelevantNlpAnalysis()))
                }));

            this._indexSearchHttpMessageHandleMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = this.GetSearchResultStringContent()
                }));

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(JsonConvert.DeserializeObject<SearchQuery>(jsonSearchQuery)));
            var resultSuggestions = this._suggestionController.GetSuggestions(JsonConvert.DeserializeObject<SearchQuery>(jsonSearchQuery));

            var suggestedDocumentList = ((Suggestion) resultSuggestions.As<OkObjectResult>().Value).SuggestedDocuments as List<SuggestedDocument>;
            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(queryChatkeyRefresh));
            var resultLastSuggestions = this._suggestionController.GetSuggestions(queryChatkeyRefresh);

            var lastSuggestedDocument = ((Suggestion)resultLastSuggestions.As<OkObjectResult>().Value).SuggestedDocuments;
            lastSuggestedDocument.Should().BeEquivalentTo(suggestedDocumentList);
        }

        public ActionExecutingContext GetActionExecutingContext(SearchQuery searchQuery)
        {
            var actionContext = new ActionContext(
                new Mock<HttpContext>().Object,
                new Mock<RouteData>().Object,
                new Mock<ActionDescriptor>().Object
            );

            var actionExecutingContext = new Mock<ActionExecutingContext>(
                MockBehavior.Strict,
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                this._suggestionController);

            object param = searchQuery;
            actionExecutingContext
                .Setup(x => x.ActionArguments.TryGetValue("searchQuery", out param))
                .Returns(true);

            return actionExecutingContext.Object;
        }

        public ActionExecutingContext GetActionExecutingContext(Guid chatkey)
        {
            var actionContext = new ActionContext(
                new Mock<HttpContext>().Object,
                new Mock<RouteData>().Object,
                new Mock<ActionDescriptor>().Object
            );

            var actionExecutingContext = new Mock<ActionExecutingContext>(
                MockBehavior.Strict,
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                this._suggestionController);

            var param = new object();
            actionExecutingContext
                .Setup(x => x.ActionArguments.TryGetValue("searchQuery", out param))
                .Returns(false);

            param = chatkey;
            actionExecutingContext
                .Setup(x => x.ActionArguments.TryGetValue("chatkey", out param))
                .Returns(true);

            actionExecutingContext
                .Setup(x => x.ActionArguments["chatkey"])
                .Returns(chatkey);

            return actionExecutingContext.Object;
        }

        public NlpAnalysis GetRelevantNlpAnalysis()
        {
            var intents = new List<Intent>
            {
                new IntentBuilder().WithName("Need Help").WithConfidence(98).Build(),
                new IntentBuilder().WithName("Greetings").WithConfidence(2).Build()
            };

            return new NlpAnalysisBuilder().WithIntents(intents).Build();
        }

        public NlpAnalysis GetIrrelevantNlpAnalysis()
        {
            var intents = new List<Intent>
            {
                new IntentBuilder().WithName("Need Help").WithConfidence(2).Build(),
                new IntentBuilder().WithName("Greetings").WithConfidence(98).Build()
            };

            return new NlpAnalysisBuilder().WithIntents(intents).Build();
        }

        public List<SuggestedDocument> GetSuggestedDocuments()
        {
            return new List<SuggestedDocument>
            {
                new SuggestedDocument
                {
                    Title = "Available Coveo Cloud V2 Source Types",
                    Uri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                    PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                    Summary = null,
                    Excerpt = null
                },
                new SuggestedDocument
                {
                    Title = "Coveo Cloud Query Syntax Reference",
                    Uri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                    PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                    Summary = null,
                    Excerpt = null
                },
                new SuggestedDocument
                {
                    Title = "Events",
                    Uri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                    PrintableUri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                    Summary = null,
                    Excerpt = null
                },
                new SuggestedDocument
                {
                    Title = "Coveo Facet Component (CoveoFacet)",
                    Uri = "https://coveo.github.io/search-ui/components/facet.html",
                    PrintableUri = "https://coveo.github.io/search-ui/components/facet.html",
                    Summary = null,
                    Excerpt = null
                }
            };
        }

        public StringContent GetSearchResultStringContent()
        {
            return new StringContent("{\"totalCount\": 4,\"results\": [{\"title\": \"Available Coveo Cloud V2 Source Types\", \"excerpt\": \"This is the excerpt\", \"clickUri\": \"https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm\",\"printableUri\": \"https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm\",\"score\": 4280       },{\"title\": \"Coveo Cloud Query Syntax Reference\",\"excerpt\": \"This is the excerpt\", \"clickUri\": \"https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm\",\"printableUri\": \"https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm\",\"score\": 3900},{\"title\": \"Events\", \"excerpt\": \"This is the excerpt\", \"clickUri\": \"https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573\",\"printableUri\": \"https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573\",\"score\": 2947},{\"title\": \"Coveo Facet Component (CoveoFacet)\", \"excerpt\": \"This is the excerpt\", \"clickUri\": \"https://coveo.github.io/search-ui/components/facet.html\",\"printableUri\": \"https://coveo.github.io/search-ui/components/facet.html\",\"score\": 2932}]}");
        }

        public List<string> GetIrrelevantIntents()
        {
            return new List<string>
            {
                "Greetings"
            };
        }
    }
}
