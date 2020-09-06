using System;
using Polly.CircuitBreaker;
using RabbitMQ.Client;

namespace batch_webservice
{
    public interface IMessagePublisher
    {
        void PublishBatch();
    }

    public class MessagePublisher : IMessagePublisher, IDisposable
    {
        private const int BatchSize = 10;
        private readonly IPolicyManager m_PolicyManager;
        private readonly IRabbitMQClient m_RabbitMQClient;
        private readonly IModel m_Channel;
        private MqttBinding MqttBinding => Constants.MqttBindings[MqttType.BatchPublish];

        public MessagePublisher(IPolicyManager policyManager, IRabbitMQClient rabbitMQClient)
        {
            m_PolicyManager = policyManager;
            m_RabbitMQClient = rabbitMQClient;
            m_Channel = m_RabbitMQClient.SetupMQTTBindings(MqttBinding);
        }

        public void PublishBatch()
        {
            EnsureChannelIsOpen();
            EnsureCircuitIsClosed();

            var batchKey = Guid.NewGuid();

            for (var index = 1; index <= BatchSize; index++)
            {
                var batchByteArray = CreateBatchAsByteArray(batchKey, index);
                var properties = GetPublishHeaders();

                try
                {
                    m_PolicyManager.PolicyRabbitMQPublish.Execute(() => m_Channel.BasicPublish(
                            exchange: MqttBinding.ExchangeName,
                            routingKey: MqttBinding.QueueName,
                            basicProperties: properties,
                            body: batchByteArray));
                }
                catch (Exception circuitException)
                {
                    Console.WriteLine($"Circuit is open, will abort sending batch! Sent {index} / {BatchSize} for batchkey '{batchKey}'. Exception: {circuitException.Message}");
                    throw new RabbitMQException($"Circuit is open, will abort sending batch! Sent {index} / {BatchSize}: {circuitException.Message}");
                }
            }

            Console.WriteLine($"Published batch '{batchKey}'!");
        }

        private void EnsureChannelIsOpen()
        {
            if (m_Channel.IsOpen)
                return;

            Console.WriteLine("Channel is closed, won't send batch ...");
            throw new RabbitMQException("MQTT channel is closed!");
        }

        private void EnsureCircuitIsClosed()
        {
            if (m_PolicyManager.PolicyRabbitMQPublish.CircuitState == CircuitState.Open ||
                m_PolicyManager.PolicyRabbitMQPublish.CircuitState == CircuitState.Isolated)
            {
                Console.WriteLine("Circuit is open, won't send batch ...");
                throw new RabbitMQException("Circuit is open or isolated, won't try and send batch!");
            }
        }

        private byte[] CreateBatchAsByteArray(Guid batchKey, int index)
         => new Batch(
                    hashKey: batchKey,
                    key: $"{batchKey}_key_{index}",
                    value: $"{batchKey}_value_{index}",
                    batchSize: BatchSize,
                    isLastInBatch: index == BatchSize
                )
                .ToByteArray();

        private IBasicProperties GetPublishHeaders()
        {
            var properties = m_Channel.CreateBasicProperties();

            properties.DeliveryMode = 2;
            properties.ContentType = "application/json";

            return properties;
        }

        public void Dispose()
        {
            m_Channel.Close();
        }
    }
}