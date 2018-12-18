using System.Collections.Generic;
using System.Threading.Tasks;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Services.MLAPI.LastClickAnalytics
{
    public interface ILastClickAnalytics
    {
        /// <summary>
        /// This method returns all documents from analytics in MLAPI
        /// </summary>
        /// <param name="contextEntities">list of keywords extract from the conversation</param>
        /// <returns>All documents relevant with their score</returns>
        Task<List<LastClickAnalyticsResult>> GetLastClickAnalyticsResults(HashSet<string> contextEntities);
    }
}