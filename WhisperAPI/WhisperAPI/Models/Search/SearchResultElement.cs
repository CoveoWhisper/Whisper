using Newtonsoft.Json;

namespace WhisperAPI.Models.Search
{
    public class SearchResultElement : ISearchResultElement
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("clickUri")]
        public string Uri { get; set; }

        [JsonProperty("printableUri")]
        public string PrintableUri { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }

        [JsonProperty("excerpt")]
        public string Excerpt { get; set; }
    }
}
