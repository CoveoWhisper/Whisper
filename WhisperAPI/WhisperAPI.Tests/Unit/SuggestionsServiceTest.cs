using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using WhisperAPI.Models;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;
using WhisperAPI.Models.Search;
using WhisperAPI.Services.MLAPI.Facets;
using WhisperAPI.Services.MLAPI.LastClickAnalytics;
using WhisperAPI.Services.MLAPI.NearestDocuments;
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
        private SuggestionsService _suggestionsService;

        private Mock<IIndexSearch> _indexSearchMock;
        private Mock<ILastClickAnalytics> _lastClickAnalyticsMock;
        private Mock<INearestDocuments> _nearestDocuments;
        private Mock<INlpCall> _nlpCallMock;
        private Mock<IDocumentFacets> _documentFacetsMock;
        private Mock<IFilterDocuments> _filterDocumentsMock;
        private ConversationContext _conversationContext;

        [SetUp]
        public void SetUp()
        {
            this._indexSearchMock = new Mock<IIndexSearch>();
            this._lastClickAnalyticsMock = new Mock<ILastClickAnalytics>();
            this._nearestDocuments = new Mock<INearestDocuments>();
            this._nlpCallMock = new Mock<INlpCall>();
            this._documentFacetsMock = new Mock<IDocumentFacets>();
            this._filterDocumentsMock = new Mock<IFilterDocuments>();

            var recommenderSettings = new RecommenderSettings
            {
                UseAnalyticsSearchRecommender = true,
                UseFacetQuestionRecommender = true,
                UseLongQuerySearchRecommender = true,
                UsePreprocessedQuerySearchRecommender = true,
                UseNearestDocumentsRecommender = false
            };

            this._suggestionsService = new SuggestionsService(this._indexSearchMock.Object, this._lastClickAnalyticsMock.Object, this._documentFacetsMock.Object, this._nearestDocuments.Object, this._filterDocumentsMock.Object, 7, 0.5, recommenderSettings);
            this._conversationContext = new ConversationContext(new Guid("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d9"), DateTime.Now);
        }

        [Test]
        public void When_receive_valid_search_result_from_lq_search_then_return_list_of_suggestedDocuments()
        {
            this.SetUpLqIndexSearchMockToReturn(this.GetSearchResult());

            var recommendedBy = new List<RecommenderType>
            {
                RecommenderType.LongQuerySearch
            };

            this._suggestionsService.GetLongQuerySearchRecommendations(this.GetConversationContext(true)).Result.Should().BeEquivalentTo(this.GetSuggestedDocuments(recommendedBy));
        }

        [Test]
        public void When_receive_valid_search_result_from_q_search_then_return_list_of_suggestedDocuments()
        {
            this.SetUpQIndexSearchMockToReturn(this.GetSearchResult());

            var recommendedBy = new List<RecommenderType>
            {
                RecommenderType.PreprocessedQuerySearch
            };

            this._suggestionsService.GetQuerySearchRecommendations(this.GetConversationContext(true)).Result.Should().BeEquivalentTo(this.GetSuggestedDocuments(recommendedBy));
        }

        [Test]
        public void When_receive_irrelevant_intent_then_returns_empty_list_of_suggestedDocuments()
        {
            this._suggestionsService.GetLongQuerySearchRecommendations(this.GetConversationContext(false)).Result.Should().BeEquivalentTo(new List<Recommendation<Document>>());
            this._suggestionsService.GetQuerySearchRecommendations(this.GetConversationContext(false)).Result.Should().BeEquivalentTo(new List<Recommendation<Document>>());
            this._suggestionsService.GetLastClickAnalyticsRecommendations(this.GetConversationContext(false)).Result.Should().BeEquivalentTo(new List<Recommendation<Document>>());
        }

        [Test]
        public void When_receiving_empty_last_click_analytics_results_then_return_empty_suggestions_list()
        {
            this.SetUpLastClickAnalyticsMockToReturn(new List<LastClickAnalyticsResult>());
            this._suggestionsService.GetLastClickAnalyticsRecommendations(this.GetConversationContext(true)).Result.Should().BeEquivalentTo(new List<Recommendation<Document>>());
        }

        [Test]
        public void When_receiving_last_click_analytics_results_then_return_suggestions_list()
        {
            this.SetUpLastClickAnalyticsMockToReturn(this.GetLastClickAnalyticsResults());
            this.SetUpFilterDocumentsMockToReturn(this.GetLastClickAnalyticsResults().Select(x => x.Document.Uri).ToList());
            this._suggestionsService.GetLastClickAnalyticsRecommendations(this.GetConversationContext(true)).Result.Should().HaveCount(this.GetLastClickAnalyticsResults().Count);
        }

        [Test]
        public void When_query_is_selected_by_agent_suggestion_is_filter_out()
        {
            var suggestion = ((SuggestionsService)this._suggestionsService).FilterOutChosenSuggestions(
                this.GetDocumentsWithScore(),
                this.GetQueriesSentByAgent().Select(x => ContextItemBuilder.Build.WithSearchQuery(x).Instance));

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
            this.SetUpLqIndexSearchMockToReturn(this.GetSearchResult());
            this.SetUpNearestDocumentsMockToReturn(this.GetNearestDocumentsResults());
            this.SetUpLastClickAnalyticsMockToReturn(this.GetLastClickAnalyticsResults());
            this.SetUpFilterDocumentsMockToReturn(this.GetLastClickAnalyticsResults().Select(x => x.Document.Uri).ToList());

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
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(10)]
        public void When_receive_less_documents_from_search_than_maximum_documents_return_all_documents(int maxDocuments)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();

            this.SetUpLastClickAnalyticsMockToReturn(this.GetLastClickAnalyticsResults());
            this.SetUpNearestDocumentsMockToReturn(this.GetNearestDocumentsResults());
            this.SetUpFilterDocumentsMockToReturn(this.GetLastClickAnalyticsResults().Select(x => x.Document.Uri).ToList());
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpLqIndexSearchMockToReturn(this.GetSearchResult());

            var searchQuery = SearchQueryBuilder.Build
                .WithMaxDocuments(maxDocuments)
                .Instance;

            var nlpAnalysis = NlpAnalysisBuilder.Build
                .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, nlpAnalysis, searchQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, searchQuery);

            var recommendedBy = new List<RecommenderType>
            {
                RecommenderType.LongQuerySearch
            };

            suggestion.Documents.Should().HaveCount(this.GetSuggestedDocuments(recommendedBy).Count + this.GetLastClickAnalyticsResults().Count);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void When_receive_more_questions_than_maximum_questions_return_that_maximum_of_questions(int maxQuestions)
        {
            this._conversationContext.Questions.Clear();
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpLqIndexSearchMockToReturn(this.GetSearchResult());
            this.SetUpNearestDocumentsMockToReturn(this.GetNearestDocumentsResults());
            this.SetUpLastClickAnalyticsMockToReturn(this.GetLastClickAnalyticsResults());
            this.SetUpFilterDocumentsMockToReturn(this.GetLastClickAnalyticsResults().Select(x => x.Document.Uri).ToList());

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
            this.SetUpLqIndexSearchMockToReturn(this.GetSearchResult());
            this.SetUpLastClickAnalyticsMockToReturn(this.GetLastClickAnalyticsResults());
            this.SetUpNearestDocumentsMockToReturn(this.GetNearestDocumentsResults());
            this.SetUpFilterDocumentsMockToReturn(this.GetLastClickAnalyticsResults().Select(x => x.Document.Uri).ToList());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxQuestions(maxQuestions)
                .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, nlpAnalysis, suggestionQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Questions.Should().HaveCount(this.GetSuggestedQuestions().Count);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void When_having_more_documents_from_last_search_than_maximum_questions_return_that_maximum_of_documents(int maxQuestions)
        {
            this._conversationContext.Questions.Clear();

            var nlpAnalysis = NlpAnalysisBuilder.Build.Instance;

            this.SetUpNLPCallMockToReturn(nlpAnalysis, true);
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpLqIndexSearchMockToReturn(this.GetSearchResult());
            this.SetUpLastClickAnalyticsMockToReturn(this.GetLastClickAnalyticsResults());
            this.SetUpNearestDocumentsMockToReturn(this.GetNearestDocumentsResults());
            this.SetUpFilterDocumentsMockToReturn(this.GetLastClickAnalyticsResults().Select(x => x.Document.Uri).ToList());

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
            this.SetUpLastClickAnalyticsMockToReturn(this.GetLastClickAnalyticsResults());
            this.SetUpNearestDocumentsMockToReturn(this.GetNearestDocumentsResults());
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpLqIndexSearchMockToReturn(this.GetSearchResult());
            this.SetUpFilterDocumentsMockToReturn(this.GetLastClickAnalyticsResults().Select(x => x.Document.Uri).ToList());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxQuestions(maxQuestions)
                .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, nlpAnalysis, suggestionQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Questions.Should().HaveCount(this.GetSuggestedQuestions().Count);
        }

        [Test]
        public void When_search_query_contains_overriden_recommender_settings_then_they_are_overriden()
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();

            var nlpAnalysis = NlpAnalysisBuilder.Build.Instance;

            this.SetUpNLPCallMockToReturn(nlpAnalysis, true);
            this.SetUpLastClickAnalyticsMockToReturn(this.GetLastClickAnalyticsResults());
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpLqIndexSearchMockToReturn(this.GetSearchResult());
            this.SetUpFilterDocumentsMockToReturn(this.GetLastClickAnalyticsResults().Select(x => x.Document.Uri).ToList());

            var overridenRecommenderSettings = new RecommenderSettings
            {
                UseAnalyticsSearchRecommender = true,
                UseFacetQuestionRecommender = false,
                UseLongQuerySearchRecommender = true,
                UsePreprocessedQuerySearchRecommender = false
            };

            var suggestionQuery = SearchQueryBuilder.Build
                .WithOverridenRecommenderSettings(overridenRecommenderSettings)
                .Instance;

            this._suggestionsService.UpdateContextWithNewItem(this._conversationContext, nlpAnalysis, suggestionQuery, true);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Questions.Should().HaveCount(0);
            suggestion.Documents.Should().HaveCount(5);
        }

        [Test]
        public void When_having_less_words_into_parsed_query_than_numberOfWordsIntoQ_then_return_all_words()
        {
            var context = new ConversationContext(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), DateTime.Now)
            {
                ContextItems = new List<ContextItem>
                {
                    ContextItemBuilder.Build
                        .WithSearchQuery(
                            SearchQueryBuilder.Build
                                .WithChatKey(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"))
                                .WithQuery("I need help with coveo search api")
                                .WithMessageType(SearchQuery.MessageType.Customer)
                                .Instance)
                        .WithRelevant(true)
                        .WithNlpAnalysis(
                            NlpAnalysisBuilder.Build
                                .WithQuery("search api")
                                .Instance)
                        .Instance
                }
            };

            var query = this._suggestionsService.CreateQuery(context);
            query.Split(" ").Length.Should().Be(2);
        }

        [Test]
        public void When_having_more_words_into_parsed_query_than_numberOfWordsIntoQ_then_return_numberOfWordsIntoQ_words()
        {
            var context = new ConversationContext(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), DateTime.Now)
            {
                ContextItems = new List<ContextItem>
                {
                    ContextItemBuilder.Build
                        .WithSearchQuery(
                            SearchQueryBuilder.Build
                                .WithChatKey(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"))
                                .WithQuery("I need help with coveo search api")
                                .WithMessageType(SearchQuery.MessageType.Customer)
                                .Instance)
                        .WithRelevant(true)
                        .WithNlpAnalysis(
                            NlpAnalysisBuilder.Build
                                .WithQuery("search api")
                                .Instance)
                        .Instance,
                    ContextItemBuilder.Build
                        .WithSearchQuery(
                            SearchQueryBuilder.Build
                                .WithChatKey(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"))
                                .WithQuery("I need help with coveo search api")
                                .WithMessageType(SearchQuery.MessageType.Customer)
                                .Instance)
                        .WithRelevant(true)
                        .WithNlpAnalysis(
                            NlpAnalysisBuilder.Build
                                .WithQuery("coveo test year month word hi")
                                .Instance)
                        .Instance
                }
            };

            var query = this._suggestionsService.CreateQuery(context);
            query.Split(" ").Length.Should().Be(7);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(15)]
        [TestCase(100)]
        public void Get_context_entities(int contextItemsCount)
        {
            List<ContextItem> contextItems = new List<ContextItem>();
            int irrelevantCount = contextItemsCount / 2;
            for (int i = 0; i < contextItemsCount; i++)
            {
                contextItems.Add(
                    ContextItemBuilder.Build
                        .WithNlpAnalysis(NlpAnalysisBuilder.Build.WithQuery(i.ToString()).Instance)
                        .WithRelevant(i >= irrelevantCount)
                        .Instance);
            }

            int expectedCount = contextItemsCount > 10 ? Math.Min(contextItemsCount - irrelevantCount, 10) : contextItemsCount - irrelevantCount;
            ConversationContext context = new ConversationContext() { ContextItems = contextItems };
            this._suggestionsService.GetContextEntities(context).Should().HaveCount(expectedCount);
        }

        public List<SearchQuery> GetQueriesSentByAgent()
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

        public List<Recommendation<Document>> GetSuggestedDocuments(List<RecommenderType> recommenderTypes)
        {
            return new List<Recommendation<Document>>
            {
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Available Coveo Cloud V2 Source Types")
                        .WithUri("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                        .WithPrintableUri("https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm")
                        .Instance)
                    .WithConfidence(0.98)
                    .WithRecommendationType(recommenderTypes).Instance,
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Coveo Cloud Query Syntax Reference")
                        .WithUri("https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm")
                        .WithPrintableUri("https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm")
                        .Instance)
                    .WithConfidence(0.96)
                    .WithRecommendationType(recommenderTypes).Instance,
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Events")
                        .WithUri("https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573")
                        .WithPrintableUri("https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573")
                        .Instance)
                    .WithConfidence(0.80)
                    .WithRecommendationType(recommenderTypes).Instance,
                RecommendationBuilder<Document>.Build.WithValue(
                    DocumentBuilder.Build
                        .WithTitle("Coveo Facet Component (CoveoFacet)")
                        .WithUri("https://coveo.github.io/search-ui/components/facet.html")
                        .WithPrintableUri("https://coveo.github.io/search-ui/components/facet.html")
                        .Instance)
                    .WithConfidence(0.51)
                    .WithRecommendationType(recommenderTypes).Instance
            };
        }

        public IEnumerable<Tuple<Document, double>> GetDocumentsWithScore()
        {
            var recommendedBy = new List<RecommenderType>
            {
                RecommenderType.LongQuerySearch
            };

            return this.GetSuggestedDocuments(recommendedBy).Select(d => new Tuple<Document, double>(d.Value, d.Confidence));
        }

        public List<LastClickAnalyticsResult> GetLastClickAnalyticsResults()
        {
            return new List<LastClickAnalyticsResult>
            {
                LastClickAnalyticsResultsBuilder.Build.Instance
            };
        }

        public List<NearestDocumentsResult> GetNearestDocumentsResults()
        {
            return new List<NearestDocumentsResult>
            {
                NearestDocumentsResultBuilder.Build.Instance
            };
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

        public ConversationContext GetConversationContext(bool relevant)
        {
            ConversationContext context = new ConversationContext(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), DateTime.Now)
            {
                ContextItems = new List<ContextItem>
                {
                    ContextItemBuilder.Build
                    .WithSearchQuery(
                        SearchQueryBuilder.Build
                        .WithChatKey(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"))
                        .WithQuery("I need help with coveo search api")
                        .WithMessageType(SearchQuery.MessageType.Customer)
                        .Instance)
                    .WithRelevant(relevant)
                    .WithNlpAnalysis(
                        NlpAnalysisBuilder.Build
                        .WithQuery("search api")
                        .Instance)
                    .Instance
                }
            };
            return context;
        }

        private void SetUpLastClickAnalyticsMockToReturn(List<LastClickAnalyticsResult> lastClickAnalyticsResults)
        {
            this._lastClickAnalyticsMock
                .Setup(x => x.GetLastClickAnalyticsResults(It.IsAny<HashSet<string>>()))
                .Returns(Task.FromResult(lastClickAnalyticsResults));
        }

        private void SetUpNearestDocumentsMockToReturn(List<NearestDocumentsResult> nearestDocumentsResults)
        {
            this._nearestDocuments
                .Setup(x => x.GetNearestDocumentsResults(It.IsAny<NearestDocumentsParameters>()))
                .Returns(Task.FromResult(nearestDocumentsResults));
        }

        private void SetUpDocumentFacetMockToReturn(List<FacetQuestion> facetQuestions)
        {
            this._documentFacetsMock
                .Setup(x => x.GetQuestions(It.IsAny<IEnumerable<string>>()))
                .Returns(facetQuestions);
        }

        private void SetUpFilterDocumentsMockToReturn(List<string> filteredDocumentsUri)
        {
            this._filterDocumentsMock
                .Setup(x => x.FilterDocumentsByFacets(It.IsAny<FilterDocumentsParameters>()))
                .Returns(filteredDocumentsUri);
        }

        private void SetUpNLPCallMockToReturn(NlpAnalysis nlpAnalysis, bool isRelevant)
        {
            this._nlpCallMock
                .Setup(x => x.AnalyzeSearchQuery(It.IsAny<SearchQuery>(), out isRelevant))
                .Returns(nlpAnalysis);
        }

        private void SetUpLqIndexSearchMockToReturn(ISearchResult searchResult)
        {
            this._indexSearchMock
                .Setup(x => x.LqSearch(It.IsAny<string>(), It.IsAny<List<Facet>>()))
                .Returns(Task.FromResult(searchResult));
        }

        private void SetUpQIndexSearchMockToReturn(ISearchResult searchResult)
        {
            this._indexSearchMock
                .Setup(x => x.QSearch(It.IsAny<string>(), It.IsAny<List<Facet>>()))
                .Returns(Task.FromResult(searchResult));
        }
    }
}
