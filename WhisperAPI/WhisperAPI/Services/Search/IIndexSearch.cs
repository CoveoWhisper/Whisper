using WhisperAPI.Models.Search;

namespace WhisperAPI.Services.Search
{
    public interface IIndexSearch
    {
        ISearchResult Search(string query);
    }
}
