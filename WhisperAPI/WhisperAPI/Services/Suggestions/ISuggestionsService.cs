using System;
using System.Collections.Generic;
using WhisperAPI.Models;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Services.Suggestions
{
    public interface ISuggestionsService
    {
        Suggestion GetNewSuggestion(ConversationContext conversationContext, SuggestionQuery query);

        IEnumerable<Recommendation<Document>> GetLongQuerySearchRecommendations(ConversationContext conversationContext);

        void UpdateContextWithNewItem(ConversationContext context, NlpAnalysis nlpAnalysis, SearchQuery searchQuery, bool isRelevant);

        bool UpdateContextWithSelectedSuggestion(ConversationContext conversationContext, Guid selectQueryId);
    }
}
