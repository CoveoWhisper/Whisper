using System.Collections.Generic;
using System.Threading.Tasks;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.Search;

namespace WhisperAPI.Services.Search
{
    public interface IIndexSearch
    {
        Task<ISearchResult> Search(string query, IEnumerable<Facet> mustHaveFacets);
    }
}
