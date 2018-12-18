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

        /// <summary>
        /// Add the new query into context and returns relevant documents and questions
        /// </summary>
        /// <param name="searchQuery">Query contaning the new query send from agent of asker</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult GetSuggestions([FromBody] SearchQuery searchQuery)
        {
            var nlpAnalysis = this._nlpService.AnalyzeSearchQuery(searchQuery, out bool isRelevant);

            this._questionsService.DetectAnswer(this.ConversationContext, searchQuery);
            bool questionAskedDetected = this._questionsService.DetectQuestionAsked(this.ConversationContext, searchQuery);

            if (questionAskedDetected)
            {
                isRelevant = false;
            }

            this._suggestionsService.UpdateContextWithNewItem(this.ConversationContext, nlpAnalysis, searchQuery, isRelevant);

            var suggestion = this._suggestionsService.GetNewSuggestion(this.ConversationContext, searchQuery);

            LogSuggestion(suggestion);
            return this.Ok(suggestion);
        }

        /// <summary>
        /// This method returns the object containing all documents and questions without having a new query
        /// </summary>
        /// <param name="suggestionQuery">Query containing the chatkey of the context to get</param>
        /// <returns>Suggestion of documents and questions</returns>
        [HttpGet]
        public IActionResult GetSuggestions(SuggestionQuery suggestionQuery)
        {
            Log.Debug($"SuggestionQuery: {suggestionQuery}");

            var suggestion = this._suggestionsService.GetNewSuggestion(this.ConversationContext, suggestionQuery);

            LogSuggestion(suggestion);

            return this.Ok(suggestion);
        }

        /// <summary>
        /// When an agent click on a suggestion in the UI, this method is call to update the status of it
        /// </summary>
        /// <param name="selectQuery">Query contaning the id of the suggestion</param>
        /// <returns></returns>
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

        /// <summary>
        /// Remove all facet in the context
        /// </summary>
        /// <param name="query">Query containing the chatkey</param>
        /// <returns></returns>
        [HttpDelete("Facets")]
        public IActionResult RemoveAllFacets([FromBody] Query query)
        {
            this._questionsService.RejectAllAnswers(this.ConversationContext);

            Log.Debug("Removed all facets");
            return this.NoContent();
        }

        /// <summary>
        /// Remove completely a facet and all her values
        /// </summary>
        /// <param name="id">id of the facet to remove</param>
        /// <param name="query">Query containing the chatkey</param>
        /// <returns></returns>
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

        /// <summary>
        /// Add a facet value for filtering documents
        /// </summary>
        /// <param name="query">Query containing the facet to add</param>
        /// <returns></returns>
        [HttpPut("Filter")]
        public IActionResult AddFilter([FromBody] FilterQuery query)
        {
            this.ConversationContext.AddMustHaveFacets(query.Facet);
            return this.NoContent();
        }

        /// <summary>
        /// Remove a facet value for filtering documents
        /// </summary>
        /// <param name="query">Query containing the facet to remove</param>
        /// <returns></returns>
        [HttpDelete("Filter")]
        public IActionResult RemoveFilter([FromBody] FilterQuery query)
        {
            this.ConversationContext.RemoveFacetValue(query.Facet);
            return this.NoContent();
        }

        private static void LogSuggestion(Suggestion suggestion)
        {
            suggestion.Documents?.ForEach(x => Log.Debug($"Title: {x.Value.Title}, Uri: {x.Value.Uri}, PrintableUri: {x.Value.PrintableUri}, Summary: {x.Value.Summary}"));
            suggestion.Questions?.ForEach(x => Log.Debug($"Id: {x.Value.Id}, Text: {x.Value.Text}"));
            suggestion.ActiveFacets?.ForEach(x => Log.Debug($"Id: {x.Id}, Name: {x.Name}, Values: {string.Join(" ", x.Values)}"));
        }
    }
}
