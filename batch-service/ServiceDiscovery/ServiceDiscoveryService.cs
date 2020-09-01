using System;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Hosting;

namespace batch_webservice
{
    public class ServiceDiscoveryService : IHostedService
    {
        private readonly IConsulClient m_ConsulClient;
        private readonly ServiceConfig m_ServiceConfig;

        private string m_RegistrationId;

        public ServiceDiscoveryService(
            IConsulClient consulClient,
            ServiceConfig serviceConfig)
        {
            m_ConsulClient = consulClient;
            m_ServiceConfig = serviceConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            m_RegistrationId = $"{m_ServiceConfig.ServiceName}-{m_ServiceConfig.ServiceId}";

            var registration = new AgentServiceRegistration()
            {
                ID = m_RegistrationId,
                Name = m_ServiceConfig.ServiceName,
                Address = m_ServiceConfig.ServiceAddress.Host,
                Port = m_ServiceConfig.ServiceAddress.Port
            };

            await m_ConsulClient.Agent.ServiceDeregister(m_RegistrationId, cancellationToken);
            await m_ConsulClient.Agent.ServiceRegister(registration, cancellationToken);

            Console.WriteLine("Successfully registered service to Consul!");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await m_ConsulClient.Agent.ServiceDeregister(m_RegistrationId, cancellationToken);
            Console.WriteLine("Successfully unregistered service to Consul!");
        }
    }
}