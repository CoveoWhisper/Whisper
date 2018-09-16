using System;
using System.Collections.Generic;

namespace WhisperAPI.Models
{
    public class ConversationContext
    {
        public ConversationContext(Guid chatkey, DateTime datetime)
            : this()
        {
            this.ChatKey = chatkey;
            this.StartDate = datetime;
        }

        public ConversationContext()
        {
            this.SearchQueries = new List<SearchQuery>();
            this.SuggestedDocuments = new HashSet<SuggestedDocument>();
            this.Questions = new HashSet<Question>();
            this.SelectedSuggestedDocuments = new HashSet<SuggestedDocument>();
            this.SelectedQuestions = new HashSet<Question>();
        }

        public Guid ChatKey { get; set; }

        public DateTime StartDate { get; set; }

        public List<SearchQuery> SearchQueries { get; set; }

        public HashSet<SuggestedDocument> SuggestedDocuments { get; set; }

        public HashSet<Question> Questions { get; set; }

        public HashSet<SuggestedDocument> SelectedSuggestedDocuments { get; set; }

        public HashSet<Question> SelectedQuestions { get; set; }
    }
}
