using WhisperAPI.Models;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Tests.Data.Builders
{
    public class LastClickAnalyticsResultsBuilder
    {
        private Document _document;

        private double _score;

        public static LastClickAnalyticsResultsBuilder Build => new LastClickAnalyticsResultsBuilder();

        public LastClickAnalyticsResults Instance => new LastClickAnalyticsResults
        {
            Document = this._document,
            Score = this._score
        };

        private LastClickAnalyticsResultsBuilder()
        {
            this._document = new Document();
            this._score = 0;
        }

        public LastClickAnalyticsResultsBuilder WithDocument(Document document)
        {
            this._document = document;
            return this;
        }

        public LastClickAnalyticsResultsBuilder WithScore(double score)
        {
            this._score = score;
            return this;
        }
    }
}
