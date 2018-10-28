using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
                .Setup(x => x.GetSuggestedDocuments(It.IsAny<ConversationContext>()))
                .Returns(GetListOfDocuments());

            this._questionsServiceMock = new Mock<IQuestionsService>();

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, this._questionsServiceMock.Object, this._contexts);

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
                Questions = GetListOfQuestions().Select(QuestionToClient.FromQuestion).ToList(),
                SuggestedDocuments = GetListOfDocuments()
            };

            this._suggestionServiceMock = new Mock<ISuggestionsService>();
            var query = this._validSearchQueryList[validQueryIndex];

            this._suggestionServiceMock
<<<<<<< HEAD
                .Setup(x => x.GetNewSuggestion(It.IsAny<ConversationContext>(), query))
=======
                .Setup(x => x.GetSuggestion(It.IsAny<ConversationContext>(), query))
>>>>>>> Functionality added, need to comply to old test + make new ones
                .Returns(suggestionFromService);

            this._questionsServiceMock = new Mock<IQuestionsService>();

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, this._questionsServiceMock.Object, this._contexts);

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(query));

            var result = this._suggestionController.GetSuggestions(query);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;
            suggestion.Should().NotBeNull();
            suggestion?.SuggestedDocuments.Should().BeEquivalentTo(suggestionFromService.SuggestedDocuments);
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
            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, this._questionsServiceMock.Object, this._contexts);
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

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, this._questionsServiceMock.Object, this._contexts);

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(query));
            this._suggestionController.SelectSuggestion(this._validSelectQueryList[validQueryIndex]).Should().BeEquivalentTo(new OkResult());
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

        private static List<SuggestedDocument> GetListOfDocuments()
        {
            return new List<SuggestedDocument>
            {
                SuggestedDocumentBuilder.Build
                    .WithTitle("Available Coveo Cloud V2 Source Types")
                    .WithUri("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                    .WithPrintableUri("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                    .Instance,
                SuggestedDocumentBuilder.Build
                    .WithTitle("Coveo Cloud Query Syntax Reference")
                    .WithUri("https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm")
                    .WithPrintableUri("https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm")
                    .Instance,
                SuggestedDocumentBuilder.Build
                    .WithTitle("Events")
                    .WithUri("https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573")
                    .WithPrintableUri("https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573")
                    .Instance,
                SuggestedDocumentBuilder.Build
                    .WithTitle("Coveo Facet Component (CoveoFacet)")
                    .WithUri("https://coveo.github.io/search-ui/components/facet.html")
                    .WithPrintableUri("https://coveo.github.io/search-ui/components/facet.html")
                    .Instance
            };
        }

        private static List<Question> GetListOfQuestions()
        {
            return new List<Question>
            {
                FacetQuestionBuilder.Build.WithFacetName("Dummy").WithFacetValues("A", "B", "C").Instance,
                FacetQuestionBuilder.Build.WithFacetName("Dummy").WithFacetValues("C", "D", "E").Instance,
            };
        }
    }
}
