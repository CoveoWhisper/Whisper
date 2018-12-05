using System;
using WhisperAPI.Models.Queries;
using WhisperAPI.Settings;

namespace WhisperAPI.Tests.Data.Builders
{
    public class SearchQueryBuilder
    {
        private Guid? _chatKey;

        private string _query;

        private SearchQuery.MessageType _type;

        private int _maxDocuments;

        private int _maxQuestions;

        private RecommenderSettings _overridenRecommenderSettings;

        public static SearchQueryBuilder Build => new SearchQueryBuilder();

        public SearchQuery Instance => new SearchQuery
        {
            ChatKey = this._chatKey,
            Query = this._query,
            Type = this._type,
            MaxDocuments = this._maxDocuments,
            MaxQuestions = this._maxQuestions,
            OverridenRecommenderSettings = this._overridenRecommenderSettings
        };

        private SearchQueryBuilder()
        {
            this._chatKey = Guid.NewGuid();
            this._query = "Test";
            this._type = SearchQuery.MessageType.Customer;
            this._maxDocuments = 10;
            this._maxQuestions = 10;
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

        public SearchQueryBuilder WithMessageType(SearchQuery.MessageType type)
        {
            this._type = type;
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

        public SearchQueryBuilder WithOverridenRecommenderSettings(RecommenderSettings overridenRecommenderSettings)
        {
            this._overridenRecommenderSettings = overridenRecommenderSettings;
            return this;
        }
    }
}
