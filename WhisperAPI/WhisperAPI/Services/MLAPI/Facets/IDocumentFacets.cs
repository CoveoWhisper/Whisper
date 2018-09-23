using System.Collections.Generic;
using WhisperAPI.Models;

namespace WhisperAPI.Services.MLAPI.Facets
{
    public interface IDocumentFacets
    {
        List<Question> GetQuestions(IEnumerable<SuggestedDocument> suggestedDocuments);
    }
}