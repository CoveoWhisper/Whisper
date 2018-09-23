using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using WhisperAPI.Models.Search;
using WhisperAPI.Services.Search;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class IndexSearchTest
    {
        private Mock<HttpMessageHandler> _httpMessageHandler;
        private HttpClient _httpClient;

        [SetUp]
        public void SetUp()
        {
           this._httpMessageHandler = new Mock<HttpMessageHandler>();
        }

        [Test]
        [TestCase("test")]
        public void When_receive_ok_response_from_post_then_return_result_correctly(string query)
        {
            this._httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = this.GetStringContent()
                }));

            this._httpClient = new HttpClient(this._httpMessageHandler.Object);
            IIndexSearch indexSearchOK = new IndexSearch(null, this._httpClient, "https://localhost:5000");

            indexSearchOK.Search(query).Should().BeEquivalentTo(this.GetSearchResult());
        }

        [Test]
        [TestCase("test")]
        public void When_receive_not_found_response_from_post_then_throws_exception(string query)
        {
            this._httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Content = this.GetStringContent()
                }));

            this._httpClient = new HttpClient(this._httpMessageHandler.Object);
            IIndexSearch indexSearchNotFound = new IndexSearch(null, this._httpClient, "https://localhost:5000");

            Assert.Throws<HttpRequestException>(() => indexSearchNotFound.Search(query));
        }

        [Test]
        [TestCase("test")]
        public void When_receive_ok_response_with_empty_content_from_post_then_return_null(string query)
        {
            this._httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(string.Empty)
                }));

            this._httpClient = new HttpClient(this._httpMessageHandler.Object);
            IIndexSearch indexSearchOKNoContent = new IndexSearch(null, this._httpClient, "https://localhost:5000");

            indexSearchOKNoContent.Search(query).Should().BeEquivalentTo((SearchResult)null);
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

        public StringContent GetStringContent()
        {
            return new StringContent("{\"totalCount\": 4,\"results\": [{\"title\": \"Available Coveo Cloud V2 Source Types\",\"clickUri\": \"https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm\",\"printableUri\": \"https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm\",\"score\": 4280       },{\"title\": \"Coveo Cloud Query Syntax Reference\",\"clickUri\": \"https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm\",\"printableUri\": \"https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm\",\"score\": 3900},{\"title\": \"Events\",\"clickUri\": \"https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573\",\"printableUri\": \"https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573\",\"score\": 2947},{\"title\": \"Coveo Facet Component (CoveoFacet)\",\"clickUri\": \"https://coveo.github.io/search-ui/components/facet.html\",\"printableUri\": \"https://coveo.github.io/search-ui/components/facet.html\",\"score\": 2932}]}");
        }
    }
}
