using System.Collections.Generic;
using System.Threading.Tasks;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.Search;

namespace WhisperAPI.Services.Search
{
    public interface IIndexSearch
    {
        Task<ISearchResult> LqSearch(string query, IEnumerable<Facet> mustHaveFacets);

        Task<ISearchResult> QSearch(string query, IEnumerable<Facet> mustHaveFacets);
    }
}
