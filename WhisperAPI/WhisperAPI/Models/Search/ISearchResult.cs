using System.Collections.Generic;

namespace WhisperAPI.Models.Search
{
    public interface ISearchResult
    {
        int NbrElements { get; set; }

        IEnumerable<ISearchResultElement> Elements { get; set; }
    }
}
