using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WhisperAPI.Models;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;
using WhisperAPI.Models.Search;
using WhisperAPI.Services.MLAPI.Facets;
using WhisperAPI.Services.MLAPI.LastClickAnalytics;
using WhisperAPI.Services.MLAPI.NearestDocuments;
using WhisperAPI.Services.Search;
using WhisperAPI.Settings;

[assembly: InternalsVisibleTo("WhisperAPI.Tests")]

namespace WhisperAPI.Services.Suggestions
{
    public class SuggestionsService : ISuggestionsService
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IIndexSearch _indexSearch;

        private readonly ILastClickAnalytics _lastClickAnalytics;

        private readonly IDocumentFacets _documentFacets;

        private readonly INearestDocuments _nearestDocuments;

        private readonly IFilterDocuments _filterDocuments;

        private readonly int _numberOfWordsIntoQ;

        private readonly double _minimumConfidence;

        private RecommenderSettings _recommenderSettings;

        public SuggestionsService(
            IIndexSearch indexSearch,
            ILastClickAnalytics lastClickAnalytics,
            IDocumentFacets documentFacets,
            INearestDocuments nearestDocuments,
            IFilterDocuments filterDocuments,
            int numberOfWordsIntoQ,
            double minimumConfidence,
            RecommenderSettings recommenderSettings)
        {
            this._indexSearch = indexSearch;
            this._lastClickAnalytics = lastClickAnalytics;
            this._documentFacets = documentFacets;
            this._nearestDocuments = nearestDocuments;
            this._filterDocuments = filterDocuments;
            this._numberOfWordsIntoQ = numberOfWordsIntoQ;
            this._minimumConfidence = minimumConfidence;
            this._recommenderSettings = recommenderSettings;
        }

        public Suggestion GetNewSuggestion(ConversationContext conversationContext, SuggestionQuery query)
        {
            var allRecommendedQuestions = new List<IEnumerable<Recommendation<Question>>>();

            if (query.OverridenRecommenderSettings != null)
            {
                this._recommenderSettings = query.OverridenRecommenderSettings;
            }

            var tasks = new Dictionary<RecommenderType, Task<IEnumerable<Recommendation<Document>>>>();

            if (this._recommenderSettings.UseLongQuerySearchRecommender)
            {
                tasks.Add(RecommenderType.LongQuerySearch, this.GetLongQuerySearchRecommendations(conversationContext));
            }

            if (this._recommenderSettings.UsePreprocessedQuerySearchRecommender)
            {
                tasks.Add(RecommenderType.PreprocessedQuerySearch, this.GetQuerySearchRecommendations(conversationContext));
            }

            if (this._recommenderSettings.UseAnalyticsSearchRecommender)
            {
                tasks.Add(RecommenderType.LastClickAnalytics, this.GetLastClickAnalyticsRecommendations(conversationContext));
            }

            if (this._recommenderSettings.UseNearestDocumentsRecommender)
            {
                var documentsUri = this._recommenderSettings.UsePreprocessedQuerySearchRecommender
                    ? tasks[RecommenderType.PreprocessedQuerySearch].Result
                    : this.GetQuerySearchRecommendations(conversationContext).Result;

                tasks.Add(RecommenderType.NearestDocuments, this.GetNearestDocumentsRecommendations(conversationContext, documentsUri.Select(r => r.Value.Uri)));
            }

            var allRecommendedDocuments = Task.WhenAll(tasks.Values).Result.ToList();
            var mergedDocuments = this.MergeRecommendedDocuments(allRecommendedDocuments);

            if (mergedDocuments.Any() && this._recommenderSettings.UseFacetQuestionRecommender)
            {
                allRecommendedQuestions.Add(this.GenerateQuestions(conversationContext, mergedDocuments.Select(d => d.Value)).Take(query.MaxQuestions));
            }

            var mergedQuestions = this.MergeRecommendedQuestions(allRecommendedQuestions).Take(query.MaxQuestions);

            var suggestion = new Suggestion
            {
                ActiveFacets = conversationContext.MustHaveFacets,
                Documents = mergedDocuments.Take(query.MaxDocuments).ToList(),
                Questions = mergedQuestions.Select(r => r.ConvertValue(QuestionToClient.FromQuestion)).ToList()
            };

            UpdateContextWithNewSuggestions(conversationContext, suggestion.Documents.Select(r => r.Value));

            return suggestion;
        }

