using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using WhisperAPI.Models;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.Queries;
using WhisperAPI.Models.Search;
using WhisperAPI.Services.MLAPI.Facets;
using WhisperAPI.Services.Search;
[assembly: InternalsVisibleTo("WhisperAPI.Tests")]

namespace WhisperAPI.Services.Suggestions
{
    public class SuggestionsService : ISuggestionsService
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IIndexSearch _indexSearch;

        private readonly IDocumentFacets _documentFacets;

        private readonly IFilterDocuments _filterDocuments;

        public SuggestionsService(
            IIndexSearch indexSearch,
            IDocumentFacets documentFacets,
            IFilterDocuments filterDocuments)
        {
            this._indexSearch = indexSearch;
            this._documentFacets = documentFacets;
            this._filterDocuments = filterDocuments;
        }

        public Suggestion GetNewSuggestion(ConversationContext conversationContext, SuggestionQuery query)
        {
            // TODO which recommenders to use, propagate from config
            bool useLongQuerySearchReccomender = true;
            bool usePreprocessedQuerySearchReccomender = true;
            bool useAnalyticsSearchReccomender = true;
            bool useFacetQuestionRecommender = true;

            var allRecommendedDocuments = new List<IEnumerable<Recommendation<Document>>>();
            var allRecommendedQuestions = new List<IEnumerable<Recommendation<Question>>>();

            if (useLongQuerySearchReccomender)
            {
                allRecommendedDocuments.Add(this.GetLongQuerySearchRecommendations(conversationContext).ToList());
            }

            if (usePreprocessedQuerySearchReccomender)
            {
                // TODO
            }

            if (useAnalyticsSearchReccomender)
            {
                // TODO
            }

            // TODO ensure documents are filtered, here, in the calls or afterwards
            var mergedDocuments = this.MergeRecommendedDocuments(allRecommendedDocuments);

            if (useFacetQuestionRecommender)
            {
                allRecommendedQuestions.Add(this.GenerateQuestions(conversationContext, mergedDocuments.Select(d => d.Value)).Take(query.MaxQuestions));
            }

            var mergedQuestions = this.MergeRecommendedQuestions(allRecommendedQuestions).Take(query.MaxQuestions);

            List<Facet> activeFacets = GetActiveFacets(conversationContext).ToList();
            if (activeFacets.Any())
            {
                var documentUris = this.FilterDocumentsByFacet(mergedDocuments.Select(r => r.Value), conversationContext.FilterDocumentsParameters.MustHaveFacets);
                var keptDocumentsUris = new HashSet<string>(documentUris);
                mergedDocuments = mergedDocuments.Where(r => keptDocumentsUris.Contains(r.Value.Uri));
            }

            // TODO I don't think this is used
            // UpdateContextWithNewSuggestions(conversationContext, allRecommendedDocuments);

            var suggestion = new Suggestion
            {
                ActiveFacets = activeFacets,
                Documents = mergedDocuments.Take(query.MaxDocuments).ToList(),
                Questions = mergedQuestions.Select(r => r.ConvertValue(QuestionToClient.FromQuestion)).ToList()
            };

            return suggestion;
        }

        // We assurme that every list of recommendations is already filtered by confidence descending
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

        public Suggestion GetLastSuggestion(ConversationContext conversationContext, SuggestionQuery query)
        {
            // TODO: Delete this method ?
            throw new NotImplementedException();
        }

        public IEnumerable<Recommendation<Document>> GetLongQuerySearchRecommendations(ConversationContext conversationContext)
        {
            var allRelevantQueries = string.Join(" ", conversationContext.SearchQueries.Where(x => x.Relevant).Select(m => m.Query));

            if (string.IsNullOrEmpty(allRelevantQueries.Trim()))
            {
                return new List<Recommendation<Document>>();
            }

            // TODO: why suggested documents
            var coveoIndexDocuments = this.SearchCoveoIndex(allRelevantQueries, conversationContext.SuggestedDocuments.ToList());

            // TODO return recommendations with confidence and recommender type
            var documentsFiltered = this.FilterOutChosenSuggestions(coveoIndexDocuments, conversationContext.SearchQueries);

            return documentsFiltered.Select(d => new Recommendation<Document>
            {
                Value = d,
                Confidence = 1,
                RecommendedBy = new List<RecommenderType>
                {
                    RecommenderType.LongQuerySearch
                }
            });
        }

        public void UpdateContextWithNewQuery(ConversationContext context, SearchQuery searchQuery)
        {
            context.SearchQueries.Add(searchQuery);
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

        internal IEnumerable<Document> FilterOutChosenSuggestions(
            IEnumerable<Document> coveoIndexDocuments,
            IEnumerable<SearchQuery> queriesList)
        {
            var queries = queriesList
                .Select(x => x.Query)
                .ToList();

            return coveoIndexDocuments.Where(x => !queries.Any(y => y.Contains(x.Uri)));
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

        private static void UpdateContextWithNewSuggestions(ConversationContext context, List<Document> documents)
        {
            foreach (var document in documents)
            {
                context.SuggestedDocuments.Add(document);
            }
        }

        private static void UpdateContextWithNewQuestions(ConversationContext context, List<Question> questions)
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

        private IEnumerable<Document> SearchCoveoIndex(string query, List<Document> suggestedDocuments)
        {
            ISearchResult searchResult = this._indexSearch.Search(query);
            var documents = new List<Document>();

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
                    Document document = suggestedDocuments.Find(x => x.Uri == result.Uri) ?? new Document(result);
                    documents.Add(document);
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
