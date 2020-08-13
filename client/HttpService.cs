using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

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
            var result =  await m_HttpClient.PostAsJsonAsync("api/hello", model);
            result.EnsureSuccessStatusCode();
        }
    }
}