using System;
using RabbitMQ.Client;

namespace batch_webservice
{
    public interface IRabbitMQClient
    {
        IModel SetupMQTTBindings(MqttBinding binding);
    }

    public class RabbitMQClient : IDisposable, IRabbitMQClient
    {
        private readonly Lazy<IConnection> m_Connection = new Lazy<IConnection>(() => Connect());

        public RabbitMQClient()
        {
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

        private static IConnection Connect()
        {
            int maxConnectionRetries = 20;
            int connectionRetries = 0;

            while (connectionRetries <= maxConnectionRetries)
            {
                try
                {
                    var connection = new ConnectionFactory() { HostName = "mqtt", Port = 5672 }.CreateConnection();
                    Console.WriteLine("Successfully connected to MQTT broker!");
                    return connection;
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
                {
                    Console.WriteLine($"Failed to connect to MQTT broker {connectionRetries} / {maxConnectionRetries}.. Will retry!");
                }
                finally
                {
                    connectionRetries++;
                }
            }

            Console.WriteLine($"Failed to connect to MQTT broker after {maxConnectionRetries}! Will exit!");
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
