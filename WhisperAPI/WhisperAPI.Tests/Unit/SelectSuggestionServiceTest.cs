using System;
using System.Collections.Generic;
using NUnit.Framework;
using WhisperAPI.Models;
using WhisperAPI.Services.SelectSuggestion;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class SelectSuggestionServiceTest
    {
        private ISelectSuggestionService _selectSuggestionService;
        private Guid _chatkey;
        private ConversationContext _conversationContext;

        [SetUp]
        public void SetUp()
        {
            this._selectSuggestionService = new SelectSuggestionService();
            this._chatkey = new Guid("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d9");
            this._conversationContext = new ConversationContext(this._chatkey, DateTime.Now);
        }

        [Test]
        [TestCase("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d1", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d2", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d1")]
        [TestCase("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d1", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d2", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d2")]
        public void When_receiving_valid_suggestion_selection_add_suggestion_to_list_and_return_true(string suggestedDocumentId, string questionId, string selectSuggestionId)
        {
            SuggestedDocument suggestedDocument = new SuggestedDocument();
            Question question = new Question();
            suggestedDocument.Id = new Guid(suggestedDocumentId);
            question.Id = new Guid(questionId);
            SearchQuery searchQuery = new SearchQuery();
            searchQuery.ChatKey = this._chatkey;
            searchQuery.Query = selectSuggestionId;

            this._conversationContext.SuggestedDocuments.Add(suggestedDocument);
            this._conversationContext.Questions.Add(question);

            bool isContextUpdated = this._selectSuggestionService.UpdateContextWithSelectedSuggestion(this._conversationContext, searchQuery);

            Assert.IsTrue(isContextUpdated);
            Assert.IsTrue(this._conversationContext.SelectedQuestions.Contains(question) != this._conversationContext.SelectedSuggestedDocuments.Contains(suggestedDocument));
        }

        [Test]
        [TestCase("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d3")]
        public void When_receiving_invalid_suggestion_selection_do_not_add_suggestion_to_list_and_return_false(string selectSuggestionId)
        {
            SearchQuery searchQuery = new SearchQuery();
            searchQuery.ChatKey = this._chatkey;
            searchQuery.Query = selectSuggestionId;
            this._conversationContext.SelectedSuggestedDocuments.Clear();
            this._conversationContext.SelectedQuestions.Clear();

            bool isContextUpdated = this._selectSuggestionService.UpdateContextWithSelectedSuggestion(this._conversationContext, searchQuery);

            Assert.IsFalse(isContextUpdated);
            Assert.IsTrue(this._conversationContext.SelectedSuggestedDocuments.Count == 0);
            Assert.IsTrue(this._conversationContext.SelectedQuestions.Count == 0);
        }
    }
}
