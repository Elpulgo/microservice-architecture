using System;
using RabbitMQ.Client;
using System.Linq;
using Polly.CircuitBreaker;
using System.Threading;

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
        private readonly Lazy<IModel> m_Channel = new Lazy<IModel>(() => Connect());
        private readonly IPolicyManager m_PolicyManager;

        public RabbitMQClient(IPolicyManager policyManager)
        {
            m_PolicyManager = policyManager;
        }

        public void PublishBatch()
        {
            var hashKey = Guid.NewGuid();

            if (m_Channel.Value.IsClosed)
            {
                Console.WriteLine("Channel is closed, won't send batch ...");
                throw new RabbitMQException("MQTT channel is closed!");
            }

            if (m_PolicyManager.PolicyRabbitMQPublish.CircuitState == CircuitState.Open ||
                m_PolicyManager.PolicyRabbitMQPublish.CircuitState == CircuitState.Isolated)
            {
                Console.WriteLine("Circuit is open, won't send batch ...");
                throw new RabbitMQException("Circuit is open or isolated, won't try and send batch!");
            }

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

                try
                {
                    m_PolicyManager.PolicyRabbitMQPublish.Execute(() => m_Channel.Value.BasicPublish(
                            exchange: ExchangeName,
                            routingKey: QueueName,
                            basicProperties: props,
                            body: body));
                }
                catch (Exception circuitException)
                {
                    Console.WriteLine($"Circuit is open, will abort sending batch! Sent {message} / {BatchSize}. Exception: {circuitException.Message}");
                    throw new RabbitMQException($"Circuit is open, will abort sending batch! Sent {message} / {BatchSize}: {circuitException.Message}");
                }

                Console.WriteLine($"Published batch {message} / {BatchSize} ...");
            }
        }

        private static IModel Connect()
        {
            int maxConnectionRetries = 20;
            int connectionRetries = 0;

            while (connectionRetries <= maxConnectionRetries)
            {
                try
                {
                    var connection = new ConnectionFactory() { HostName = "mqtt", Port = 5672 }.CreateConnection();
                    Console.WriteLine("Successfully connected to MQTT broker!");
                    var channel = connection.CreateModel();
                    SetupRoutes(channel);
                    return channel;
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

            Console.WriteLine("Successfully setup and bind queues/exchanges!");
        }


        public void Dispose()
        {
            if (!m_Channel.IsValueCreated)
                return;

            m_Channel.Value.Close();
        }
    }

    public class RabbitMQException : Exception
    {
        public RabbitMQException(string message) : base(message)
        {

        }
    }
}
