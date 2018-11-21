using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using WhisperAPI.Controllers;
using WhisperAPI.Models;
using WhisperAPI.Models.Queries;
using WhisperAPI.Services.Context;
using WhisperAPI.Services.NLPAPI;
using WhisperAPI.Services.Questions;
using WhisperAPI.Services.Suggestions;
using WhisperAPI.Tests.Data.Builders;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class SuggestionsControllerTest
    {
        private List<SearchQuery> _invalidSearchQueryList;
        private List<SearchQuery> _validSearchQueryList;
        private List<SelectQuery> _invalidSelectQueryList;
        private List<SelectQuery> _validSelectQueryList;

        private Mock<ISuggestionsService> _suggestionServiceMock;
        private Mock<IQuestionsService> _questionsServiceMock;
        private Mock<INlpCall> _nlpServiceMock;
        private SuggestionsController _suggestionController;
        private InMemoryContexts _contexts;

        [SetUp]
        public void SetUp()
        {
            var chatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501");

            this._contexts = new InMemoryContexts(new TimeSpan(1, 0, 0, 0));
            this._invalidSearchQueryList = new List<SearchQuery>
            {
                null,
                SearchQueryBuilder.Build.WithChatKey(chatKey).WithQuery(null).WithMessageType(SearchQuery.MessageType.Customer).Instance,
                SearchQueryBuilder.Build.WithChatKey(chatKey).WithQuery(null).WithMessageType(SearchQuery.MessageType.Agent).Instance,
            };

            this._validSearchQueryList = new List<SearchQuery>
            {
                SearchQueryBuilder.Build.WithChatKey(chatKey).WithQuery("test").WithMessageType(SearchQuery.MessageType.Customer).Instance,
                SearchQueryBuilder.Build.WithChatKey(chatKey).WithQuery("test").WithMessageType(SearchQuery.MessageType.Agent).Instance,
                SearchQueryBuilder.Build.WithChatKey(chatKey).WithQuery("test").WithMaxDocuments(5).WithMaxQuestions(5).WithMessageType(SearchQuery.MessageType.Customer).Instance,
            };

            this._invalidSelectQueryList = new List<SelectQuery>
            {
                null,
                SelectQueryBuilder.Build.WithChatKey(chatKey).WithId(null).Instance,
            };

            this._validSelectQueryList = new List<SelectQuery>
            {
                SelectQueryBuilder.Build.WithChatKey(chatKey).WithId(new Guid("0f8fad7b-d9cb-469f-a165-708677289501")).Instance,
            };
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void When_receive_invalid_or_null_searchQuery_then_return_bad_request(int invalidQueryIndex)
        {
            this._suggestionServiceMock = new Mock<ISuggestionsService>();
            this._suggestionServiceMock
                .Setup(x => x.GetLongQuerySearchRecommendations(It.IsAny<ConversationContext>()))
                .Returns(GetListOfDocuments());

            this._questionsServiceMock = new Mock<IQuestionsService>();

            this._nlpServiceMock = new Mock<INlpCall>();

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, this._questionsServiceMock.Object, this._nlpServiceMock.Object, this._contexts);

            SearchQuery query = this._invalidSearchQueryList[invalidQueryIndex];

            var actionContext = this.GetActionExecutingContext(query);
            this._suggestionController.OnActionExecuting(actionContext);
            actionContext.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        public void When_receive_valid_searchQuery_then_return_Ok_request(int validQueryIndex)
        {
            var suggestionFromService = new Suggestion
            {
                Questions = GetListOfQuestions().Select(r => r.ConvertValue(QuestionToClient.FromQuestion)).ToList(),
                Documents = GetListOfDocuments()
            };

            this._suggestionServiceMock = new Mock<ISuggestionsService>();
            var query = this._validSearchQueryList[validQueryIndex];

            this._suggestionServiceMock
                .Setup(x => x.GetNewSuggestion(It.IsAny<ConversationContext>(), query))
                .Returns(suggestionFromService);

            this._questionsServiceMock = new Mock<IQuestionsService>();

            this._nlpServiceMock = new Mock<INlpCall>();

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, this._questionsServiceMock.Object, this._nlpServiceMock.Object, this._contexts);

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(query));

            var result = this._suggestionController.GetSuggestions(query);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;
            suggestion.Should().NotBeNull();
            suggestion?.Documents.Should().BeEquivalentTo(suggestionFromService.Documents);
            suggestion?.Questions.Should().BeEquivalentTo(suggestionFromService.Questions);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        public void When_receive_invalid_or_null_selectQuery_then_return_bad_request(int invalidQueryIndex)
        {
            this._suggestionServiceMock = new Mock<ISuggestionsService>();
            this._suggestionServiceMock
                .Setup(x => x.UpdateContextWithSelectedSuggestion(It.IsAny<ConversationContext>(), new Guid("c21d07d5-fd5a-42ab-ac2c-2ef6101e58d1")))
                .Returns(false);

            SelectQuery query = this._invalidSelectQueryList[invalidQueryIndex];

            this._questionsServiceMock = new Mock<IQuestionsService>();

            this._nlpServiceMock = new Mock<INlpCall>();

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, this._questionsServiceMock.Object, this._nlpServiceMock.Object, this._contexts);
            var actionContext = this.GetActionExecutingContext(query);
            this._suggestionController.OnActionExecuting(actionContext);
            actionContext.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        [TestCase(0)]
        public void When_receive_valid_selectQuery_then_return_Ok_request(int validQueryIndex)
        {
            this._suggestionServiceMock = new Mock<ISuggestionsService>();
            this._suggestionServiceMock
                .Setup(x => x.UpdateContextWithSelectedSuggestion(It.IsAny<ConversationContext>(), this._validSelectQueryList[validQueryIndex].Id.Value))
                .Returns(true);

            SelectQuery query = this._invalidSelectQueryList[validQueryIndex];

            this._questionsServiceMock = new Mock<IQuestionsService>();

            this._nlpServiceMock = new Mock<INlpCall>();

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, this._questionsServiceMock.Object, this._nlpServiceMock.Object, this._contexts);

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(query));
            this._suggestionController.SelectSuggestion(this._validSelectQueryList[validQueryIndex]).Should().BeEquivalentTo(new OkResult());
        }

        [Test]
        public void When_adding_facet_filter_then_it_is_added_in_context()
        {
            this._questionsServiceMock = new Mock<IQuestionsService>();
            this._suggestionServiceMock = new Mock<ISuggestionsService>();

            var facet = FacetBuilder.Build
                .WithName("Year")
                .WithValue("2018")
                .Instance;

            var query = FilterQueryBuilder.Build
                .WithFacet(facet)
                .Instance;

            this._nlpServiceMock = new Mock<INlpCall>();

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, this._questionsServiceMock.Object, this._nlpServiceMock.Object, this._contexts);

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(query));
            this._suggestionController.AddFilter(query);

            var context =
                this._suggestionController.GetType().BaseType
                    .GetProperty("ConversationContext", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(this._suggestionController) as ConversationContext;

            context.FilterDocumentsParameters.MustHaveFacets.Should().HaveCount(1);
            context.FilterDocumentsParameters.MustHaveFacets.Single().Should().BeEquivalentTo(facet);
        }

        [Test]
        public void When_removing_facet_filter_then_it_is_removed_in_context()
        {
            this._questionsServiceMock = new Mock<IQuestionsService>();
            this._suggestionServiceMock = new Mock<ISuggestionsService>();

            var facet = FacetBuilder.Build
                .WithName("Year")
                .WithValue("2018")
                .Instance;

            var query = FilterQueryBuilder.Build
                .WithFacet(facet)
                .Instance;

            this._nlpServiceMock = new Mock<INlpCall>();

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, this._questionsServiceMock.Object, this._nlpServiceMock.Object, this._contexts);

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(query));
            this._suggestionController.AddFilter(query);

            this._suggestionController.RemoveFilter(query);

            var context =
                this._suggestionController.GetType().BaseType
                    .GetProperty("ConversationContext", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(this._suggestionController) as ConversationContext;

            context.FilterDocumentsParameters.MustHaveFacets.Should().HaveCount(0);
        }

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
                        var key = result.MemberNames.FirstOrDefault() ?? string.Empty;
                        actionExecutingContext.ModelState.AddModelError(key, result.ErrorMessage);
                        this._suggestionController.ModelState.AddModelError(key, result.ErrorMessage);
                    }
                }
            }

            return actionExecutingContext;
        }

        private static List<Recommendation<Document>> GetListOfDocuments()
        {
            return new List<Recommendation<Document>>
            {
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Available Coveo Cloud V2 Source Types")
                        .WithUri("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                        .WithPrintableUri("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                        .Instance).Instance,
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Coveo Cloud Query Syntax Reference")
                        .WithUri("https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm")
                        .WithPrintableUri("https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm")
                        .Instance).Instance,
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Events")
                        .WithUri("https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573")
                        .WithPrintableUri("https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573")
                        .Instance).Instance,
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Coveo Facet Component (CoveoFacet)")
                        .WithUri("https://coveo.github.io/search-ui/components/facet.html")
                        .WithPrintableUri("https://coveo.github.io/search-ui/components/facet.html")
                        .Instance).Instance
            };
        }

        private static List<Recommendation<Question>> GetListOfQuestions()
        {
            return new List<Recommendation<Question>>
            {
                RecommendationBuilder<Question>.Build.WithValue(
                    FacetQuestionBuilder.Build
                        .WithFacetName("Dummy")
                        .WithFacetValues("A", "B", "C")
                        .Instance).Instance,
                RecommendationBuilder<Question>.Build.WithValue(
                    FacetQuestionBuilder.Build
                        .WithFacetName("Dummy")
                        .WithFacetValues("C", "D", "E")
                        .Instance).Instance
            };
        }
    }
}
