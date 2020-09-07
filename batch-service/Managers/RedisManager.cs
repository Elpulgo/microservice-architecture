using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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
        private readonly Lazy<ConnectionMultiplexer> m_RedisConnection;
        private readonly IPolicyManager m_PolicyManager;

        private IConfiguration Configuration { get; }
        private string RedisUrl => Configuration["REDIS_URL"];
        public RedisManager(
            IConfiguration configuration,
            IPolicyManager policyManager)
        {
            Configuration = configuration;
            m_RedisConnection = new Lazy<ConnectionMultiplexer>(() => OpenConnection());
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
                throw new RedisException($"Failed to get all batch keys: {exception.Message}");
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
                throw new RedisException($"Failed to get values for batch key '{hashKey}': {exception.Message}");
            }
        }

        private IDatabase GetRedisInstance()
            => m_PolicyManager.PolicyRedisConnection.Execute(() => m_RedisConnection.Value.GetDatabase());

        private ConnectionMultiplexer OpenConnection()
            => ConnectionMultiplexer.Connect(RedisUrl);

        private IServer GetServer()
            => m_PolicyManager.PolicyRedisServer.Execute(() => m_RedisConnection.Value.GetServer(RedisUrl));
    }

    public class RedisException : Exception
    {
        public RedisException(string message) : base(message)
        {

        }
    }
}