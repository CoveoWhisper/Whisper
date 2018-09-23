using System.Collections.Generic;
using Newtonsoft.Json;

namespace WhisperAPI.Models.Search
{
    public class SearchResult : ISearchResult
    {
        [JsonProperty("results")]
        private IEnumerable<SearchResultElement> _elements;

        [JsonProperty("totalCount")]
        public int NbrElements { get; set; }

        public IEnumerable<ISearchResultElement> Elements { get => this._elements; set => this._elements = (IEnumerable<SearchResultElement>)value; }
    }
}
