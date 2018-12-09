using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Services.MLAPI.Facets
{
    public interface IDocumentFacets
    {
        List<FacetQuestionResult> GetQuestions(IEnumerable<string> documentsUri);
    }
}