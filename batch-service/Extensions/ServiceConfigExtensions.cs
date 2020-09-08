using System;
using Microsoft.Extensions.Configuration;

namespace batch_webservice
{

    public static class ServiceConfigExtensions
    {
        public static ServiceConfig GetServiceConfig(this IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var serviceConfig = new ServiceConfig
            {
                ServiceDiscoveryAddress = configuration.GetValue<Uri>("ServiceDiscoveryAddress"),
                ServiceAddress = configuration.GetValue<Uri>("ServiceAddress"),
                ServiceName = configuration.GetValue<string>("ServiceName"),
                ServiceId = configuration.GetValue<string>("ServiceId")
            };

            return serviceConfig;
        }
    }
}