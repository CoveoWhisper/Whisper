using System;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Tests.Data.Builders
{
    public class QueryBuilder
    {
        private Guid? _chatKey;

        public static QueryBuilder Build => new QueryBuilder();

        public Query Instance => new Query
        {
            ChatKey = this._chatKey
        };

        private QueryBuilder()
        {
            this._chatKey = Guid.NewGuid();
        }

        public QueryBuilder WithChatKey(Guid? chatKey)
        {
            this._chatKey = chatKey;
            return this;
        }
    }
}
