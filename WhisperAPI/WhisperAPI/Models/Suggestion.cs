using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Models
{
    public class Suggestion
    {
        public List<Recommendation<QuestionToClient>> Questions { get; set; }

        public List<Recommendation<Document>> Documents { get; set; }

        public List<Facet> ActiveFacets { get; set; }
    }
}