using System;
using System.Linq;
using WhisperAPI.Models;

namespace WhisperAPI.Services.SelectSuggestion
{
    public class SelectSuggestionService : ISelectSuggestionService
    {
        public bool UpdateContextWithSelectedSuggestion(ConversationContext conversationContext, SearchQuery searchQuery)
        {
            Guid suggestionId = new Guid(searchQuery.Query);
            SuggestedDocument suggestedDocument = conversationContext.SuggestedDocuments.ToList().Find(x => x.Id == suggestionId);
            if (suggestedDocument == null)
            {
                Question question = conversationContext.Questions.ToList().Find(x => x.Id == suggestionId);
                if (question == null)
                {
                    return false;
                }

                conversationContext.SelectedQuestions.Add(question);
                return true;
            }

            conversationContext.SelectedSuggestedDocuments.Add(suggestedDocument);
            return true;
        }
    }
}
