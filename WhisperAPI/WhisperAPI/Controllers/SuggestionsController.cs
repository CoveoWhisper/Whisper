﻿using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WhisperAPI.Models;
using WhisperAPI.Models.Queries;
using WhisperAPI.Services.Context;
using WhisperAPI.Services.Suggestions;

namespace WhisperAPI.Controllers
{
    [Route("/Whisper/[Controller]")]
    public class SuggestionsController : ContextController
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISuggestionsService _suggestionsService;

        public SuggestionsController(ISuggestionsService suggestionsService, IContexts contexts)
            : base(contexts)
        {
            this._suggestionsService = suggestionsService;
        }

        [HttpPost]
        public IActionResult GetSuggestions([FromBody] SearchQuery searchQuery)
        {
            if (!this.ModelState.IsValid || searchQuery?.Query == null)
            {
                return this.BadRequest();
            }

            this._suggestionsService.UpdateContextWithNewQuery(this.ConversationContext, searchQuery);

            var suggestion = new Suggestion();

            var suggestedDocuments = this._suggestionsService.GetSuggestedDocuments(this.ConversationContext).ToList();
            suggestion.SuggestedDocuments = suggestedDocuments;

            this._suggestionsService.UpdateContextWithNewSuggestions(this.ConversationContext, suggestedDocuments);
            suggestedDocuments.ForEach(x => Log.Debug($"Id: {x.Id}, Title: {x.Title}, Uri: {x.Uri}, PrintableUri: {x.PrintableUri}, Summary: {x.Summary}"));

            if (suggestedDocuments.Any())
            {
                var questions = this._suggestionsService.GetQuestionsFromDocument(this.ConversationContext, suggestedDocuments).ToList();
                suggestion.Questions = questions;

                this._suggestionsService.UpdateContextWithNewQuestions(this.ConversationContext, questions);
                questions.ForEach(x => Log.Debug($"Id: {x.Id}, Text: {x.Text}"));
            }

            return this.Ok(suggestion);
        }

        [HttpPost("Select")]
        public IActionResult SelectSuggestion([FromBody] SelectQuery selectQuery)
        {
            if (!this.ModelState.IsValid || selectQuery == null || selectQuery.Id == null)
            {
                return this.BadRequest();
            }

            bool isContextUpdated = this._suggestionsService.UpdateContextWithSelectedSuggestion(this.ConversationContext, selectQuery.Id.Value);
            if (!isContextUpdated)
            {
                return this.BadRequest();
            }

            Log.Debug($"Select suggestion with id {selectQuery.Id}");

            return this.Ok();
        }
    }
}
