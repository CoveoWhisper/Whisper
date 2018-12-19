using System.Collections.Generic;
using System.Threading.Tasks;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.Search;

namespace WhisperAPI.Services.Search
{
    public interface IIndexSearch
    {
        /// <summary>
        /// This method calls Coveo with a Lq search
        /// </summary>
        /// <param name="query">The query to send to Coveo</param>
        /// <param name="mustHaveFacets">List of facet to filter documents</param>
        /// <returns>Returns all documents from Coveo</returns>
        Task<ISearchResult> LqSearch(string query, IEnumerable<Facet> mustHaveFacets);

        /// <summary>
        /// This method calls Coveo with a Q search
        /// </summary>
        /// <param name="query">The query to send to Coveo</param>
        /// <param name="mustHaveFacets">List of facet to filter documents</param>
        /// <returns>Returns all documents from Coveo</returns>
        Task<ISearchResult> QSearch(string query, IEnumerable<Facet> mustHaveFacets);
    }
}
