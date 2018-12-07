using System.Collections.Generic;
using System.Threading.Tasks;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Services.MLAPI.NearestDocuments
{
    public interface INearestDocuments
    {
        Task<List<NearestDocumentsResult>> GetNearestDocumentsResults(NearestDocumentsParameters parameters);
    }
}