using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Models
{
    public class ContextItem
    {
        public SearchQuery SearchQuery { get; set; }

        public NlpAnalysis NlpAnalysis { get; set; }
    }
}
