using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Services.MLAPI.LastClickAnalytics
{
    public class LastClickAnalytics : ILastClickAnalytics
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _baseAddress;

        private readonly HttpClient _httpClient;

        public LastClickAnalytics(HttpClient httpClient, string baseAddress)
        {
            this._httpClient = httpClient;
            this._baseAddress = baseAddress;
            this.InitHttpClient();
        }

        public async Task<List<LastClickAnalyticsResult>> GetLastClickAnalyticsResults(HashSet<string> contextEntities)
        {
            var response = await this._httpClient.PostAsync("ML/Analytics", CreateStringContent(contextEntities));
            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<List<LastClickAnalyticsResult>>(response.Content.ReadAsStringAsync().Result);
        }

        private static StringContent CreateStringContent(IEnumerable<string> contextEntities)
        {
            var json = JsonConvert.SerializeObject(contextEntities.ToList());
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private void InitHttpClient()
        {
            this._httpClient.BaseAddress = new Uri(this._baseAddress);
            this._httpClient.DefaultRequestHeaders.Accept.Clear();
            this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
