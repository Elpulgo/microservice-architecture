using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            var httpBaseAddress = builder.Configuration["RestUrl"];
            Console.WriteLine("Url supposedly is: " + httpBaseAddress);
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(httpBaseAddress) });
            builder.Services.AddSingleton<WebSocketService>();
            builder.Services.AddScoped<HttpService>();

            await builder.Build().RunAsync();
        }
    }
}
