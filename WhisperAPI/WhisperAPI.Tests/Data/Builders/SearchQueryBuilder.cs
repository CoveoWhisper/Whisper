using System;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Tests.Data.Builders
{
    public class SearchQueryBuilder
    {
        private Guid? _chatKey;

        private string _query;

        private string _preProcessedQuery;

        private SearchQuery.MessageType _type;

        private bool _relevant;

        private int _maxDocuments;

        private int _maxQuestions;

        public static SearchQueryBuilder Build => new SearchQueryBuilder();

        public SearchQuery Instance => new SearchQuery
        {
            ChatKey = this._chatKey,
            Query = this._query,
            FilteredQuery = this._preProcessedQuery,
            Type = this._type,
            MaxDocuments = this._maxDocuments,
            MaxQuestions = this._maxQuestions,
            Relevant = this._relevant
        };

        private SearchQueryBuilder()
        {
            this._chatKey = Guid.NewGuid();
            this._query = "Test";
            this._preProcessedQuery = "Test";
            this._type = SearchQuery.MessageType.Customer;
            this._maxDocuments = 10;
            this._maxQuestions = 10;
            this._relevant = false;
        }

        public SearchQueryBuilder WithChatKey(Guid? chatKey)
        {
            this._chatKey = chatKey;
            return this;
        }

        public SearchQueryBuilder WithQuery(string query)
        {
            this._query = query;
            return this;
        }

        public SearchQueryBuilder WithPreProcessedQuery(string query)
        {
            this._preProcessedQuery = query;
            return this;
        }

        public SearchQueryBuilder WithMessageType(SearchQuery.MessageType type)
        {
            this._type = type;
            return this;
        }

        public SearchQueryBuilder WithRelevant(bool relevant)
        {
            this._relevant = relevant;
            return this;
        }

        public SearchQueryBuilder WithMaxDocuments(int maxDocuments)
        {
            this._maxDocuments = maxDocuments;
            return this;
        }

        public SearchQueryBuilder WithMaxQuestions(int maxQuestions)
        {
            this._maxQuestions = maxQuestions;
            return this;
        }
    }
}
