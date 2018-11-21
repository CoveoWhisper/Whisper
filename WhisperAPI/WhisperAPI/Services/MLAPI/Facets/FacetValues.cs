using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace WhisperAPI.Services.MLAPI.Facets
{
    public class FacetValues : IFacetValues
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _baseAddress;

        private readonly HttpClient _httpClient;

        public FacetValues(HttpClient httpClient, string baseAddress)
        {
            this._httpClient = httpClient;
            this._baseAddress = baseAddress;
            this.InitHttpClient();
        }

        public List<Models.MLAPI.FacetValues> GetFacetValues(IEnumerable<string> facetsName)
        {
            var response = this._httpClient.PostAsync("ML/Facets", CreateStringContent(facetsName)).Result;
            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<List<Models.MLAPI.FacetValues>>(response.Content.ReadAsStringAsync().Result);
        }

        private static StringContent CreateStringContent(IEnumerable<string> facetsName)
        {
            var json = JsonConvert.SerializeObject(facetsName.ToList());
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
