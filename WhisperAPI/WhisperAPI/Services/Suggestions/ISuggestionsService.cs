using System;
using System.Collections.Generic;
using WhisperAPI.Models;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Services.Suggestions
{
    public interface ISuggestionsService
    {
        Suggestion GetNewSuggestion(ConversationContext conversationContext, SuggestionQuery query);

        Suggestion GetLastSuggestion(ConversationContext conversationContext, SuggestionQuery query);

        IEnumerable<Document> GetDocuments(ConversationContext conversationContext);

        void UpdateContextWithNewItem(ConversationContext context, ContextItem contextItem);

        bool UpdateContextWithSelectedSuggestion(ConversationContext conversationContext, Guid selectQueryId);
    }
}
