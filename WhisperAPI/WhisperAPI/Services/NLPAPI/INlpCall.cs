using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Services.NLPAPI
{
    public interface INlpCall
    {
        NlpAnalysis AnalyseSearchQuery(SearchQuery searchQuery);
    }
}
