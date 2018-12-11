using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Services.MLAPI.NearestDocuments
{
    public class NearestDocuments : INearestDocuments
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _baseAddress;

        private readonly HttpClient _httpClient;

        public NearestDocuments(HttpClient httpClient, string baseAddress)
        {
            this._httpClient = httpClient;
            this._baseAddress = baseAddress;
            this.InitHttpClient();
        }

        public async Task<List<NearestDocumentsResult>> GetNearestDocumentsResults(NearestDocumentsParameters parameters)
        {
            var response = await this._httpClient.PostAsync("ML/NearestDocuments", CreateStringContent(parameters));
            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<List<NearestDocumentsResult>>(response.Content.ReadAsStringAsync().Result);
        }

        private static StringContent CreateStringContent(NearestDocumentsParameters parameters)
        {
            var json = JsonConvert.SerializeObject(parameters);
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
