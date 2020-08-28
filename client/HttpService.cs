using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

namespace client
{
    public class HttpClientRoundtrip : HttpClient
    {
    }

    public class HttpClientBatch : HttpClient
    {
    }

    public class HttpService
    {
        private readonly HttpClientRoundtrip m_HttpClientRoundtrip;
        private readonly HttpClientBatch m_HttpClientBatch;

        public HttpService(
            HttpClientRoundtrip httpClientRoundtrip,
            HttpClientBatch httpClientBatch)
        {
            m_HttpClientRoundtrip = httpClientRoundtrip;
            m_HttpClientBatch = httpClientBatch;
        }

        public async Task PostAsync(PostModel model)
        {
            var result = await m_HttpClientRoundtrip.PostAsJsonAsync("api/roundtrip", model);
            result.EnsureSuccessStatusCode();
        }

        public async Task PostBatchAsync()
        {
            var result = await m_HttpClientBatch.PostAsJsonAsync("api/batch", new object());
            result.EnsureSuccessStatusCode();
        }
    }
}