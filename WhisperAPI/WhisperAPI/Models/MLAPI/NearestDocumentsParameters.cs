using System.Collections.Generic;

namespace WhisperAPI.Models.MLAPI
{
    public class NearestDocumentsParameters
    {
        public string ParsedQuery { get; set; }

        public IEnumerable<string> DocumentsUri { get; set; }
    }
}
