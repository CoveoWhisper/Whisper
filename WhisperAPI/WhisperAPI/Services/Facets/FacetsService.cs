using System.Collections.Generic;
using WhisperAPI.Services.MLAPI.Facets;
using FacetValues = WhisperAPI.Models.MLAPI.FacetValues;

namespace WhisperAPI.Services.Facets
{
    public class FacetsService : IFacetsService
    {
        private readonly IFacetValues _facetValues;

        public FacetsService(IFacetValues facetValues)
        {
            this._facetValues = facetValues;
        }

        public List<FacetValues> GetFacetValues(IEnumerable<string> facetsName)
        {
            return this._facetValues.GetFacetValues(facetsName);
        }
    }
}
