using WhisperAPI.Models;

namespace WhisperAPI.Services.SelectSuggestion
{
    public interface ISelectSuggestionService
    {
        void UpdateContextWithSelectedSuggestion(ConversationContext conversationContext, SelectSuggestionPost selectSuggestionPost);
    }
}
