using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Services.NLPAPI
{
    public class NlpCall : INlpCall
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly HttpClient _httpClient;

        private readonly List<string> _irrelevantIntents;

        private readonly string _baseAddress;

        public NlpCall(HttpClient httpClient, List<string> irrelevantIntents, string baseAddress)
        {
            this._httpClient = httpClient;
            this._irrelevantIntents = irrelevantIntents;
            this._baseAddress = baseAddress;
            this.InitHttpClient();
        }

        public void UpdateAndAnalyseSearchQuery(SearchQuery searchQuery)
        {
            var nlpAnalysis = this.GetNlpAnalysis(searchQuery.Query);
            if (nlpAnalysis == null)
            {
                return;
            }

            searchQuery.Relevant = this.IsQueryRelevant(nlpAnalysis);
            searchQuery.FilteredQuery = nlpAnalysis.ParsedQuery;
        }

        internal bool IsQueryRelevant(NlpAnalysis nlpAnalysis)
        {
            nlpAnalysis.Intents.ForEach(x => Log.Debug($"Intent - Name: {x.Name}, Confidence: {x.Confidence}"));
            nlpAnalysis.Entities.ForEach(x => Log.Debug($"Entity - Name: {x.Name}"));
            return this.IsIntentRelevant(nlpAnalysis);
        }

        private static StringContent CreateStringContent(string sentence)
        {
            return new StringContent($"{{\"sentence\": \"{sentence}\"}}", Encoding.UTF8, "application/json");
        }

        private NlpAnalysis GetNlpAnalysis(string sentence)
        {
            var response = this._httpClient.PostAsync("NLP/Analyze", CreateStringContent(sentence)).Result;
            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<NlpAnalysis>(response.Content.ReadAsStringAsync().Result);
        }

        private void InitHttpClient()
        {
            this._httpClient.BaseAddress = new Uri(this._baseAddress);
            this._httpClient.DefaultRequestHeaders.Accept.Clear();
            this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private bool IsIntentRelevant(NlpAnalysis nlpAnalysis)
        {
            var mostConfidentIntent = nlpAnalysis.Intents.OrderByDescending(x => x.Confidence).First();
            return !this._irrelevantIntents.Any(x => Regex.IsMatch(mostConfidentIntent.Name, this.WildCardToRegularExpression(x)));
        }

        private string WildCardToRegularExpression(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }
    }
}
