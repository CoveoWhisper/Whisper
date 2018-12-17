using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Services.MLAPI.Facets
{
    public interface IFilterDocuments
    {
        /// <summary>
        /// This method calls MLAPI and filters documents
        /// </summary>
        /// <param name="parameters">Parameters to send to MLAPI</param>
        /// <returns>Uris of documents filtered</returns>
        List<string> FilterDocumentsByFacets(FilterDocumentsParameters parameters);
    }
}