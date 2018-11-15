using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace WhisperAPI.Models.Queries
{
    public class SearchQuery : SuggestionQuery
    {
        public enum MessageType
        {
            Customer = 0,
            Agent = 1
        }

        [JsonIgnore]
        public int Id { get; set; }

        [Required(ErrorMessage = "Query is required")]
        [JsonProperty("query")]
        public string Query { get; set; }

        [Required(ErrorMessage = "Type is required")]
        [JsonProperty("type")]
        public MessageType? Type { get; set; }

        [JsonIgnore]
        public bool Relevant { get; set; }
    }
}