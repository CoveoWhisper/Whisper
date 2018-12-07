using System.Collections.Generic;

namespace WhisperAPI.Models.MLAPI
{
    public class NearestDocumentsParameters
    {
        public HashSet<string> ContextEntities { get; set; }

        public IEnumerable<string> DocumentsUri { get; set; }
    }
}
