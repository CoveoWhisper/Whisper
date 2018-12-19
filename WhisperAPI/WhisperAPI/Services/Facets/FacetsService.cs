using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Services.MLAPI.Facets;

namespace WhisperAPI.Services.Facets
{
    public class FacetsService : IFacetsService
    {
        private readonly IFacetValues _facetValues;

        public FacetsService(IFacetValues facetValues)
        {
            this._facetValues = facetValues;
        }

        public List<Facet> GetFacetValues(IEnumerable<string> facetsName)
        {
            return this._facetValues.GetFacetValues(facetsName);
        }
    }
}
