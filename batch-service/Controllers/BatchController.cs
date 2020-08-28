using Microsoft.AspNetCore.Mvc;

namespace batch_webservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BatchController : ControllerBase
    {
        private readonly IRabbitMQClient m_RabbitMQClient;

        public BatchController(IRabbitMQClient rabbitMQClient)
        {
            m_RabbitMQClient = rabbitMQClient;
        }

        [HttpPost]
        public IActionResult Post()
        {
            m_RabbitMQClient.PublishBatch();
            return Ok();
        }
    }
}
