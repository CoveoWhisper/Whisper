using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using WhisperAPI.Settings;

namespace WhisperAPI.Models.Queries
{
    public class SuggestionQuery : Query
    {
        [Required(ErrorMessage = "MaxDocuments is required")]
        [Range(0, int.MaxValue)]
        [JsonProperty("maxDocuments")]
        public int MaxDocuments { get; set; }

        [Required(ErrorMessage = "MaxQuestions is required")]
        [Range(0, int.MaxValue)]
        [JsonProperty("maxQuestions")]
        public int MaxQuestions { get; set; }

        [JsonProperty("overridenRecommenderSettings")]
        public RecommenderSettings OverridenRecommenderSettings { get; set; }
    }
}
