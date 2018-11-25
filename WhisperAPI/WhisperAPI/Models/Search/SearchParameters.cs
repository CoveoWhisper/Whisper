﻿using Newtonsoft.Json;

namespace WhisperAPI.Models.Search
{
    public class SearchParameters
    {
        [JsonProperty(PropertyName = "numberOfResults")]
        public int NumberOfResults { get; set; }
    }
}
