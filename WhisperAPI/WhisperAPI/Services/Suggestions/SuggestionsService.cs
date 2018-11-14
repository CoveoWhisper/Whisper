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
            var documents = this.GetDocuments(conversationContext).ToList();
            conversationContext.LastNotFilteredDocuments = documents;
            conversationContext.FilterDocumentsParameters.Documents = documents.Select(x => x.Uri).ToList();
            return this.GetSuggestion(conversationContext, query);
        }

        public Suggestion GetLastSuggestion(ConversationContext conversationContext, SuggestionQuery query)
        {
            return this.GetSuggestion(conversationContext, query);
        }

        public IEnumerable<Document> GetDocuments(ConversationContext conversationContext)
        {
            var allRelevantQueries = string.Join(" ", conversationContext.SearchQueries.Where(x => x.Relevant).Select(m => m.Query));

            if (string.IsNullOrEmpty(allRelevantQueries.Trim()))
            {
                return new List<Document>();
            }

            var coveoIndexDocuments = this.SearchCoveoIndex(allRelevantQueries, conversationContext.SuggestedDocuments.ToList());

            return this.FilterOutChosenSuggestions(coveoIndexDocuments, conversationContext.SearchQueries);
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

        private List<Document> FilterDocumentsByFacet(ConversationContext conversationContext)
        {
            var filteredDocuments = this._filterDocuments.FilterDocumentsByFacets(conversationContext.FilterDocumentsParameters);
            return conversationContext.LastNotFilteredDocuments.Where(d => filteredDocuments.Contains(d.Uri)).ToList();
        }

        private Suggestion GetSuggestion(ConversationContext conversationContext, SuggestionQuery suggestionQuery)
        {
            var suggestion = new Suggestion
            {
                ActiveFacets = GetActiveFacets(conversationContext).ToList()
            };

            if (suggestion.ActiveFacets.Any())
            {
                var documents = this.FilterDocuments(conversationContext).Take(suggestionQuery.MaxDocuments).ToList();
                suggestion.Documents = documents;
            }
            else
            {
                suggestion.Documents = conversationContext.LastNotFilteredDocuments.Take(suggestionQuery.MaxDocuments).ToList();
            }

            if (suggestion.Documents.Any())
            {
                suggestion.Questions = this.GenerateQuestions(conversationContext, suggestion.Documents).Take(suggestionQuery.MaxQuestions).ToList();
            }

            return suggestion;
        }

        private IEnumerable<Document> FilterDocuments(ConversationContext conversationContext)
        {
            var documentsFiltered = this.FilterDocumentsByFacet(conversationContext);

            UpdateContextWithNewSuggestions(conversationContext, documentsFiltered);
            documentsFiltered.ForEach(x =>
                Log.Debug($"Id: {x.Id}, Title: {x.Title}, Uri: {x.Uri}, PrintableUri: {x.PrintableUri}, Summary: {x.Summary}"));

            return documentsFiltered;
        }

        private IEnumerable<QuestionToClient> GenerateQuestions(ConversationContext conversationContext, List<Document> documents)
        {
            var questions = this.GetQuestionsFromDocument(conversationContext, documents).ToList();
            var questionsToClient = questions.Select(QuestionToClient.FromQuestion).ToList();

            UpdateContextWithNewQuestions(conversationContext, questions);
            questions.ForEach(x => Log.Debug($"Id: {x.Id}, Text: {x.Text}"));

            return questionsToClient;
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
