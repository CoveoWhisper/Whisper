using System.Collections.Generic;

namespace WhisperAPI.Models
{
    public class FacetQuestion : Question
    {
        public string FacetName { get; set; }

        public List<string> FacetValues { get; set; }

        public string Answer { get; set; }

        public override string Text => $"What {this.FacetName} is it? Is it {string.Join(", ", this.FacetValues)} ?";
    }
}
