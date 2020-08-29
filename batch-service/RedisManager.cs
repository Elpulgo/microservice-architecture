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

        public RedisManager()
        {

        }

        public IEnumerable<string> GetAllBatchKeys()
        {
            var keyScan = GetServer().Keys(pattern: "*");

            if (!keyScan.Any())
                yield break;

            var database = m_RedisConnection.Value.GetDatabase();
            var keysWithTypes = keyScan.ToDictionary(key => key.ToString(), key => database.KeyType(key));

            foreach (var redisValue in keysWithTypes.Where(kwt => kwt.Value == RedisType.Hash).Select(kwt => kwt.Key).ToList())
            {
                yield return redisValue;
            }
        }

        public async Task<List<Tuple<string, string>>> GetValuesForBatchKey(string hashKey)
            => (await m_RedisConnection.Value
                .GetDatabase()
                .HashGetAllAsync(hashKey))
                .Select(hashEntry => new Tuple<string, string>(hashEntry.Name.ToString(), hashEntry.Value.ToString()))
                .ToList();

        private static ConnectionMultiplexer OpenConnection()
            => ConnectionMultiplexer.Connect(GetRedisUrl());

        private IServer GetServer()
            => m_RedisConnection.Value.GetServer(GetRedisUrl());

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
}