        public async Task<IEnumerable<Recommendation<Document>>> GetLongQuerySearchRecommendations(ConversationContext conversationContext)
        {
            var allRelevantQueries = string.Join(" ", conversationContext.ContextItems.Where(x => x.Relevant).Select(m => m.SearchQuery.Query));

            if (string.IsNullOrEmpty(allRelevantQueries.Trim()))
            {
                return new List<Recommendation<Document>>();
            }

            var searchResult = await this._indexSearch.LqSearch(allRelevantQueries, conversationContext.MustHaveFacets);
            var coveoIndexDocuments = this.CreateDocumentsFromCoveoSearch(searchResult, conversationContext.SuggestedDocuments.ToList());
            var documentsFiltered = this.FilterOutChosenSuggestions(coveoIndexDocuments, conversationContext.ContextItems);

            return documentsFiltered.Select(d => new Recommendation<Document>
            {
                Value = d.Item1,
                Confidence = d.Item2,
                RecommendedBy = new List<RecommenderType>
                {
                    RecommenderType.LongQuerySearch
                }
            });
        }

        public void UpdateContextWithNewItem(ConversationContext context, NlpAnalysis nlpAnalysis, SearchQuery searchQuery, bool isRelevant)
        {
            context.ContextItems.Add(new ContextItem
            {
                NlpAnalysis = nlpAnalysis,
                SearchQuery = searchQuery,
                Relevant = isRelevant,
            });
        }

        public bool UpdateContextWithSelectedSuggestion(ConversationContext conversationContext, Guid selectQueryId)
        {
            Document document = conversationContext.SuggestedDocuments.ToList().Find(x => x.Id == selectQueryId);
            if (document != null)
            {
                conversationContext.SelectedSuggestedDocuments.Add(document);
                return true;
            }

            Question question = conversationContext.Questions.ToList().Find(x => x.Id == selectQueryId);
            if (question != null)
            {
                question.Status = QuestionStatus.Clicked;
                return true;
            }

            return false;
        }

        internal async Task<IEnumerable<Recommendation<Document>>> GetQuerySearchRecommendations(ConversationContext conversationContext)
        {
            var query = this.CreateQuery(conversationContext);

            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<Recommendation<Document>>();
            }

            var searchResult = await this._indexSearch.QSearch(query, conversationContext.MustHaveFacets);
            var coveoIndexDocuments = this.CreateDocumentsFromCoveoSearch(searchResult, conversationContext.SuggestedDocuments.ToList());
            var documentsFiltered = this.FilterOutChosenSuggestions(coveoIndexDocuments, conversationContext.ContextItems);

