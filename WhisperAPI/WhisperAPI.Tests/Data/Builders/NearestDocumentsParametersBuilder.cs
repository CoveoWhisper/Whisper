using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Tests.Data.Builders
{
    public class NearestDocumentsParametersBuilder
    {
        private List<string> _documentsUri;

        private HashSet<string> _contextEntities;

        public static NearestDocumentsParametersBuilder Build => new NearestDocumentsParametersBuilder();

        public NearestDocumentsParameters Instance => new NearestDocumentsParameters
        {
            ContextEntities = this._contextEntities,
            DocumentsUri = this._documentsUri
        };

        private NearestDocumentsParametersBuilder()
        {
            this._contextEntities = new HashSet<string>();
            this._documentsUri = new List<string>();
        }

        public NearestDocumentsParametersBuilder WithContextEntities(HashSet<string> contextEntities)
        {
            this._contextEntities = contextEntities;
            return this;
        }

        public NearestDocumentsParametersBuilder WithDocumentsUri(List<string> documentsUri)
        {
            this._documentsUri = documentsUri;
            return this;
        }

        public NearestDocumentsParametersBuilder AddContextEntity(string contextEntity)
        {
            this._contextEntities.Add(contextEntity);
            return this;
        }

        public NearestDocumentsParametersBuilder AddDocumentUri(string documentUri)
        {
            this._documentsUri.Add(documentUri);
            return this;
        }
    }
}
