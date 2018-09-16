using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace WhisperAPI.Models
{
    public class SelectSuggestionPost
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "ChatKey is required")]
        [JsonProperty("chatkey")]
        public Guid? ChatKey { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Id is required")]
        [JsonProperty("id")]
        public Guid? Id { get; set; }
    }
}