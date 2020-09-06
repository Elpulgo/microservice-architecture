using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Hosting;

namespace batch_webservice
{
    public class Startup
    {
        private readonly string CorsPolicyName = "AllowOrigin";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(c => c.AddPolicy(
                CorsPolicyName,
                options => options.AllowAnyOrigin().AllowAnyHeader()));

            services.RegisterServiceDiscovery(Configuration.GetServiceConfig());
            services.AddControllers();

            services.AddSingleton<IHostedService, MessageConsumer>();
            services.AddSingleton<IMessageConsumer, MessageConsumer>();

            services.AddSingleton<IMessagePublisher, MessagePublisher>();
            services.AddSingleton<IRabbitMQClient, RabbitMQClient>();
            services.AddSingleton<IRedisManager, RedisManager>();
            services.AddSingleton<IPolicyManager, PolicyManager>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseCors(CorsPolicyName);
            app.MaintainCorsHeadersOnError();
            app.UseExceptionHandler(err => err.UseCustomErrors(env));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
