using Microsoft.AspNetCore.Mvc;
using System;
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
            try
            {
                m_RabbitMQClient.PublishBatch();
                return Ok();
            }
            catch (RabbitMQException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (Exception)
            {
                return BadRequest("Failed to send batch!");
            }
        }

        [HttpGet]
        [Route("batchkeys")]
        public IActionResult Get()
        {
            try
            {
                var batchKeys = m_RedisManager.GetAllBatchKeys();
                return Ok(batchKeys);
            }
            catch (RedisException redisException)
            {
                return BadRequest(redisException.Message);
            }
            catch (Exception)
            {
                return BadRequest($"Failed to get batchkeys ...");
            }
        }

        [HttpGet]
        [Route("batchvalues/{hashKey}")]
        public async Task<IActionResult> GetValuesForBatch(string hashKey)
        {
            try
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
            catch (RedisException redisException)
            {
                return BadRequest(redisException.Message);
            }
            catch (Exception)
            {
                return BadRequest($"Failed to get values for batch '{hashKey}' ...");
            }
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
