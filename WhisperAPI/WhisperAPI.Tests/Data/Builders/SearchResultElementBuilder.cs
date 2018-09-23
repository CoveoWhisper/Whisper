using WhisperAPI.Models.Search;

namespace WhisperAPI.Tests.Data.Builders
{
    public class SearchResultElementBuilder
    {
        private string _title;

        private string _uri;

        private string _printableUri;

        private string _summary;

        private int _score;

        public static SearchResultElementBuilder Build => new SearchResultElementBuilder();

        public SearchResultElement Instance => new SearchResultElement
        {
            PrintableUri = this._printableUri,
            Summary = this._summary,
            Title = this._title,
            Uri = this._uri,
            Score = this._score
        };

        private SearchResultElementBuilder()
        {
            this._title = "Title";
            this._uri = "Uri";
            this._printableUri = "PrintableUri";
            this._summary = "Summary";
            this._score = 1;
        }

        public SearchResultElementBuilder WithTitle(string title)
        {
            this._title = title;
            return this;
        }

        public SearchResultElementBuilder WithUri(string uri)
        {
            this._uri = uri;
            return this;
        }

        public SearchResultElementBuilder WithPrintableUri(string printableUri)
        {
            this._printableUri = printableUri;
            return this;
        }

        public SearchResultElementBuilder WithSummary(string summary)
        {
            this._summary = summary;
            return this;
        }

        public SearchResultElementBuilder WithScore(int score)
        {
            this._score = score;
            return this;
        }
    }
}
