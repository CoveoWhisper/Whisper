using System.Collections.Generic;

namespace WhisperAPI.Models.NLPAPI
{
    public class NlpAnalysis
    {
        public List<Intent> Intents { get; set; }

        public List<Entity> Entities { get; set; }

        public string ParsedQuery { get; set; }
    }
}
