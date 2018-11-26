using Newtonsoft.Json;

namespace WhisperAPI.Models.Search
{
    public class LqSearchParameters : SearchParameters
    {
        [JsonProperty(PropertyName = "lq")]
        public string Lq { get; set; }
    }
}
