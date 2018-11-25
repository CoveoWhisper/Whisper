using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using WhisperAPI.Models;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;
using WhisperAPI.Models.Search;
using WhisperAPI.Services.MLAPI.Facets;
using WhisperAPI.Services.NLPAPI;
using WhisperAPI.Services.Search;
using WhisperAPI.Services.Suggestions;
using WhisperAPI.Settings;
using WhisperAPI.Tests.Data.Builders;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class SuggestionsServiceTest
    {
        private ISuggestionsService _suggestionsService;

        private Mock<IIndexSearch> _indexSearchMock;
        private Mock<INlpCall> _nlpCallMock;
        private Mock<IDocumentFacets> _documentFacetsMock;
        private Mock<IFilterDocuments> _filterDocuments;
        private ConversationContext _conversationContext;

        [SetUp]
        public void SetUp()
        {
            this._indexSearchMock = new Mock<IIndexSearch>();
            this._nlpCallMock = new Mock<INlpCall>();
            this._documentFacetsMock = new Mock<IDocumentFacets>();
            this._filterDocuments = new Mock<IFilterDocuments>();

            var recommenderSettings = new RecommenderSettings
            {
                UseAnalyticsSearchReccomender = true,
                UseFacetQuestionRecommender = true,
                UseLongQuerySearchRecommender = true,
                UsePreprocessedQuerySearchReccomender = true
            };

            this._suggestionsService = new SuggestionsService(this._indexSearchMock.Object, this._documentFacetsMock.Object, this._filterDocuments.Object, recommenderSettings);
            this._conversationContext = new ConversationContext(new Guid("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d9"), DateTime.Now);
        }

        [Test]
        public void When_receive_valid_search_result_from_search_then_return_list_of_suggestedDocuments()
        {
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());
            this._suggestionsService.GetLongQuerySearchRecommendations(this.GetConversationContext()).Should().BeEquivalentTo(this.GetSuggestedDocuments());
        }

        [Test]
        public void When_receive_irrelevant_intent_then_returns_empty_list_of_suggestedDocuments()
        {
            this.SetUpNLPCallMockToReturn(NlpAnalysisBuilder.Build.Instance, false);

            this._suggestionsService.GetLongQuerySearchRecommendations(this.GetConversationContext()).Should().BeEquivalentTo(new List<Recommendation<Document>>());
        }

        [Test]
        public void When_query_is_selected_by_agent_suggestion_is_filter_out()
        {
            var suggestion = ((SuggestionsService)this._suggestionsService).FilterOutChosenSuggestions(
                this.GetDocumentsWithScore(),
                this.GetQueriesSentByByAgent().Select(x => ContextItemBuilder.Build.WithSearchQuery(x).Instance));

            suggestion.Should().HaveCount(2);
            suggestion.Should().NotContain(this.GetDocumentsWithScore().ToList().Find(x =>
                x.Item1.Uri == "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm"));
            suggestion.Should().NotContain(this.GetDocumentsWithScore().ToList().Find(x =>
                x.Item1.Uri == "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm"));
        }

        [Test]
        [TestCase("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d1", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d2", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d1")]
        [TestCase("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d1", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d2", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d2")]
        public void When_receiving_valid_selectQueryId_add_suggestion_to_list_and_return_true(string suggestedDocumentId, string questionId, string selectQueryId)
        {
            Document document = new Document();
            Question question = FacetQuestionBuilder.Build.Instance;
            document.Id = new Guid(suggestedDocumentId);
            question.Id = new Guid(questionId);

            this._conversationContext.SuggestedDocuments.Add(document);
            this._conversationContext.Questions.Add(question);

            bool isContextUpdated = this._suggestionsService.UpdateContextWithSelectedSuggestion(this._conversationContext, new Guid(selectQueryId));

            Assert.IsTrue(isContextUpdated);
            Assert.IsTrue(this._conversationContext.ClickedQuestions.Contains(question) != this._conversationContext.SelectedSuggestedDocuments.Contains(document));
        }

        [Test]
        [TestCase("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d3")]
        public void When_receiving_invalid_selectQueryId_do_not_add_suggestion_to_list_and_return_false(string selectQueryId)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();

            bool isContextUpdated = this._suggestionsService.UpdateContextWithSelectedSuggestion(this._conversationContext, new Guid(selectQueryId));

            Assert.IsFalse(isContextUpdated);
            Assert.IsTrue(this._conversationContext.SelectedSuggestedDocuments.Count == 0);
            Assert.IsTrue(this._conversationContext.AnswerPendingQuestions.Count == 0);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void When_receive_more_documents_from_search_than_maximum_documents_return_that_maximum_of_documents(int maxDocuments)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxDocuments(maxDocuments)
                .Instance;

            var contextItem = NlpAnalysisBuilder.Build
                .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, contextItem, suggestionQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Documents.Should().HaveCount(maxDocuments);
        }

        [Test]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(10)]
        public void When_receive_less_documents_from_search_than_maximum_documents_return_all_documents(int maxDocuments)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();

            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var searchQuery = SearchQueryBuilder.Build
                .WithMaxDocuments(maxDocuments)
                .Instance;

            var nlpAnalysis = NlpAnalysisBuilder.Build
                .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, nlpAnalysis, searchQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, searchQuery);

            suggestion.Documents.Should().HaveCount(this.GetSuggestedDocuments().Count);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void When_having_more_documents_from_last_search_than_maximum_documents_return_that_maximum_of_documents(int maxDocuments)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();

            var nlpAnalysis = NlpAnalysisBuilder.Build.Instance;

            this.SetUpNLPCallMockToReturn(nlpAnalysis, true);
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var searchQuery = SearchQueryBuilder.Build
                .WithMaxDocuments(maxDocuments)
                .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, nlpAnalysis, searchQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, searchQuery);

            suggestion.Documents.Should().HaveCount(maxDocuments);
        }

        [Test]
        [TestCase(5)]
        public void When_having_less_documents_from_last_search_than_maximum_documents_return_all_documents(int maxQuestions)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();

            var nlpAnalysis = NlpAnalysisBuilder.Build.Instance;

            this.SetUpNLPCallMockToReturn(nlpAnalysis, true);
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var searchQuery = SearchQueryBuilder.Build
                            .WithMaxQuestions(maxQuestions)
                            .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, nlpAnalysis, searchQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, searchQuery);

            suggestion.Documents.Should().HaveCount(this.GetSearchResult().NbrElements);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void When_receive_more_questions_than_maximum_questions_return_that_maximum_of_questions(int maxQuestions)
        {
            this._conversationContext.Questions.Clear();
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxQuestions(maxQuestions)
                .Instance;

            var contextItem = NlpAnalysisBuilder.Build
                .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, contextItem, suggestionQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Questions.Should().HaveCount(maxQuestions);
        }

        [Test]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(10)]
        public void When_receive_less_questions_than_maximum_questions_return_all_questions(int maxQuestions)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();

            var nlpAnalysis = NlpAnalysisBuilder.Build.Instance;

            this.SetUpNLPCallMockToReturn(nlpAnalysis, true);

            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxQuestions(maxQuestions)
                .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, nlpAnalysis, suggestionQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Questions.Should().HaveCount(this.GetSuggestedQuestions().Count());
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void When_having_more_documents_from_last_search_than_maximum_questions_return_that_maximum_of_documents(int maxQuestions)
        {
            this._conversationContext.Questions.Clear();

            var nlpAnalysis = NlpAnalysisBuilder.Build.Instance;

            this.SetUpNLPCallMockToReturn(nlpAnalysis, true);
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxQuestions(maxQuestions)
                .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, nlpAnalysis, suggestionQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, suggestionQuery);

        }

        [Test]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(10)]
        public void When_having_less_documents_from_last_search_than_maximum_questions_return_all_documents(int maxQuestions)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();

            var nlpAnalysis = NlpAnalysisBuilder.Build.Instance;

            this.SetUpNLPCallMockToReturn(nlpAnalysis, true);
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxQuestions(maxQuestions)
                .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, nlpAnalysis, suggestionQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Questions.Should().HaveCount(this.GetSuggestedQuestions().Count());
        }

        public List<SearchQuery> GetQueriesSentByByAgent()
        {
            return new List<SearchQuery>
            {
               new SearchQuery
               {
                   ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"),
                   Query = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                   Type = SearchQuery.MessageType.Agent,
               },
               new SearchQuery
               {
                   ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"),
                   Query = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                   Type = SearchQuery.MessageType.Agent,
               }
            };
        }

        public SearchResult GetSearchResult()
        {
            return new SearchResult
            {
                NbrElements = 4,
                Elements = new List<SearchResultElement>
                {
                    new SearchResultElement
                    {
                        Title = "Available Coveo Cloud V2 Source Types",
                        Uri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                        PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                        Summary = null,
                        Score = 4280,
                        PercentScore = 98
                    },
                    new SearchResultElement
                    {
                        Title = "Coveo Cloud Query Syntax Reference",
                        Uri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                        PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                        Summary = null,
                        Score = 3900,
                        PercentScore = 96
                    },
                    new SearchResultElement
                    {
                        Title = "Events",
                        Uri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                        PrintableUri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                        Summary = null,
                        Score = 2947,
                        PercentScore = 80
                    },
                    new SearchResultElement
                    {
                        Title = "Coveo Facet Component (CoveoFacet)",
                        Uri = "https://coveo.github.io/search-ui/components/facet.html",
                        PrintableUri = "https://coveo.github.io/search-ui/components/facet.html",
                        Summary = null,
                        Score = 2932,
                        PercentScore = 51
                    }
                }
            };
        }

        public List<Recommendation<Document>> GetSuggestedDocuments()
        {
            return new List<Recommendation<Document>>
            {
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Available Coveo Cloud V2 Source Types")
                        .WithUri("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                        .WithPrintableUri("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                        .Instance)
                    .WithConfidence(0.98).Instance,
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Coveo Cloud Query Syntax Reference")
                        .WithUri("https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm")
                        .WithPrintableUri("https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm")
                        .Instance)
                    .WithConfidence(0.96).Instance,
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Events")
                        .WithUri("https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573")
                        .WithPrintableUri("https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573")
                        .Instance)
                    .WithConfidence(0.80).Instance,
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Coveo Facet Component (CoveoFacet)")
                        .WithUri("https://coveo.github.io/search-ui/components/facet.html")
                        .WithPrintableUri("https://coveo.github.io/search-ui/components/facet.html")
                        .Instance)
                    .WithConfidence(0.51).Instance
            };
        }

        public IEnumerable<Tuple<Document, double>> GetDocumentsWithScore()
        {
            return this.GetSuggestedDocuments().Select(d => new Tuple<Document, double>(d.Value, d.Confidence));
        }

        public List<FacetQuestion> GetSuggestedQuestions()
        {
            return new List<FacetQuestion>
            {
                FacetQuestionBuilder.Build.Instance,
                FacetQuestionBuilder.Build.Instance,
                FacetQuestionBuilder.Build.Instance,
                FacetQuestionBuilder.Build.Instance
            };
        }

        public ConversationContext GetConversationContext()
        {
            ConversationContext context = new ConversationContext(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), DateTime.Now)
            {
                ContextItems = new List<ContextItem>
                {
                    ContextItemBuilder.Build
                    .WithSearchQuery(
                        SearchQueryBuilder.Build
                        .WithChatKey(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"))
                        .WithQuery("rest api")
                        .WithMessageType(SearchQuery.MessageType.Customer)
                        .Instance)
                    .WithRelevant(true)
                    .Instance
                }
            };
            return context;
        }

        private void SetUpDocumentFacetMockToReturn(List<FacetQuestion> facetQuestions)
        {
            this._documentFacetsMock
                .Setup(x => x.GetQuestions(It.IsAny<IEnumerable<string>>()))
                .Returns(facetQuestions);
        }

        private void SetUpNLPCallMockToReturn(NlpAnalysis nlpAnalysis, bool isRelevant)
        {
            this._nlpCallMock
                .Setup(x => x.AnalyzeSearchQuery(It.IsAny<SearchQuery>(), out isRelevant))
                .Returns(nlpAnalysis);
        }

        private void SetUpIndexSearchMockToReturn(ISearchResult searchResult)
        {
            this._indexSearchMock
                .Setup(x => x.LqSearch(It.IsAny<string>()))
                .Returns(searchResult);
        }
    }
}
