using System;
using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Tests.Data.Builders
{
    public class NearestDocumentsParametersBuilder
    {
        private List<string> _documentsUri;

        private string _parsedQuery;

        public static NearestDocumentsParametersBuilder Build => new NearestDocumentsParametersBuilder();

        public NearestDocumentsParameters Instance => new NearestDocumentsParameters
        {
            ParsedQuery = this._parsedQuery,
            DocumentsUri = this._documentsUri
        };

        private NearestDocumentsParametersBuilder()
        {
            this._parsedQuery = string.Empty;
            this._documentsUri = new List<string>();
        }

        public NearestDocumentsParametersBuilder WithParsedQuery(string parsedQuery)
        {
            this._parsedQuery = parsedQuery;
            return this;
        }

        public NearestDocumentsParametersBuilder WithDocumentsUri(List<string> documentsUri)
        {
            this._documentsUri = documentsUri;
            return this;
        }

        public NearestDocumentsParametersBuilder AddDocumentUri(string documentUri)
        {
            this._documentsUri.Add(documentUri);
            return this;
        }
    }
}
