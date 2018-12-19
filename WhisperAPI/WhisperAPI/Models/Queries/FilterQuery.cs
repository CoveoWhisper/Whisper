using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Models.Queries
{
    public class FilterQuery : Query
    {
        public Facet Facet { get; set; }
    }
}
