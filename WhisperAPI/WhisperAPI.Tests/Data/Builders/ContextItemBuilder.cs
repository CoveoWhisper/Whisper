using System;
using WhisperAPI.Models;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Tests.Data.Builders
{
    public class ContextItemBuilder
    {
        private NlpAnalysis _nlpAnalysis;

        private SearchQuery _searchQuery;

        public static ContextItemBuilder Build => new ContextItemBuilder();

        public ContextItem Instance => new ContextItem
        {
            NlpAnalysis = this._nlpAnalysis,
            SearchQuery = this._searchQuery,
        };

        private ContextItemBuilder()
        {
            this._nlpAnalysis = NlpAnalysisBuilder.Build.Instance;
            this._searchQuery = SearchQueryBuilder.Build.Instance;
        }

        public ContextItemBuilder WithSearchQuery(SearchQuery searchQuery)
        {
            this._searchQuery = searchQuery;
            return this;
        }

        public ContextItemBuilder WithNlpAnalysis(NlpAnalysis nlpAnalysis)
        {
            this._nlpAnalysis = nlpAnalysis;
            return this;
        }
    }
}
