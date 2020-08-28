using System;
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

            var httpBaseAddressRoundtrip = builder.Configuration["RestUrlRoundtrip"];
            var httpBaseAddressBatch = builder.Configuration["RestUrlBatch"];

            Console.WriteLine("Url roundtrip supposedly is: " + httpBaseAddressRoundtrip);
            Console.WriteLine("Url batch supposedly is: " + httpBaseAddressBatch);

            builder.Services.AddScoped(sp => new HttpClientRoundtrip() { BaseAddress = new Uri(httpBaseAddressRoundtrip) });
            builder.Services.AddScoped(sp => new HttpClientBatch() { BaseAddress = new Uri(httpBaseAddressBatch) });

            builder.Services.AddSingleton<WebSocketService>();
            builder.Services.AddScoped<HttpService>();

            await builder.Build().RunAsync();
        }
    }
}
