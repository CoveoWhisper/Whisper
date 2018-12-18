using System.Collections.Generic;

namespace WhisperAPI.Services.MLAPI.Facets
{
    public interface IFacetValues
    {
        /// <summary>
        /// This method calls MLAPI and returns a list of facets
        /// </summary>
        /// <param name="facetsName">List of facets name</param>
        /// <returns>List of facets containing the name and the values</returns>
        List<Models.MLAPI.Facet> GetFacetValues(IEnumerable<string> facetsName);
    }
}