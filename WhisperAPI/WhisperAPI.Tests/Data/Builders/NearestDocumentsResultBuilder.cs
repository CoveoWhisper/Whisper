using WhisperAPI.Models;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Tests.Data.Builders
{
    public class NearestDocumentsResultBuilder
    {
        private Document _document;

        private double _score;

        public static NearestDocumentsResultBuilder Build => new NearestDocumentsResultBuilder();

        public NearestDocumentsResult Instance => new NearestDocumentsResult
        {
            Document = this._document,
            Score = this._score
        };

        private NearestDocumentsResultBuilder()
        {
            this._document = DocumentBuilder.Build.Instance;
            this._score = 0.75;
        }

        public NearestDocumentsResultBuilder WithDocument(Document document)
        {
            this._document = document;
            return this;
        }

        public NearestDocumentsResultBuilder WithScore(double score)
        {
            this._score = score;
            return this;
        }
    }
}
