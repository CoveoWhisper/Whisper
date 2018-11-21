using System.Collections.Generic;

namespace WhisperAPI.Models.Queries
{
    public class FacetQuery : Query
    {
        public List<string> FacetsName { get; set; }
    }
}
