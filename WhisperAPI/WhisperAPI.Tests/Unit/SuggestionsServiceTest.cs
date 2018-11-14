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

            this._suggestionsService = new SuggestionsService(this._indexSearchMock.Object, this._documentFacetsMock.Object, this._filterDocuments.Object);
            this._conversationContext = new ConversationContext(new Guid("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d9"), DateTime.Now);
        }

        [Test]
        [TestCase]
        public void When_receive_valid_search_result_from_search_then_return_list_of_suggestedDocuments()
        {
            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Need Help").Instance
            };

            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intents).Instance;

            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());
            this.SetUpNLPCallMockToReturn(SearchQueryBuilder.Build.Instance, true);

            this._suggestionsService.GetDocuments(this.GetConversationContext()).Should().BeEquivalentTo(this.GetSuggestedDocuments());
        }

        [Test]
        [TestCase]
        public void When_receive_empty_search_result_from_search_then_return_empty_list_of_suggestedDocuments()
        {
            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Need Help").Instance
            };

            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intents).Instance;
            this.SetUpNLPCallMockToReturn(SearchQueryBuilder.Build.Instance, false);
            this.SetUpIndexSearchMockToReturn(new SearchResult());

            this._suggestionsService.GetDocuments(this.GetConversationContext()).Should().BeEquivalentTo(new List<Document>());
        }

        [Test]
        [TestCase]
        public void When_receive_irrelevant_intent_then_returns_empty_list_of_suggestedDocuments()
        {
            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Greetings").Instance
            };

            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intents).Instance;
            this.SetUpNLPCallMockToReturn(SearchQueryBuilder.Build.Instance, false);

            this._suggestionsService.GetDocuments(this.GetConversationContext()).Should().BeEquivalentTo(new List<Document>());
        }

        [Test]
        [TestCase]
        public void When_query_is_selected_by_agent_suggestion_is_filter_out()
        {
            var suggestion = ((SuggestionsService)this._suggestionsService).FilterOutChosenSuggestions(
                this.GetSuggestedDocuments(), this.GetQueriesSentByByAgent());

            suggestion.Should().HaveCount(2);
            suggestion.Should().NotContain(this.GetSuggestedDocuments().Find(x =>
                x.Uri == "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm"));
            suggestion.Should().NotContain(this.GetSuggestedDocuments().Find(x =>
                x.Uri == "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm"));
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

            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Need Help").Instance
            };
            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intents).Instance;

            this.SetUpNLPCallMockToReturn(SearchQueryBuilder.Build.Instance, true);
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxDocuments(maxDocuments)
                .WithRelevant(true)
                .Instance;

            this._suggestionsService.UpdateContextWithNewQuery(this._conversationContext, suggestionQuery);
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

            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Need Help").Instance
            };
            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intents).Instance;

            this.SetUpNLPCallMockToReturn(SearchQueryBuilder.Build.Instance, true);
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxDocuments(maxDocuments)
                .WithRelevant(true)
                .Instance;

            this._suggestionsService.UpdateContextWithNewQuery(this._conversationContext, suggestionQuery);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Documents.Should().HaveCount(this.GetSuggestedDocuments().Count());
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void When_having_more_documents_from_last_search_than_maximum_documents_return_that_maximum_of_documents(int maxDocuments)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();
            this._conversationContext.LastNotFilteredDocuments = this.GetSuggestedDocuments();

            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxDocuments(maxDocuments)
                .WithRelevant(true)
                .Instance;

            var suggestion = this._suggestionsService.GetLastSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Documents.Should().HaveCount(maxDocuments);
        }

        [Test]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(10)]
        public void When_having_less_documents_from_last_search_than_maximum_documents_return_all_documents(int maxQuestions)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();
            this._conversationContext.LastNotFilteredDocuments = this.GetSuggestedDocuments();

            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxQuestions(maxQuestions)
                .WithRelevant(true)
                .Instance;

            var suggestion = this._suggestionsService.GetLastSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Documents.Should().HaveCount(this._conversationContext.LastNotFilteredDocuments.Count());
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void When_receive_more_documents_from_search_than_maximum_questions_return_that_maximum_of_documents(int maxQuestions)
        {
            this._conversationContext.Questions.Clear();

            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Need Help").Instance
            };
            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intents).Instance;

            this.SetUpNLPCallMockToReturn(SearchQueryBuilder.Build.Instance, true);
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxQuestions(maxQuestions)
                .WithRelevant(true)
                .Instance;

            this._suggestionsService.UpdateContextWithNewQuery(this._conversationContext, suggestionQuery);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Questions.Should().HaveCount(maxQuestions);
        }

        [Test]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(10)]
        public void When_receive_less_documents_from_search_than_maximum_questions_return_all_documents(int maxQuestions)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();

            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Need Help").Instance
            };
            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intents).Instance;

            this.SetUpNLPCallMockToReturn(SearchQueryBuilder.Build.Instance, true);
            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());
            this.SetUpIndexSearchMockToReturn(this.GetSearchResult());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxQuestions(maxQuestions)
                .WithRelevant(true)
                .Instance;

            this._suggestionsService.UpdateContextWithNewQuery(this._conversationContext, suggestionQuery);
            var suggestion = this._suggestionsService.GetNewSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Questions.Should().HaveCount(this.GetSuggestedQuestions().Count());
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void When_having_more_documents_from_last_search_than_maximum_questions_return_that_maximum_of_documents(int maxQuestions)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();
            this._conversationContext.LastNotFilteredDocuments = this.GetSuggestedDocuments();

            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxQuestions(maxQuestions)
                .WithRelevant(true)
                .Instance;

            var suggestion = this._suggestionsService.GetLastSuggestion(this._conversationContext, suggestionQuery);

            suggestion.Questions.Should().HaveCount(maxQuestions);
        }

        [Test]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(10)]
        public void When_having_less_documents_from_last_search_than_maximum_questions_return_all_documents(int maxQuestions)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();
            this._conversationContext.LastNotFilteredDocuments = this.GetSuggestedDocuments();

            this.SetUpDocumentFacetMockToReturn(this.GetSuggestedQuestions());

            var suggestionQuery = SearchQueryBuilder.Build
                .WithMaxQuestions(maxQuestions)
                .Instance;

            var suggestion = this._suggestionsService.GetLastSuggestion(this._conversationContext, suggestionQuery);

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
                   Relevant = true
               },
               new SearchQuery
               {
                   ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"),
                   Query = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                   Type = SearchQuery.MessageType.Agent,
                   Relevant = true
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
                        Score = 4280
                    },
                    new SearchResultElement
                    {
                        Title = "Coveo Cloud Query Syntax Reference",
                        Uri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                        PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                        Summary = null,
                        Score = 3900
                    },
                    new SearchResultElement
                    {
                        Title = "Events",
                        Uri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                        PrintableUri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                        Summary = null,
                        Score = 2947
                    },
                    new SearchResultElement
                    {
                        Title = "Coveo Facet Component (CoveoFacet)",
                        Uri = "https://coveo.github.io/search-ui/components/facet.html",
                        PrintableUri = "https://coveo.github.io/search-ui/components/facet.html",
                        Summary = null,
                        Score = 2932
                    }
                }
            };
        }

        public List<Document> GetSuggestedDocuments()
        {
            return new List<Document>
            {
                new Document
                {
                    Title = "Available Coveo Cloud V2 Source Types",
                    Uri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                    PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                    Summary = null
                },
                new Document
                {
                    Title = "Coveo Cloud Query Syntax Reference",
                    Uri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                    PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                    Summary = null
                },
                new Document
                {
                    Title = "Events",
                    Uri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                    PrintableUri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                    Summary = null
                },
                new Document
                {
                    Title = "Coveo Facet Component (CoveoFacet)",
                    Uri = "https://coveo.github.io/search-ui/components/facet.html",
                    PrintableUri = "https://coveo.github.io/search-ui/components/facet.html",
                    Summary = null
                }
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

        public List<string> GetIrrelevantIntents()
        {
            return new List<string>
            {
                "Greetings"
            };
        }

        public ConversationContext GetConversationContext()
        {
            ConversationContext context = new ConversationContext(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), DateTime.Now)
            {
                SearchQueries = new List<SearchQuery>
                {
                    new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = "rest api", Type = SearchQuery.MessageType.Customer, FilteredQuery = "rest api", Relevant = true }
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

        private void SetUpNLPCallMockToReturn(SearchQuery searchQuery, bool relevant)
        {
            this._nlpCallMock
                .Setup(x => x.UpdateAndAnalyseSearchQuery(searchQuery))
                .Callback(() =>
                {
                    searchQuery.FilteredQuery = searchQuery.Query;
                    searchQuery.Relevant = relevant;
                });
        }

        private void SetUpIndexSearchMockToReturn(ISearchResult searchResult)
        {
            this._indexSearchMock
                .Setup(x => x.Search(It.IsAny<string>()))
                .Returns(searchResult);
        }
    }
}
