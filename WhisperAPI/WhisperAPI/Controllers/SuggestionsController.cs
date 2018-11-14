using System;
using Microsoft.AspNetCore.Mvc;
using WhisperAPI.Models;
using WhisperAPI.Models.Queries;
using WhisperAPI.Services.Context;
using WhisperAPI.Services.NLPAPI;
using WhisperAPI.Services.Questions;
using WhisperAPI.Services.Suggestions;

namespace WhisperAPI.Controllers
{
    [Route("/Whisper/[Controller]")]
    public class SuggestionsController : ContextController
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISuggestionsService _suggestionsService;

        private readonly IQuestionsService _questionsService;

        private readonly INlpCall _nlpService;

        public SuggestionsController(ISuggestionsService suggestionsService, IQuestionsService questionsService, INlpCall nlpService, IContexts contexts)
            : base(contexts)
        {
            this._suggestionsService = suggestionsService;
            this._questionsService = questionsService;
            this._nlpService = nlpService;
        }

        [HttpPost]
        public IActionResult GetSuggestions([FromBody] SearchQuery searchQuery)
        {
            this._nlpService.UpdateAndAnalyseSearchQuery(searchQuery);
            this._suggestionsService.UpdateContextWithNewQuery(this.ConversationContext, searchQuery);
            this._questionsService.DetectAnswer(this.ConversationContext, searchQuery);
            bool questionAskedDetected = this._questionsService.DetectQuestionAsked(this.ConversationContext, searchQuery);

            if (questionAskedDetected)
            {
                searchQuery.Relevant = false;
            }

            var suggestion = this._suggestionsService.GetNewSuggestion(this.ConversationContext, searchQuery);

            LogSuggestion(suggestion);
            return this.Ok(suggestion);
        }

        [HttpGet]
        public IActionResult GetSuggestions(SuggestionQuery suggestionQuery)
        {
            Log.Debug($"SuggestionQuery: {suggestionQuery}");

            var suggestion = this._suggestionsService.GetLastSuggestion(this.ConversationContext, suggestionQuery);

            LogSuggestion(suggestion);

            return this.Ok(suggestion);
        }

        [HttpPost("Select")]
        public IActionResult SelectSuggestion([FromBody] SelectQuery selectQuery)
        {
            var isContextUpdated = this._suggestionsService.UpdateContextWithSelectedSuggestion(this.ConversationContext, selectQuery.Id.GetValueOrDefault());
            if (!isContextUpdated)
            {
                return this.BadRequest($"Could not find any suggestion with id: {selectQuery.Id}");
            }

            Log.Debug($"Select suggestion with id {selectQuery.Id}");
            return this.Ok();
        }

        [HttpDelete("Facets")]
        public IActionResult RemoveAllFacets([FromBody] Query query)
        {
            this._questionsService.RejectAllAnswers(this.ConversationContext);

            Log.Debug("Removed all facets");
            return this.NoContent();
        }

        [HttpDelete("Facets/{id}")]
        public IActionResult RemoveFacet([FromRoute] Guid id, [FromBody] Query query)
        {
            if (!this._questionsService.RejectAnswer(this.ConversationContext, id))
            {
                return this.BadRequest($"Question with id {id} doesn't exist.");
            }

            Log.Debug($"Removed facet with id {id}");
            return this.NoContent();
        }

        [HttpPut("Filter")]
        public IActionResult AddFilter([FromBody] FilterQuery query)
        {
            this.ConversationContext.FilterDocumentsParameters.MustHaveFacets.Add(query.Facet);
            return this.NoContent();
        }

        [HttpDelete("Filter")]
        public IActionResult RemoveFilter([FromBody] FilterQuery query)
        {
            this.ConversationContext.FilterDocumentsParameters.MustHaveFacets.Remove(query.Facet);
            return this.NoContent();
        }

        private static void LogSuggestion(Suggestion suggestion)
        {
            suggestion.Documents?.ForEach(x => Log.Debug($"Title: {x.Title}, Uri: {x.Uri}, PrintableUri: {x.PrintableUri}, Summary: {x.Summary}"));
            suggestion.Questions?.ForEach(x => Log.Debug($"Id: {x.Id}, Text: {x.Text}"));
            suggestion.ActiveFacets?.ForEach(x => Log.Debug($"Id: {x.Id}, Name: {x.Name}, Value: {x.Value}"));
        }
    }
}
