using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace batch_webservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BatchController : ControllerBase
    {
        private readonly IRabbitMQClient m_RabbitMQClient;
        private readonly IRedisManager m_RedisManager;


        public BatchController(
            IRabbitMQClient rabbitMQClient,
            IRedisManager redisManager)
        {
            m_RabbitMQClient = rabbitMQClient;
            m_RedisManager = redisManager;
        }

        [HttpPost]
        public IActionResult Post()
        {
            m_RabbitMQClient.PublishBatch();
            return Ok();
        }

        [HttpGet]
        [Route("batchkeys")]
        public IActionResult Get()
        {
            var batchKeys = m_RedisManager.GetAllBatchKeys();
            return Ok(batchKeys);
        }

        [HttpGet]
        [Route("batchvalues/{hashKey}")]
        public async Task<IActionResult> GetValuesForBatch(string hashKey)
        {
            var batchEntries = await m_RedisManager.GetValuesForBatchKey(hashKey);

            var dtoEntries = batchEntries
            .Select(entry => new KeyValueModel()
                {
                    Key = entry.Item1,
                    Value = entry.Item2
                })
            .ToList();

            return Ok(dtoEntries);
        }
    }

    // Was lazy,, should be in own file
    internal class KeyValueModel
    {
        public KeyValueModel()
        {

        }

        public string Key { get; set; }
        public string Value { get; set; }
    }
}
