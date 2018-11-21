using System;
using System.Collections.Generic;
using System.Linq;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Models
{
    public class ConversationContext
    {
        public ConversationContext(Guid chatKey, DateTime datetime)
            : this()
        {
            this.ChatKey = chatKey;
            this.StartDate = datetime;
        }

        public ConversationContext()
        {
            this.ContextItems = new List<ContextItem>();
            this.SuggestedDocuments = new HashSet<Document>();
            this.LastSuggestedQuestions = new List<Question>();
            this.Questions = new HashSet<Question>();
            this.SelectedSuggestedDocuments = new HashSet<Document>();
            this.FilterDocumentsParameters = new FilterDocumentsParameters();
        }

        public Guid ChatKey { get; set; }

        public DateTime StartDate { get; set; }

        public List<ContextItem> ContextItems { get; set; }

        public HashSet<Document> SuggestedDocuments { get; set; }

        public HashSet<Document> SelectedSuggestedDocuments { get; set; }

        public HashSet<Question> Questions { get; set; }

        public IReadOnlyList<Question> ClickedQuestions => this.Questions.Where(q => q.Status == QuestionStatus.Clicked).ToList();

        public IReadOnlyList<Question> AnswerPendingQuestions => this.Questions.Where(q => q.Status == QuestionStatus.AnswerPending).ToList();

        public IReadOnlyList<Question> AnsweredQuestions => this.Questions.Where(q => q.Status == QuestionStatus.Answered).ToList();

        public List<Question> LastSuggestedQuestions { get; set; }

        public FilterDocumentsParameters FilterDocumentsParameters { get; set; }
    }
}
