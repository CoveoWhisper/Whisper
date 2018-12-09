using System.Collections.Generic;

namespace WhisperAPI.Services.MLAPI.Facets
{
    public interface IFacetValues
    {
        List<Models.MLAPI.Facet> GetFacetValues(IEnumerable<string> facetsName);
    }
}