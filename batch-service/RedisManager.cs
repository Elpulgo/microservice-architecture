using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace batch_webservice
{
    public interface IRedisManager
    {
        IEnumerable<string> GetAllBatchKeys();
        Task<List<Tuple<string, string>>> GetValuesForBatchKey(string hashKey);
    }

    public class RedisManager : IRedisManager
    {
        private const string Pattern = "*";
        private readonly Lazy<ConnectionMultiplexer> m_RedisConnection = new Lazy<ConnectionMultiplexer>(() => OpenConnection());
        private readonly IPolicyManager m_PolicyManager;

        public RedisManager(IPolicyManager policyManager)
        {
            m_PolicyManager = policyManager;
        }

        public IEnumerable<string> GetAllBatchKeys()
        {
            var keyScan = Enumerable.Empty<RedisKey>();
            IDatabase database = null;

            try
            {
                keyScan = GetServer().Keys(pattern: "*");

                if (!keyScan.Any())
                    yield break;

                database = GetRedisInstance();
            }
            catch (Exception exception)
            {
                throw new RedisException(exception.Message);
            }

            var keysWithTypes = keyScan.ToDictionary(key => key.ToString(), key => database.KeyType(key));

            foreach (var redisValue in keysWithTypes.Where(kwt => kwt.Value == RedisType.Hash).Select(kwt => kwt.Key).ToList())
            {
                yield return redisValue;
            }
        }

        public async Task<List<Tuple<string, string>>> GetValuesForBatchKey(string hashKey)
        {
            try
            {
                var allHashKeys = await GetRedisInstance().HashGetAllAsync(hashKey);
                return allHashKeys
                    .Select(hashEntry => new Tuple<string, string>(hashEntry.Name.ToString(), hashEntry.Value.ToString()))
                    .ToList();
            }
            catch (Exception exception)
            {
                throw new RedisException(exception.Message);
            }
        }

        private IDatabase GetRedisInstance()
            => m_PolicyManager.PolicyRedisConnection.Execute(() => m_RedisConnection.Value.GetDatabase());

        private static ConnectionMultiplexer OpenConnection()
            => ConnectionMultiplexer.Connect(GetRedisUrl());

        private IServer GetServer()
            => m_PolicyManager.PolicyRedisServer.Execute(() => m_RedisConnection.Value.GetServer(GetRedisUrl()));

        private static string GetRedisUrl()
        {
            var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
            if (string.IsNullOrEmpty(redisUrl))
            {
                Console.WriteLine("Failed to access environment variable for Redis 'REDIS_URL'! Check your environment!");
                Environment.Exit(-1);
            }

            return redisUrl;
        }
    }

    public class RedisException : Exception
    {
        public RedisException(string message) : base(message)
        {

        }
    }
}