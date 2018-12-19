using System.Collections.Generic;

namespace WhisperAPI.Models.MLAPI
{
    public class FilterDocumentsParameters
    {
        public List<string> Documents { get; set; }

        public List<Facet> MustHaveFacets { get; set; }

        public List<Facet> MustNotHaveFacets { get; set; }

        public FilterDocumentsParameters()
        {
            this.Documents = new List<string>();
            this.MustHaveFacets = new List<Facet>();
            this.MustNotHaveFacets = new List<Facet>();
        }
    }
}
