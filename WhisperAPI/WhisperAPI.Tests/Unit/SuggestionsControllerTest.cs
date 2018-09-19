using System;
using System.Collections.Generic;
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
using WhisperAPI.Services.Context;
using WhisperAPI.Services.Suggestions;
using static WhisperAPI.Models.SearchQuery;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class SuggestionsControllerTest
    {
        private List<SearchQuery> _invalidSearchQueryList;
        private List<SearchQuery> _validSearchQueryList;
        private List<SearchQuery> _invalidSearchQueryListSelectSuggestion;
        private List<SearchQuery> _validSearchQueryListSelectSuggestion;

        private Mock<ISuggestionsService> _suggestionServiceMock;
        private SuggestionsController _suggestionController;
        private InMemoryContexts _contexts;

        [SetUp]
        public void SetUp()
        {
            this._contexts = new InMemoryContexts(new TimeSpan(1, 0, 0, 0));
            this._invalidSearchQueryList = new List<SearchQuery>
            {
                null,
                new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = null, Type = MessageType.Customer },
                new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = null, Type = MessageType.Agent }
            };

            this._validSearchQueryList = new List<SearchQuery>
            {
                 new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = "test", Type = MessageType.Customer },
                 new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = "test", Type = MessageType.Agent }
            };

            this._invalidSearchQueryListSelectSuggestion = new List<SearchQuery>
            {
                null,
                new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = null, Type = MessageType.Agent },
                new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = "not_a_suggestion_id", Type = MessageType.Agent }
            };

            this._validSearchQueryListSelectSuggestion = new List<SearchQuery>
            {
                new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = "0f8fad7b-d9cb-469f-a165-708677289501", Type = MessageType.Agent }
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
                .Returns(this.GetListOfDocuments());

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, null);

            this._suggestionController.GetSuggestions(this._invalidSearchQueryList[invalidQueryIndex]).Should().BeEquivalentTo(new BadRequestResult());
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        public void When_receive_valid_searchQuery_then_return_Ok_request(int validQueryIndex)
        {
            this._suggestionServiceMock = new Mock<ISuggestionsService>();
            this._suggestionServiceMock
                .Setup(x => x.GetSuggestedDocuments(It.IsAny<ConversationContext>()))
                .Returns(this.GetListOfDocuments());

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, this._contexts);

            this._suggestionController.OnActionExecuting(this.GetActionExecutingContext(validQueryIndex));
            var result = this._suggestionController.GetSuggestions(this._validSearchQueryList[validQueryIndex]);

            var suggestion = result.As<OkObjectResult>().Value as Suggestion;

            suggestion.SuggestedDocuments.Should().BeEquivalentTo(this.GetListOfDocuments());
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void When_receive_invalid_or_null_searchQuery_then_return_bad_request_select_suggestion(int invalidQueryIndex)
        {
            this._suggestionServiceMock = new Mock<ISuggestionsService>();
            this._suggestionServiceMock
                .Setup(x => x.UpdateContextWithSelectedSuggestion(It.IsAny<ConversationContext>(), It.IsAny<SearchQuery>()))
                .Returns(false);

            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, null);
            this._suggestionController.SelectSuggestion(this._invalidSearchQueryListSelectSuggestion[invalidQueryIndex]).Should().BeEquivalentTo(new BadRequestResult());
        }

        [Test]
        [TestCase(0)]
        public void When_receive_valid_searchQuery_then_return_Ok_request_select_suggestion(int validQueryIndex)
        {
            this._suggestionServiceMock = new Mock<ISuggestionsService>();
            this._suggestionServiceMock
                .Setup(x => x.UpdateContextWithSelectedSuggestion(It.IsAny<ConversationContext>(), It.IsAny<SearchQuery>()))
                .Returns(true);
            this._suggestionController = new SuggestionsController(this._suggestionServiceMock.Object, null);
            this._suggestionController.SelectSuggestion(this._validSearchQueryListSelectSuggestion[validQueryIndex]).Should().BeEquivalentTo(new OkResult());
        }

        public ActionExecutingContext GetActionExecutingContext(int indexOfTest)
        {
            var actionContext = new ActionContext(
                new Mock<HttpContext>().Object,
                new Mock<RouteData>().Object,
                new Mock<ActionDescriptor>().Object);

            var actionExecutingContext = new Mock<ActionExecutingContext>(
                MockBehavior.Strict,
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                this._suggestionController);

            actionExecutingContext
                .Setup(x => x.ActionArguments["searchQuery"])
                .Returns(this._validSearchQueryList[indexOfTest]);

            return actionExecutingContext.Object;
        }

        public List<SuggestedDocument> GetListOfDocuments()
        {
            return new List<SuggestedDocument>
            {
                new SuggestedDocument
                {
                    Title = "Available Coveo Cloud V2 Source Types",
                    Uri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                    PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                    Summary = null
                },
                new SuggestedDocument
                {
                    Title = "Coveo Cloud Query Syntax Reference",
                    Uri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                    PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                    Summary = null
                },
                new SuggestedDocument
                {
                    Title = "Events",
                    Uri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                    PrintableUri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                    Summary = null
                },
                new SuggestedDocument
                {
                    Title = "Coveo Facet Component (CoveoFacet)",
                    Uri = "https://coveo.github.io/search-ui/components/facet.html",
                    PrintableUri = "https://coveo.github.io/search-ui/components/facet.html",
                    Summary = null
                }
            };
        }
    }
}
