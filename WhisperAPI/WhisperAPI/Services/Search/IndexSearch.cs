﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WhisperAPI.Models.MLAPI;
using WhisperAPI.Models.Search;

[assembly: InternalsVisibleTo("WhisperAPI.Tests")]

namespace WhisperAPI.Services.Search
{
    public class IndexSearch : IIndexSearch
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _searchEndPoint = "rest/search/v2";
        private readonly string _apiKey;
        private readonly int _numberOfResults;
        private readonly HttpClient _httpClient;

        public IndexSearch(string apiKey, int numberOfResults, HttpClient client, string searchBaseAddress)
        {
            this._apiKey = apiKey;
            this._numberOfResults = numberOfResults;
            this._httpClient = client;
            this.InitHttpClient(searchBaseAddress);
        }

        public async Task<ISearchResult> Search(string query, IEnumerable<Facet> mustHaveFacets)
        {
            var stringResult = await this.GetStringFromPost(this._searchEndPoint, this.CreateStringContent(query, mustHaveFacets));
            return JsonConvert.DeserializeObject<SearchResult>(stringResult);
        }

        internal string GenerateAdvancedQuery(IEnumerable<Facet> facets)
        {
            return facets.Aggregate(string.Empty, (current, facet) => current + (" (@" + facet.Name + "==" + facet.Value + ")"));
        }

        private async Task<string> GetStringFromPost(string url, StringContent content)
        {
            HttpResponseMessage response = await this._httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private StringContent CreateStringContent(string query, IEnumerable<Facet> mustHaveFacets)
        {
            var searchParameters = new SearchParameters
            {
                Lq = query,
                NumberOfResults = this._numberOfResults,
                AdvancedQuery = this.GenerateAdvancedQuery(mustHaveFacets)
            };

            var json = JsonConvert.SerializeObject(searchParameters);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private void InitHttpClient(string baseURL)
        {
            this._httpClient.BaseAddress = new System.Uri(baseURL);

            // Add an Accept header for JSON format.
            this._httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // Add an Authorization header with the ApiKey
            this._httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {this._apiKey}");
        }
    }
}