            return documentsFiltered.Select(d => new Recommendation<Document>
            {
                Value = d.Item1,
                Confidence = d.Item2,
                RecommendedBy = new List<RecommenderType>
                {
                    RecommenderType.PreprocessedQuerySearch
                }
            });
        }

        internal async Task<IEnumerable<Recommendation<Document>>> GetLastClickAnalyticsRecommendations(ConversationContext context)
        {
            HashSet<string> contextEntities = this.GetContextEntities(context);
            if (!contextEntities.Any())
            {
                return new List<Recommendation<Document>>();
            }

            List<LastClickAnalyticsResult> lastClickAnalyticsResults = await this._lastClickAnalytics.GetLastClickAnalyticsResults(contextEntities);
            if (context.MustHaveFacets.Any())
            {
                List<string> filteredDocumentsUri = this.FilterDocumentsByFacet(lastClickAnalyticsResults.Select(x => x.Document), context.MustHaveFacets);
                lastClickAnalyticsResults = lastClickAnalyticsResults.Where(x => filteredDocumentsUri.Contains(x.Document.Uri)).ToList();
            }

            return lastClickAnalyticsResults.Select(x => new Recommendation<Document>
            {
                Value = x.Document,
                Confidence = x.Score,
                RecommendedBy = new List<RecommenderType>
                {
                    RecommenderType.LastClickAnalytics
                }
            }).OrderByDescending(recommendation => recommendation.Confidence);
        }

        internal async Task<IEnumerable<Recommendation<Document>>> GetNearestDocumentsRecommendations(ConversationContext conversationContext, IEnumerable<string> documentsUri)
        {
            var allRelevantParsedQuery = conversationContext.ContextItems.Where(x => x.Relevant).Select(c => c.NlpAnalysis.ParsedQuery);
            var contextEntities = new HashSet<string>(string.Join(" ", allRelevantParsedQuery).Split(" "));

            if (!contextEntities.Any() || !documentsUri.Any())
            {
                return new List<Recommendation<Document>>();
            }

            var parameters = new NearestDocumentsParameters
            {
                ContextEntities = contextEntities,
                DocumentsUri = documentsUri
            };

            var results = await this._nearestDocuments.GetNearestDocumentsResults(parameters);
            if (conversationContext.MustHaveFacets.Any())
            {
                var filteredDocumentsUri = this.FilterDocumentsByFacet(results.Select(x => x.Document), conversationContext.MustHaveFacets);
                results = results.Where(x => filteredDocumentsUri.Contains(x.Document.Uri)).ToList();
            }

            return results.Select(x => new Recommendation<Document>
            {
                Value = x.Document,
                Confidence = x.Score,
                RecommendedBy = new List<RecommenderType>
                {
                    RecommenderType.NearestDocuments
                }
            }).OrderByDescending(recommendation => recommendation.Confidence);
        }

        internal string CreateQuery(ConversationContext conversationContext)
        {
            var words = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var queryWords = new Queue<string>();

            using (var allParsedRelevantQueriesEnumerator = conversationContext.ContextItems
                .Where(c => c.SearchQuery.Type == SearchQuery.MessageType.Customer && c.Relevant)
                .Select(x => x.NlpAnalysis.ParsedQuery).Reverse().GetEnumerator())
            {
                while (words.Count < this._numberOfWordsIntoQ)
                {
                    if (!queryWords.Any())
                    {
                        if (!allParsedRelevantQueriesEnumerator.MoveNext())
                        {
                            break;
                        }

                        var parsedQueryWords = allParsedRelevantQueriesEnumerator.Current.Split(" ");

                        foreach (var word in parsedQueryWords)
                        {
                            queryWords.Enqueue(word);
                        }
                    }
                    else
                    {
                        words.Add(queryWords.Dequeue());
                    }
                }
            }

            return string.Join(" ", words);
        }

        internal HashSet<string> GetContextEntities(ConversationContext context)
        {
            List<ContextItem> contextItems = context.ContextItems.Skip(Math.Max(0, context.ContextItems.Count - 1)).ToList();
            HashSet<string> contextEntities = new HashSet<string>();
            foreach (ContextItem contextItem in contextItems)
            {
                if (contextItem.Relevant)
                {
                    contextEntities.UnionWith(contextItem.NlpAnalysis.ParsedQuery.Split(' '));
                }
            }

            return contextEntities;
        }

        // We assume that every list of recommendations is already filtered by confidence descending
        internal IEnumerable<Recommendation<Document>> MergeRecommendedDocuments(List<IEnumerable<Recommendation<Document>>> allRecommendedDocuments)
        {
            // The algorithm will take the max confidence for every document.
            // If a document appear multiple times, it has more chance to get higher (because it is a max with more arguments)
            // This algorithm can change
            return allRecommendedDocuments.SelectMany(x => x.Where(r => r.Confidence >= this._minimumConfidence))
                .GroupBy(r => r.Value.Uri)
                .Select(group => new Recommendation<Document>
                {
                    Value = group.First().Value,
                    Confidence = group.Select(r => r.Confidence).Max(),
                    RecommendedBy = group.SelectMany(r => r.RecommendedBy).ToList()
                }).OrderByDescending(x => x.Confidence);
        }

        internal IEnumerable<Recommendation<Question>> MergeRecommendedQuestions(List<IEnumerable<Recommendation<Question>>> allRecommendedQuestions)
        {
            // Modify if same questions can appear multiple times
            return allRecommendedQuestions.SelectMany(x => x).OrderByDescending(r => r.Confidence);
        }

        internal IEnumerable<Tuple<Document, double>> FilterOutChosenSuggestions(
            IEnumerable<Tuple<Document, double>> coveoIndexDocuments,
            IEnumerable<ContextItem> queriesList)
        {
            var queries = queriesList
                .Select(x => x.SearchQuery.Query)
                .ToList();

            return coveoIndexDocuments.Where(x => !queries.Any(y => y.Contains(x.Item1.Uri)));
        }

        private static void AssociateKnownQuestionsWithId(ConversationContext conversationContext, List<Question> questions)
        {
            foreach (var question in questions)
            {
                var associatedQuestion = conversationContext.Questions.SingleOrDefault(contextQuestion => contextQuestion.Text.Equals(question.Text));
                question.Id = associatedQuestion?.Id ?? question.Id;
            }
        }

        private static IEnumerable<FacetQuestionResult> FilterOutChosenQuestions(
            ConversationContext conversationContext,
            IEnumerable<FacetQuestionResult> questions)
        {
            var questionsText = conversationContext.
                Questions.Where(question => question.Status != QuestionStatus.None && question.Status != QuestionStatus.Clicked)
                .Select(x => x.Text);

            return questions.Where(x => !questionsText.Any(y => y.Contains(x.FacetQuestion.Text)));
        }

        private static void UpdateContextWithNewSuggestions(ConversationContext context, IEnumerable<Document> documents)
        {
            foreach (var document in documents)
            {
                context.SuggestedDocuments.Add(document);
            }
        }

        private static void UpdateContextWithNewQuestions(ConversationContext context, IEnumerable<Question> questions)
        {
            context.LastSuggestedQuestions.Clear();
            foreach (var question in questions)
            {
                context.Questions.Add(question);
                context.LastSuggestedQuestions.Add(question);
            }
        }

        private IEnumerable<FacetQuestionResult> GetQuestionsFromDocument(ConversationContext conversationContext, IEnumerable<Document> documents)
        {
            var questionResults = this._documentFacets.GetQuestions(documents.Select(x => x.Uri));
            AssociateKnownQuestionsWithId(conversationContext, questionResults.Select(q => q.FacetQuestion).Cast<Question>().ToList());
            return FilterOutChosenQuestions(conversationContext, questionResults);
        }

        private List<string> FilterDocumentsByFacet(IEnumerable<Document> documentsToFilter, List<Facet> mustHaveFacets)
        {
            var filterParameter = new FilterDocumentsParameters
            {
                Documents = documentsToFilter.Select(d => d.Uri).ToList(),
                MustHaveFacets = mustHaveFacets
            };
            return this._filterDocuments.FilterDocumentsByFacets(filterParameter);
        }

        private IEnumerable<Recommendation<Question>> GenerateQuestions(ConversationContext conversationContext, IEnumerable<Document> documents)
        {
            var questions = this.GetQuestionsFromDocument(conversationContext, documents).ToList();

            UpdateContextWithNewQuestions(conversationContext, questions.Select(q => q.FacetQuestion));
            questions.ForEach(x => Log.Debug($"Id: {x.FacetQuestion.Id}, Text: {x.FacetQuestion.Text}"));

            return questions.Select(d => new Recommendation<Question>
            {
                Value = d.FacetQuestion,
                Confidence = d.Score,
                RecommendedBy = new List<RecommenderType>
                {
                    RecommenderType.FacetQuestions
                }
            });
        }

        private IEnumerable<Tuple<Document, double>> CreateDocumentsFromCoveoSearch(ISearchResult searchResult, List<Document> suggestedDocuments)
        {
            var documents = new List<Tuple<Document, double>>();
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
                    var document = suggestedDocuments.Find(x => x.Uri == result.Uri) ?? new Document(result);
                    documents.Add(new Tuple<Document, double>(document, Math.Round(result.PercentScore / 100, 4)));
                }
            }

            return documents;
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
    }
}
