using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Services.Facets
{
    public interface IFacetsService
    {
        List<Facet> GetFacetValues(IEnumerable<string> facetsName);
    }
}
