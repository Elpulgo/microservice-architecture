using System;
using System.Collections.Generic;

namespace batch_webservice
{
    public static class Constants
    {
        public static Dictionary<MqttType, MqttBinding> MqttBindings
         => new Dictionary<MqttType, MqttBinding>
                {
                    {
                        MqttType.BatchPublish,
                        new MqttBinding {
                            QueueName = "batch",
                            ExchangeName = "exchange_batch",
                            RoutingKey = "batch"
                        }
                    },
                    {
                        MqttType.BatchConsumeReply,
                        new MqttBinding {
                            QueueName = "batch_reply",
                            ExchangeName = "exchange_batch_reply",
                            RoutingKey = "batch_reply"
                        }
                    }
                };
    }

    public class MqttBinding
    {
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }
        public string RoutingKey { get; set; }

    }

    public enum MqttType
    {
        BatchPublish = 0,
        BatchConsumeReply = 1
    }
}