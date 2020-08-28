using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            var apiGatewayUrl = builder.Configuration["ApiGatewayUrl"];

            builder.Services.AddScoped(sp => new HttpClient() { BaseAddress = BuildHttpUrl(apiGatewayUrl) });
            builder.Services.AddSingleton(sp => new WebSocketService() { BaseAddress = BuildWebsocketUrl(apiGatewayUrl) });
            builder.Services.AddScoped<HttpService>();

            await builder.Build().RunAsync();
        }

        private static Uri BuildHttpUrl(string baseAddress) => new Uri($"http://{baseAddress}");
        private static Uri BuildWebsocketUrl(string baseAddress) => new Uri($"ws://{baseAddress}");
    }
}
