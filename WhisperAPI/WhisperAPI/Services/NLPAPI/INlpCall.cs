using WhisperAPI.Models.Queries;

namespace WhisperAPI.Services.NLPAPI
{
    public interface INlpCall
    {
        void UpdateAndAnalyseSearchQuery(SearchQuery searchQuery);
    }
}
