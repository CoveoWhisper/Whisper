using System.Threading.Tasks;
using WhisperAPI.Models.Search;

namespace WhisperAPI.Services.Search
{
    public interface IIndexSearch
    {
        Task<ISearchResult> Search(string query);
    }
}
