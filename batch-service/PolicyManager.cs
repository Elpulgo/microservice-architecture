using System;
using Polly;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace batch_webservice
{
    public interface IPolicyManager
    {
        CircuitBreakerPolicy PolicyRabbitMQPublish { get; }

        CircuitBreakerPolicy PolicyRedisConnection { get; }

        CircuitBreakerPolicy PolicyRedisServer { get; }
    }

    public class PolicyManager : IPolicyManager
    {
        public CircuitBreakerPolicy PolicyRabbitMQPublish
            => Policy
                .Handle<RabbitMQ.Client.Exceptions.BrokerUnreachableException>()
                .Or<RabbitMQ.Client.Exceptions.AlreadyClosedException>()
                .Or<RabbitMQ.Client.Exceptions.RabbitMQClientException>()
                .CircuitBreaker(3, TimeSpan.FromSeconds(30));

        public CircuitBreakerPolicy PolicyRedisConnection
            => Policy
                .Handle<RedisConnectionException>()
                .Or<RedisException>()
                .Or<RedisTimeoutException>()
                .CircuitBreaker(3, TimeSpan.FromSeconds(30));

        public CircuitBreakerPolicy PolicyRedisServer
            => Policy.Handle<RedisException>()
                .Or<RedisServerException>()
                .CircuitBreaker(3, TimeSpan.FromSeconds(30));

        public PolicyManager()
        {

        }
    }
}