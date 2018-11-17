using System.Collections.Generic;

namespace WhisperAPI.Services.MLAPI.Facets
{
    public interface IFacetValues
    {
        List<Models.MLAPI.FacetValues> GetFacetValues(IEnumerable<string> facetsName);
    }
}