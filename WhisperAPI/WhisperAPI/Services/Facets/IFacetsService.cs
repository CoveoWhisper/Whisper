using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Services.Facets
{
    public interface IFacetsService
    {
        /// <summary>
        /// Returns all values of the facets name
        /// </summary>
        /// <param name="facetsName">List of facets name</param>
        /// <returns>List of facets</returns>
        List<Facet> GetFacetValues(IEnumerable<string> facetsName);
    }
}
