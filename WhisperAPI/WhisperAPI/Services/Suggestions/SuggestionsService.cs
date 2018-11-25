using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using WhisperAPI.Models;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;
using WhisperAPI.Models.Search;
using WhisperAPI.Services.MLAPI.Facets;
using WhisperAPI.Services.Search;
using WhisperAPI.Settings;

[assembly: InternalsVisibleTo("WhisperAPI.Tests")]

namespace WhisperAPI.Services.Suggestions
{
    public class SuggestionsService : ISuggestionsService
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IIndexSearch _indexSearch;

        private readonly IDocumentFacets _documentFacets;

        private readonly RecommenderSettings _recommenderSettings;

        public SuggestionsService(
            IIndexSearch indexSearch,
            IDocumentFacets documentFacets,
            RecommenderSettings recommenderSettings)
        {
            this._indexSearch = indexSearch;
            this._documentFacets = documentFacets;
            this._recommenderSettings = recommenderSettings;
        }

        public Suggestion GetNewSuggestion(ConversationContext conversationContext, SuggestionQuery query)
        {
            var allRecommendedDocuments = new List<IEnumerable<Recommendation<Document>>>();
            var allRecommendedQuestions = new List<IEnumerable<Recommendation<Question>>>();

            if (this._recommenderSettings.UseLongQuerySearchRecommender)
            {
                allRecommendedDocuments.Add(this.GetLongQuerySearchRecommendations(conversationContext).ToList());
            }

            if (this._recommenderSettings.UsePreprocessedQuerySearchReccomender)
            {
                // TODO
            }

            if (this._recommenderSettings.UseAnalyticsSearchReccomender)
            {
                // TODO
            }

            // TODO ensure documents are filtered, here, in the calls or afterwards
            var mergedDocuments = this.MergeRecommendedDocuments(allRecommendedDocuments);

            if (mergedDocuments.Any() && this._recommenderSettings.UseFacetQuestionRecommender)
            {
                allRecommendedQuestions.Add(this.GenerateQuestions(conversationContext, mergedDocuments.Select(d => d.Value)).Take(query.MaxQuestions));
            }

            var mergedQuestions = this.MergeRecommendedQuestions(allRecommendedQuestions).Take(query.MaxQuestions);

            var activeFacets = GetActiveFacets(conversationContext).ToList();
            var suggestion = new Suggestion
            {
                ActiveFacets = activeFacets,
                Documents = mergedDocuments.Take(query.MaxDocuments).ToList(),
                Questions = mergedQuestions.Select(r => r.ConvertValue(QuestionToClient.FromQuestion)).ToList()
            };

            UpdateContextWithNewSuggestions(conversationContext, suggestion.Documents.Select(r => r.Value));

            return suggestion;
        }

        public IEnumerable<Recommendation<Document>> GetLongQuerySearchRecommendations(ConversationContext conversationContext)
        {
            var allRelevantQueries = string.Join(" ", conversationContext.ContextItems.Where(x => x.Relevant).Select(m => m.SearchQuery.Query));

            if (string.IsNullOrEmpty(allRelevantQueries.Trim()))
            {
                return new List<Recommendation<Document>>();
            }

            var coveoIndexDocuments = this.SearchCoveoIndex(allRelevantQueries, conversationContext);
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

        // We assume that every list of recommendations is already filtered by confidence descending
        internal IEnumerable<Recommendation<Document>> MergeRecommendedDocuments(List<IEnumerable<Recommendation<Document>>> allRecommendedDocuments)
        {
            // The algorithm will take the max confidence for every document.
            // If a document appear multiple times, it has more chance to get higher (because it is a max with more arguments)
            // This algorithm can change
            return allRecommendedDocuments.SelectMany(x => x)
                .GroupBy(r => r.Value.Uri)
                .Select(group => new Recommendation<Document>
                {
                    Value = group.First().Value,
                    Confidence = group.Select(r => r.Confidence).Max(),
                    RecommendedBy = group.SelectMany(r => r.RecommendedBy).ToList()
                });
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

        private static IEnumerable<Question> FilterOutChosenQuestions(
            ConversationContext conversationContext,
            IEnumerable<Question> questions)
        {
            var questionsText = conversationContext.
                Questions.Where(question => question.Status != QuestionStatus.None && question.Status != QuestionStatus.Clicked)
                .Select(x => x.Text);

            return questions.Where(x => !questionsText.Any(y => y.Contains(x.Text)));
        }

        private static IEnumerable<Facet> GetActiveFacets(ConversationContext conversationContext)
        {
            return conversationContext.AnsweredQuestions.OfType<FacetQuestion>().Select(a => new Facet
            {
                Id = a.Id,
                Name = a.FacetName,
                Value = a.Answer
            }).ToList();
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

        private IEnumerable<Question> GetQuestionsFromDocument(ConversationContext conversationContext, IEnumerable<Document> documents)
        {
            var questions = this._documentFacets.GetQuestions(documents.Select(x => x.Uri));
            AssociateKnownQuestionsWithId(conversationContext, questions.Cast<Question>().ToList());
            return FilterOutChosenQuestions(conversationContext, questions);
        }

        //Now unused, but kept in case we choose to use it again
        /*
        private List<string> FilterDocumentsByFacet(IEnumerable<Document> documentsToFilter, List<Facet> mustHaveFacets)
        {
            var filterParameter = new FilterDocumentsParameters
            {
                Documents = documentsToFilter.Select(d => d.Uri).ToList(),
                MustHaveFacets = mustHaveFacets
            };
            return this._filterDocuments.FilterDocumentsByFacets(filterParameter);
        }
        */

        private IEnumerable<Recommendation<Question>> GenerateQuestions(ConversationContext conversationContext, IEnumerable<Document> documents)
        {
            var questions = this.GetQuestionsFromDocument(conversationContext, documents).ToList();

            UpdateContextWithNewQuestions(conversationContext, questions);
            questions.ForEach(x => Log.Debug($"Id: {x.Id}, Text: {x.Text}"));

            return questions.Select(d => new Recommendation<Question>
            {
                Value = d,
                Confidence = 1,
                RecommendedBy = new List<RecommenderType>
                {
                    RecommenderType.FacetQuestions
                }
            });
        }

        private IEnumerable<Tuple<Document, double>> SearchCoveoIndex(string query, ConversationContext conversationContext)
        {
            ISearchResult searchResult = this._indexSearch.Search(query, conversationContext.MustHaveFacets);
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
                    var suggestedDocuments = conversationContext.SuggestedDocuments.ToList();
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
