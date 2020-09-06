using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace batch_webservice
{
    public interface IMessageConsumer
    {
    }

    public class MessageConsumer : IMessageConsumer, IDisposable, IHostedService
    {
        private readonly IRabbitMQClient m_RabbitMQClient;
        private readonly IModel m_Channel;
        private MqttBinding MqttBinding => Constants.MqttBindings[MqttType.BatchConsumeReply];

        public MessageConsumer(IRabbitMQClient rabbitMQClient)
        {
            m_RabbitMQClient = rabbitMQClient;
            m_Channel = m_RabbitMQClient.SetupMQTTBindings(MqttBinding);
        }

        private void Consume()
        {
            var consumer = new EventingBasicConsumer(m_Channel);
            consumer.Received += (model, deliverEventArgs) =>
            {
                var batchReply = Deserialize(deliverEventArgs);
                if (batchReply == null)
                    return;

                Console.WriteLine($"Successfully consumed batchkey '{batchReply.Key}' with status  '{batchReply.Status}'");
            };

            m_Channel.BasicConsume(
                queue: MqttBinding.QueueName,
                autoAck: true,
                consumer: consumer);
        }

        private BatchReply Deserialize(BasicDeliverEventArgs args)
        {
            try
            {
                var body = args.Body.ToArray();
                return JsonSerializer.Deserialize<BatchReply>(body);
            }
            catch (JsonException exception)
            {
                Console.WriteLine($"Failed to deserialize incoming batchreply '{exception.Message}' ...");
                return null;
            }
        }

        public void Dispose()
        {
            m_Channel.Close();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Application starting, will start consuming...");
            Consume();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Application stopped, will stop consuming...");
            return Task.CompletedTask;
        }
    }
}