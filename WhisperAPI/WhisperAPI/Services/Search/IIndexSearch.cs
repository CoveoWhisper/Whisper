using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.Search;

namespace WhisperAPI.Services.Search
{
    public interface IIndexSearch
    {
        ISearchResult Search(string query, IEnumerable<Facet> mustHaveFacets);
    }
}
