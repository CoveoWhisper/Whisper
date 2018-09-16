using System.Linq;
using WhisperAPI.Models;

namespace WhisperAPI.Services.SelectSuggestion
{
    public class SelectSuggestionsService : ISelectSuggestionService
    {
        public void UpdateContextWithSelectedSuggestion(ConversationContext conversationContext, SelectSuggestionPost selectSuggestionPost)
        {
            SuggestedDocument suggestedDocument = conversationContext.SuggestedDocuments.ToList().Find(x => x.Id == selectSuggestionPost.Id);
            if (suggestedDocument == null)
            {
                Question question = conversationContext.Questions.ToList().Find(x => x.Id == selectSuggestionPost.Id);
                conversationContext.SelectedQuestions.Add(question);
                return;
            }

            conversationContext.SelectedSuggestedDocuments.Add(suggestedDocument);
        }
    }
}
