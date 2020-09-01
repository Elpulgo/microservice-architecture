using System;
using Consul;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace batch_webservice
{
    public static class ServiceDiscoveryRegistrationExtensions
    {
        public static void RegisterServiceDiscovery(this IServiceCollection services, ServiceConfig serviceConfig)
        {
            if (serviceConfig == null)
            {
                throw new ArgumentNullException(nameof(serviceConfig));
            }

            var consulClient = CreateConsulClient(serviceConfig);
            services.AddSingleton(serviceConfig);
            services.AddSingleton<IHostedService, ServiceDiscoveryService>();
            services.AddSingleton<IConsulClient, ConsulClient>(p => consulClient);
        }
        
        private static ConsulClient CreateConsulClient(ServiceConfig serviceConfig)
        {
            return new ConsulClient(config =>
            {
                config.Address = serviceConfig.ServiceDiscoveryAddress;
            });
        }
    }
}