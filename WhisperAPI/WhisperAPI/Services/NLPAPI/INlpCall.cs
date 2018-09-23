using WhisperAPI.Models.NLPAPI;

namespace WhisperAPI.Services.NLPAPI
{
    public interface INlpCall
    {
        NlpAnalysis GetNlpAnalysis(string sentence);
    }
}
