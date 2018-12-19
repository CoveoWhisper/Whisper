using System;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Tests.Data.Builders
{
    public class FilterQueryBuilder
    {
        private Guid _chatKey;

        private Facet _facet;

        public static FilterQueryBuilder Build => new FilterQueryBuilder();

        public FilterQuery Instance => new FilterQuery
        {
            ChatKey = this._chatKey,
            Facet = this._facet
        };

        private FilterQueryBuilder()
        {
            this._chatKey = Guid.NewGuid();
            this._facet = FacetBuilder.Build.Instance;
        }

        public FilterQueryBuilder WithChatKey(Guid chatKey)
        {
            this._chatKey = chatKey;
            return this;
        }

        public FilterQueryBuilder WithFacet(Facet facet)
        {
            this._facet = facet;
            return this;
        }
    }
}
