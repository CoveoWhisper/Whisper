using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WhisperAPI.Models;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Search;
using WhisperAPI.Services.MLAPI.Facets;
using WhisperAPI.Services.NLPAPI;
using WhisperAPI.Services.Search;

namespace WhisperAPI.Services.Suggestions
{
    public class SuggestionsService : ISuggestionsService
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IIndexSearch _indexSearch;

        private readonly INlpCall _nlpCall;

        private readonly IDocumentFacets _documentFacets;

        private readonly List<string> _irrelevantIntents;

        public SuggestionsService(IIndexSearch indexSearch, INlpCall nlpCall, IDocumentFacets documentFacets, List<string> irrelevantIntents)
        {
            this._indexSearch = indexSearch;
            this._nlpCall = nlpCall;
            this._documentFacets = documentFacets;
            this._irrelevantIntents = irrelevantIntents;
        }

        public IEnumerable<SuggestedDocument> GetSuggestedDocuments(ConversationContext conversationContext)
        {
            var allRelevantQueries = string.Join(" ", conversationContext.SearchQueries.Where(x => x.Relevant).Select(m => m.Query));

            if (string.IsNullOrEmpty(allRelevantQueries.Trim()))
            {
                return new List<SuggestedDocument>();
            }

            var coveoIndexDocuments = this.SearchCoveoIndex(allRelevantQueries);

            return this.FilterOutChosenSuggestions(coveoIndexDocuments, conversationContext.SearchQueries);
        }

        public List<Question> GetQuestionsFromDocument(IEnumerable<SuggestedDocument> suggestedDocuments)
        {
            return this._documentFacets.GetQuestions(suggestedDocuments);
        }

        public void UpdateContextWithNewQuery(ConversationContext context, SearchQuery searchQuery)
        {
            searchQuery.Relevant = this.IsQueryRelevant(searchQuery);
            context.SearchQueries.Add(searchQuery);
        }

        public void UpdateContextWithNewSuggestions(ConversationContext context, List<SuggestedDocument> suggestedDocuments)
        {
            foreach (var suggestedDocument in suggestedDocuments)
            {
                context.SuggestedDocuments.Add(suggestedDocument);
            }
        }

        public bool IsQueryRelevant(SearchQuery searchQuery)
        {
            var nlpAnalysis = this._nlpCall.GetNlpAnalysis(searchQuery.Query);

            nlpAnalysis.Intents.ForEach(x => Log.Debug($"Intent - Name: {x.Name}, Confidence: {x.Confidence}"));
            nlpAnalysis.Entities.ForEach(x => Log.Debug($"Entity - Name: {x.Name}"));
            return this.IsIntentRelevant(nlpAnalysis);
        }

        public IEnumerable<SuggestedDocument> FilterOutChosenSuggestions(
            IEnumerable<SuggestedDocument> coveoIndexDocuments,
            IEnumerable<SearchQuery> queriesList)
        {
            var queries = queriesList
                .Select(x => x.Query)
                .ToList();

            return coveoIndexDocuments.Where(x => !queries.Any(y => y.Contains(x.Uri)));
        }

        private IEnumerable<SuggestedDocument> SearchCoveoIndex(string query)
        {
            ISearchResult searchResult = this._indexSearch.Search(query);
            var documents = new List<SuggestedDocument>();

            if (searchResult == null)
            {
                // error null result
                return documents;
            }

            if (searchResult.Elements == null)
            {
                // error null result elements
                return documents;
            }

            foreach (var result in searchResult.Elements)
            {
                if (this.IsElementValid(result))
                {
                    documents.Add(new SuggestedDocument(result));
                }
            }

            return documents;
        }

        private bool IsIntentRelevant(NlpAnalysis nlpAnalysis)
        {
            var mostConfidentIntent = nlpAnalysis.Intents.OrderByDescending(x => x.Confidence).First();
            return !this._irrelevantIntents.Any(x => Regex.IsMatch(mostConfidentIntent.Name, this.WildCardToRegularExpression(x)));
        }

        private bool IsElementValid(ISearchResultElement result)
        {
            if (result?.Title == null || result?.Uri == null || result?.PrintableUri == null)
            {
                // error null attributes
                return false;
            }

            return true;
        }

        private string WildCardToRegularExpression(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }
    }
}
