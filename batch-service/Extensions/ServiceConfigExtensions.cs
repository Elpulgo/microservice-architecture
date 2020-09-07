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
                ServiceDiscoveryAddress = configuration.GetValue<Uri>("CONSUL:DISCOVERYADDRESS"),
                ServiceAddress = configuration.GetValue<Uri>("CONSUL:SERVICEADDRESS"),
                ServiceName = configuration.GetValue<string>("CONSUL:SERVICENAME"),
                ServiceId = configuration.GetValue<string>("CONSUL:SERVICEID")
            };

            return serviceConfig;
        }
    }
}