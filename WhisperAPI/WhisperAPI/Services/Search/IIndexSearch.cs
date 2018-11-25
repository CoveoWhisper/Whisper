using WhisperAPI.Models.Search;

namespace WhisperAPI.Services.Search
{
    public interface IIndexSearch
    {
        ISearchResult LqSearch(string query);

        ISearchResult QSearch(string query);
    }
}
