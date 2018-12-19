using System.Collections.Generic;
using System.Threading.Tasks;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Services.MLAPI.NearestDocuments
{
    public interface INearestDocuments
    {
        /// <summary>
        /// This method returns all documents that are near the conversation
        /// </summary>
        /// <param name="parameters">Parameters to send to MLAPI</param>
        /// <returns>All documents relevant with their score</returns>
        Task<List<NearestDocumentsResult>> GetNearestDocumentsResults(NearestDocumentsParameters parameters);
    }
}