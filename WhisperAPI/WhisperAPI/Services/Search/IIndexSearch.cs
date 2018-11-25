using System.Threading.Tasks;
using WhisperAPI.Models.Search;

namespace WhisperAPI.Services.Search
{
    public interface IIndexSearch
    {
        Task<ISearchResult> LqSearch(string query);

        Task<ISearchResult> QSearch(string query);
    }
}
