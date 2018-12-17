using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Services.NLPAPI
{
    public interface INlpCall
    {
        /// <summary>
        /// This methods calls NLAPI. It will be analyze and it will returns all intents and entites from it. It also returns keywords from the query
        /// </summary>
        /// <param name="searchQuery">The new query sent from agent or asker</param>
        /// <param name="relevant">Indicates if the query sent is relevant</param>
        /// <returns>NlpAnalysis containing entites, intents and keywords of the query</returns>
        NlpAnalysis AnalyzeSearchQuery(SearchQuery searchQuery, out bool relevant);
    }
}
