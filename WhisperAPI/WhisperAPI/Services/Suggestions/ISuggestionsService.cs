using System;
using System.Collections.Generic;
using WhisperAPI.Models;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Services.Suggestions
{
    public interface ISuggestionsService
    {
        Suggestion GetNewSuggestion(ConversationContext conversationContext, SuggestionQuery query);

        IEnumerable<Recommendation<Document>> GetLongQuerySearchRecommendations(ConversationContext conversationContext);

        void UpdateContextWithNewQuery(ConversationContext conversationContext, SearchQuery searchQuery);

        bool UpdateContextWithSelectedSuggestion(ConversationContext conversationContext, Guid selectQueryId);
    }
}
