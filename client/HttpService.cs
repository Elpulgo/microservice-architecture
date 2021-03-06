using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System;
using NetCoreShared;

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

        public async Task<BatchStatus> PostBatchAsync()
        {
            var response = await m_HttpClient.PostAsJsonAsync("api/batch", new object());
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception((await response.Content.ReadAsStringAsync()));
            }

            return await response.Content.ReadFromJsonAsync<BatchStatus>();
        }

        public async Task<List<string>> GetBatchKeysAsync()
        {
            var response = await m_HttpClient.GetAsync("api/batch/batchkeys");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception((await response.Content.ReadAsStringAsync()));
            }

            return await response.Content.ReadFromJsonAsync<List<string>>();
        }

        public async Task<List<KeyValueModel>> GetKeyValuesFromBatch(string batchKey)
        {
            var response = await m_HttpClient.GetAsync($"api/batch/batchvalues/{batchKey}");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception((await response.Content.ReadAsStringAsync()));
            }

            return await response.Content.ReadFromJsonAsync<List<KeyValueModel>>();
        }
    }
}