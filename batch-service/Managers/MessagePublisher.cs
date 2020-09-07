using System;
using System.Collections.Concurrent;
using System.Threading;
using Polly.CircuitBreaker;
using RabbitMQ.Client;

namespace batch_webservice
{
    public interface IMessagePublisher
    {
        BatchStatus PublishBatch();
    }

    public class MessagePublisher : IMessagePublisher, IDisposable
    {
        private const int BatchSize = 10;
        private const int BatchReplyTimeoutLimitSeonds = 20;
        private readonly IPolicyManager m_PolicyManager;
        private readonly IRabbitMQClient m_RabbitMQClient;
        private readonly IMessageConsumer m_MessageConsumer;
        private readonly IModel m_Channel;
        private MqttBinding MqttBinding => Constants.MqttBindings[MqttType.BatchPublish];

        private ConcurrentDictionary<Guid, BatchStatus> m_BatchReplyMap;

        public MessagePublisher(
            IPolicyManager policyManager,
            IRabbitMQClient rabbitMQClient,
            IMessageConsumer messageConsumer)
        {
            m_BatchReplyMap = new ConcurrentDictionary<Guid, BatchStatus>();
            m_PolicyManager = policyManager;
            m_RabbitMQClient = rabbitMQClient;
            m_MessageConsumer = messageConsumer;
            m_Channel = m_RabbitMQClient.SetupMQTTBindings(MqttBinding);
            m_MessageConsumer.BatchReplyEventChanged += OnBatchReplyChanged;
        }

        public BatchStatus PublishBatch()
        {
            EnsureChannelIsOpen();
            EnsureCircuitIsClosed();

            var batchKey = Guid.NewGuid();
            m_BatchReplyMap.TryAdd(batchKey, BatchStatus.None);

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
                    m_BatchReplyMap.TryRemove(batchKey, out var _);
                    throw new RabbitMQException($"Circuit is open, will abort sending batch! Sent {index} / {BatchSize}: {circuitException.Message}");
                }
            }

            Console.WriteLine($"Published batch '{batchKey}'!");

            return WaitForReply(batchKey);
        }

        private BatchStatus WaitForReply(Guid batchKey)
        {
            int retries = 0;

            while (retries < BatchReplyTimeoutLimitSeonds)
            {
                if (!m_BatchReplyMap.TryGetValue(batchKey, out BatchStatus status))
                {
                    Thread.Sleep(1000);
                    retries++;
                    continue;
                }

                Console.WriteLine($"Got reply for batch key '{batchKey}' with status '{status}'!");
                m_BatchReplyMap.TryRemove(batchKey, out var _);
                return status;
            }

            m_BatchReplyMap.TryRemove(batchKey, out var _);
            throw new RabbitMQException($"Waited {BatchReplyTimeoutLimitSeonds} seconds for reply from batch with key '{batchKey}' but reply never arrived!");
        }


        private void OnBatchReplyChanged(object sender, BatchReply reply)
        {
            if (reply == null)
            {
                Console.WriteLine($"Got response from BatchReply, but reply was null...");
                return;
            }

            if (!Guid.TryParse(reply.Key, out Guid key))
            {
                Console.WriteLine($"Got response from BatchReply, but couldn't parse the key '{reply.Key}' to guid...");
                return;
            }

            if (!m_BatchReplyMap.ContainsKey(key))
            {
                Console.WriteLine($"Got reply for key '{key}', but too late, call is already disposed.");
                return;
            }

            if (!m_BatchReplyMap.TryAdd(key, reply.Status))
            {
                Console.WriteLine($"Failed to add key '{key}' to intermittent map!");
            }
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
            m_MessageConsumer.BatchReplyEventChanged -= OnBatchReplyChanged;
            m_Channel.Close();
        }
    }
}