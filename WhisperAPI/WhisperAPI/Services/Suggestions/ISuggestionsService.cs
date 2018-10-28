﻿using System;
using System.Collections.Generic;
using WhisperAPI.Models;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Services.Suggestions
{
    public interface ISuggestionsService
    {
<<<<<<< HEAD
        Suggestion GetNewSuggestion(ConversationContext conversationContext, Query query);

        Suggestion GetLastSuggestion(ConversationContext conversationContext, Query query);
=======
        Suggestion GetSuggestion(ConversationContext conversationContext, Query query);
>>>>>>> 5c832620657361ca3667d25bc90117f52030772f

        IEnumerable<SuggestedDocument> GetSuggestedDocuments(ConversationContext conversationContext);

        IEnumerable<Question> GetQuestionsFromDocument(ConversationContext conversationContext, IEnumerable<SuggestedDocument> suggestedDocuments);

        List<SuggestedDocument> FilterDocumentsByFacet(ConversationContext conversationContext, List<Facet> mustHaveFacets);

        void UpdateContextWithNewQuery(ConversationContext conversationContext, SearchQuery searchQuery);

        void UpdateContextWithNewSuggestions(ConversationContext conversationContext, List<SuggestedDocument> suggestedDocuments);

        void UpdateContextWithNewQuestions(ConversationContext conversationContext, List<Question> questions);

        bool UpdateContextWithSelectedSuggestion(ConversationContext conversationContext, Guid selectQueryId);
    }
}
