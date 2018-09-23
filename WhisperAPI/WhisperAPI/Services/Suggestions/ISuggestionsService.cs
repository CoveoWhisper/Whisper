using System.Collections.Generic;
using WhisperAPI.Models;

namespace WhisperAPI.Services.Suggestions
{
    public interface ISuggestionsService
    {
        IEnumerable<SuggestedDocument> GetSuggestedDocuments(ConversationContext conversationContext);

        void UpdateContextWithNewQuery(ConversationContext conversationContext, SearchQuery searchQuery);

        void UpdateContextWithNewSuggestions(ConversationContext conversationContext, List<SuggestedDocument> suggestedDocuments);
    }
}
