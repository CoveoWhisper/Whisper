namespace WhisperAPI.Models.Search
{
    public interface ISearchResultElement
    {
        string Title { get; set; }

        string Uri { get; set; }

        string PrintableUri { get; set; }

        string Summary { get; set; }

        int Score { get; set; }

        string Excerpt { get; set; }
    }
}
