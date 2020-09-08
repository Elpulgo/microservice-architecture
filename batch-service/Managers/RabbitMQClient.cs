using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace batch_webservice
{
    public interface IRabbitMQClient
    {
        IModel SetupMQTTBindings(MqttBinding binding);
    }

    public class RabbitMQClient : IDisposable, IRabbitMQClient
    {
        private const int MaxConnectionRetries = 20;
        private readonly Lazy<IConnection> m_Connection;

        private IConfiguration Configuration { get; }

        private string MqttConnectionString => Configuration["AMQP_URL"];

        public RabbitMQClient(IConfiguration configuration)
        {
            Configuration = configuration;
            m_Connection = new Lazy<IConnection>(() => Connect());
        }

        public IModel SetupMQTTBindings(MqttBinding mqttBinding)
        {
            var channel = m_Connection.Value.CreateModel();

            var queue = channel.QueueDeclare(
                queue: mqttBinding.QueueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            channel.ExchangeDeclare(
                exchange: mqttBinding.ExchangeName,
                type: "direct",
                durable: false,
                autoDelete: false,
                null
            );

            channel.QueueBind(
                queue: queue.QueueName,
                exchange: mqttBinding.ExchangeName,
                routingKey: mqttBinding.RoutingKey,
                null
            );

            Console.WriteLine($"Successfully setup bindings for queue '{mqttBinding.QueueName}', exchange '{mqttBinding.ExchangeName}'.");

            return channel;
        }

        private IConnection Connect()
        {
            int connectionRetries = 0;

            while (connectionRetries <= MaxConnectionRetries)
            {
                try
                {
                    var connection = new ConnectionFactory()
                    {
                        Endpoint = new AmqpTcpEndpoint(new Uri(MqttConnectionString))
                    }.CreateConnection();

                    Console.WriteLine("Successfully connected to MQTT broker!");
                    return connection;
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
                {
                    Console.WriteLine($"Failed to connect to MQTT broker {connectionRetries} / {MaxConnectionRetries}.. Will retry!");
                }
                finally
                {
                    connectionRetries++;
                    Thread.Sleep(1000);
                }
            }

            Console.WriteLine($"Failed to connect to MQTT broker after {MaxConnectionRetries}! Will exit!");
            Environment.Exit(-1);
            return null;
        }


        public void Dispose()
        {
            if (!m_Connection.IsValueCreated)
                return;

            m_Connection.Value.Close();
        }
    }

    public class RabbitMQException : Exception
    {
        public RabbitMQException(string message) : base(message)
        {

        }
    }
}
