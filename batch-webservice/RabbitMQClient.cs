using System;
using RabbitMQ.Client;
using System.Linq;

namespace batch_webservice
{
    public interface IRabbitMQClient
    {
        void PublishBatch();
    }

    public class RabbitMQClient : IDisposable, IRabbitMQClient
    {
        private const int BatchSize = 500;
        private const string ExchangeName = "exchange_batch";
        private const string QueueName = "batch";
        private readonly Lazy<IModel> m_Channel = new Lazy<IModel>(() => CreateChannel());

        public RabbitMQClient()
        {

        }

        public void PublishBatch()
        {
            var hashKey = Guid.NewGuid();

            foreach (var message in Enumerable.Range(0, BatchSize))
            {
                var batch = new Batch(
                    hashKey: hashKey,
                    key: $"{hashKey}_key_{message}",
                    value: $"{hashKey}_value_{message}");

                var body = batch.ToByteArray();
                var props = m_Channel.Value.CreateBasicProperties();

                props.DeliveryMode = 2;
                props.ContentType = "application/json";

                m_Channel.Value.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: QueueName,
                    basicProperties: props,
                    body: body);

                Console.WriteLine($"Published batch {message} / {BatchSize} ...");
            }
        }

        private static IModel CreateChannel()
        {
            var channel = Connect().CreateModel();
            SetupRoutes(channel);
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

        private static void SetupRoutes(IModel channel)
        {
            var queue = channel.QueueDeclare(
                queue: QueueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: "direct",
                durable: false,
                autoDelete: false,
                null
            );

            channel.QueueBind(
                queue: queue.QueueName,
                exchange: ExchangeName,
                queue.QueueName,
                null
            );
        }


        public void Dispose()
        {
            if (!m_Channel.IsValueCreated)
                return;

            m_Channel.Value.Close();
        }
    }
}
