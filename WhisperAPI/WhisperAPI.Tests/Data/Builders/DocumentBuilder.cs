using System;
using WhisperAPI.Models;

namespace WhisperAPI.Tests.Data.Builders
{
    public class DocumentBuilder
    {
        private Guid _id;

        private string _title;

        private string _uri;

        private string _printableUri;

        private string _summary;

        private string _excerpt;

        public static DocumentBuilder Build => new DocumentBuilder();

        public Document Instance => new Document
        {
            Id = this._id,
            Summary = this._summary,
            Title = this._title,
            Uri = this._uri,
            PrintableUri = this._printableUri,
            Excerpt = this._excerpt
        };

        public DocumentBuilder()
        {
            this._id = Guid.NewGuid();
            this._summary = "Summary";
            this._uri = "Uri";
            this._excerpt = "Excerpt";
            this._printableUri = "PrintableUri";
            this._title = "Title";
        }

        public DocumentBuilder WithId(Guid id)
        {
            this._id = id;
            return this;
        }

        public DocumentBuilder WithTitle(string title)
        {
            this._title = title;
            return this;
        }

        public DocumentBuilder WithSummary(string summary)
        {
            this._summary = summary;
            return this;
        }

        public DocumentBuilder WithExcerpt(string excerpt)
        {
            this._excerpt = excerpt;
            return this;
        }

        public DocumentBuilder WithUri(string uri)
        {
            this._uri = uri;
            return this;
        }

        public DocumentBuilder WithPrintableUri(string printableUri)
        {
            this._printableUri = printableUri;
            return this;
        }
    }
}
