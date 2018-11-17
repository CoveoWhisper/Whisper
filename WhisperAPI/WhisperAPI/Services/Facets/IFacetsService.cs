using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Services.Facets
{
    public interface IFacetsService
    {
        List<FacetValues> GetFacetValues(IEnumerable<string> facetsName);
    }
}
