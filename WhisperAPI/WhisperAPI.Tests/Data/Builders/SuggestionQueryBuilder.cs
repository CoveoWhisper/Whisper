using System;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Tests.Data.Builders
{
    public class SuggestionQueryBuilder
    {
        private Guid? _chatKey;

        private int _maxDocuments;

        private int _maxQuestions;

        public static SuggestionQueryBuilder Build => new SuggestionQueryBuilder();

        public SuggestionQuery Instance => new SuggestionQuery
        {
            ChatKey = this._chatKey,
            MaxDocuments = this._maxDocuments,
            MaxQuestions = this._maxQuestions
        };

        private SuggestionQueryBuilder()
        {
            this._chatKey = Guid.NewGuid();
            this._maxDocuments = 10;
            this._maxQuestions = 10;
        }

        public SuggestionQueryBuilder WithChatKey(Guid? chatKey)
        {
            this._chatKey = chatKey;
            return this;
        }

        public SuggestionQueryBuilder WithMaxDocuments(int maxDocuments)
        {
            this._maxDocuments = maxDocuments;
            return this;
        }

        public SuggestionQueryBuilder WithMaxQuestions(int maxQuestions)
        {
            this._maxQuestions = maxQuestions;
            return this;
        }
    }
}
