using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using WhisperAPI.Controllers;
using WhisperAPI.Models;
using WhisperAPI.Services.SelectSuggestion;
using static WhisperAPI.Models.SearchQuery;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class SelectSuggestionControllerTest
    {
        private List<SearchQuery> _invalidSearchQueryList;
        private List<SearchQuery> _validSearchQueryList;

        private Mock<ISelectSuggestionService> _selectSuggestionServiceMock;
        private SelectSuggestionController _selectSuggestionController;

        [SetUp]
        public void SetUp()
        {
            this._invalidSearchQueryList = new List<SearchQuery>
            {
                null,
                new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = null, Type = MessageType.Agent },
                new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = "not_a_suggestion_id", Type = MessageType.Agent }
            };

            this._validSearchQueryList = new List<SearchQuery>
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
            this._selectSuggestionServiceMock = new Mock<ISelectSuggestionService>();
            this._selectSuggestionServiceMock
                .Setup(x => x.UpdateContextWithSelectedSuggestion(It.IsAny<ConversationContext>(), It.IsAny<SearchQuery>()))
                .Returns(false);

            this._selectSuggestionController = new SelectSuggestionController(this._selectSuggestionServiceMock.Object, null);
            this._selectSuggestionController.SelectSuggestion(this._invalidSearchQueryList[invalidQueryIndex]).Should().BeEquivalentTo(new BadRequestResult());
        }

        [Test]
        [TestCase(0)]
        public void When_receive_valid_searchQuery_then_return_Ok_request(int validQueryIndex)
        {
            this._selectSuggestionServiceMock = new Mock<ISelectSuggestionService>();
            this._selectSuggestionServiceMock
                .Setup(x => x.UpdateContextWithSelectedSuggestion(It.IsAny<ConversationContext>(), It.IsAny<SearchQuery>()))
                .Returns(true);
            this._selectSuggestionController = new SelectSuggestionController(this._selectSuggestionServiceMock.Object, null);
            this._selectSuggestionController.SelectSuggestion(this._validSearchQueryList[validQueryIndex]).Should().BeEquivalentTo(new OkResult());
        }
    }
}
