using Newtonsoft.Json;

namespace WhisperAPI.Models.Search
{
    public class QSearchParameters : SearchParameters
    {
        [JsonProperty(PropertyName = "q")]
        public string Q { get; set; }
    }
}
