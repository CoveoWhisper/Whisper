using Newtonsoft.Json;

namespace WhisperAPI.Models.Search
{
    public class SearchParameters
    {
        [JsonProperty(PropertyName = "lq")]
        public string Lq { get; set; }

        [JsonProperty(PropertyName = "numberOfResults")]
        public int NumberOfResults { get; set; }
    }
}
