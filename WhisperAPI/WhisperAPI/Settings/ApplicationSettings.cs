﻿using System.Collections.Generic;

namespace WhisperAPI.Settings
{
    public class ApplicationSettings
    {
        public string ApiKey { get; set; }

        public string NlpApiBaseAddress { get; set; }

        public List<string> Intents { get; set; }
    }
}
