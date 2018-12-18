using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhisperAPI.Models;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Services.Suggestions
{
    public interface ISuggestionsService
    {
        /// <summary>
        /// This method calls Coveo, MLAPI and NLPAPI to get documents and questions relevant to the conversation
        /// </summary>
        /// <param name="conversationContext">The context of the conversation</param>
        /// <param name="query">The new message received</param>
        /// <returns>All documents and suggestions relevant to the conversation</returns>
        Suggestion GetNewSuggestion(ConversationContext conversationContext, SuggestionQuery query);

        /// <summary>
        /// This method calls NLPAPI to get documents from analytics recommender
        /// </summary>
        /// <param name="conversationContext">The context of the conversation</param>
        /// <returns>List of recommandation of documents</returns>
        Task<IEnumerable<Recommendation<Document>>> GetLongQuerySearchRecommendations(ConversationContext conversationContext);

        /// <summary>
        /// Update the context 
        /// </summary>
        /// <param name="context">The context of the conversation</param>
        /// <param name="nlpAnalysis">The result of NLPAPI analysis</param>
        /// <param name="searchQuery">The new query</param>
        /// <param name="isRelevant">If the query is relevant</param>
        void UpdateContextWithNewItem(ConversationContext context, NlpAnalysis nlpAnalysis, SearchQuery searchQuery, bool isRelevant);

        /// <summary>
        /// Update the context with a suggestion that the agent has selected in the UI
        /// </summary>
        /// <param name="conversationContext">The context of the conversation</param>
        /// <param name="selectQueryId">Query containing the id of the suggestion</param>
        /// <returns>True if done, false if not</returns>
        bool UpdateContextWithSelectedSuggestion(ConversationContext conversationContext, Guid selectQueryId);
    }
}
