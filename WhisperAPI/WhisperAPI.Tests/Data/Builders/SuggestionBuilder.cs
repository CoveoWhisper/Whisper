using System.Collections.Generic;
using WhisperAPI.Models;

namespace WhisperAPI.Tests.Data.Builders
{
    public class SuggestionBuilder
    {
        private List<Recommendation<QuestionToClient>> _questions;

        private List<Recommendation<Document>> _documents;

        public static SuggestionBuilder Build => new SuggestionBuilder();

        public Suggestion Instance => new Suggestion
        {
            Documents = this._documents,
            Questions = this._questions
        };

        private SuggestionBuilder()
        {
            this._questions = new List<Recommendation<QuestionToClient>>();
            this._documents = new List<Recommendation<Document>>();
        }

        public SuggestionBuilder WithQuestions(List<Recommendation<QuestionToClient>> questions)
        {
            this._questions = questions;
            return this;
        }

        public SuggestionBuilder WithDocuments(List<Recommendation<Document>> documents)
        {
            this._documents = documents;
            return this;
        }
    }
}
