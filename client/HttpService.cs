using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System;
using System.Linq;

namespace client
{
    public class HttpService
    {
        private readonly HttpClient m_HttpClient;

        public HttpService(HttpClient httpClient)
        {
            m_HttpClient = httpClient;
        }

        public async Task PostAsync(PostModel model)
        {
            var response = await m_HttpClient.PostAsJsonAsync("api/roundtrip", model);
            response.EnsureSuccessStatusCode();
        }

        public async Task PostBatchAsync()
        {
            var response = await m_HttpClient.PostAsJsonAsync("api/batch", new object());
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<string>> GetBatchKeysAsync()
        {
            var response = await m_HttpClient.GetAsync("api/batch/batchkeys");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<string>>();
        }

        public async Task<List<KeyValueModel>> GetKeyValuesFromBatch(string batchKey)
        {
            var response = await m_HttpClient.GetAsync($"api/batch/batchvalues/{batchKey}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<KeyValueModel>>();
        }
    }
}