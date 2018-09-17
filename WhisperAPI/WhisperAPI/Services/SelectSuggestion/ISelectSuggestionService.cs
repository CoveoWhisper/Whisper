using WhisperAPI.Models;

namespace WhisperAPI.Services.SelectSuggestion
{
    public interface ISelectSuggestionService
    {
        bool UpdateContextWithSelectedSuggestion(ConversationContext conversationContext, SearchQuery searchQuery);
    }
}
