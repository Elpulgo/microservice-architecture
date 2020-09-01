using System;
using Polly;
using Polly.CircuitBreaker;

namespace batch_webservice
{
    public interface IPolicyManager
    {
        CircuitBreakerPolicy PolicyRabbitMQPublish { get; }
    }

    public class PolicyManager : IPolicyManager
    {
        public CircuitBreakerPolicy PolicyRabbitMQPublish
            => Policy
                .Handle<RabbitMQ.Client.Exceptions.BrokerUnreachableException>()
                .Or<RabbitMQ.Client.Exceptions.AlreadyClosedException>()
                .Or<RabbitMQ.Client.Exceptions.RabbitMQClientException>()
                .CircuitBreaker(3, TimeSpan.FromSeconds(30));

        public PolicyManager()
        {

        }
    }
}