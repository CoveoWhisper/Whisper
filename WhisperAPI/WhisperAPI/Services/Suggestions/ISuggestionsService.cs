﻿using System;
using System.Collections.Generic;
using WhisperAPI.Models;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Services.Suggestions
{
    public interface ISuggestionsService
    {
        IEnumerable<SuggestedDocument> GetSuggestedDocuments(ConversationContext conversationContext);

        void UpdateContextWithNewQuery(ConversationContext conversationContext, SearchQuery searchQuery);

        void UpdateContextWithNewSuggestions(ConversationContext conversationContext, List<SuggestedDocument> suggestedDocuments);

        bool UpdateContextWithSelectedSuggestion(ConversationContext conversationContext, Guid selectQueryId);
    }
}
