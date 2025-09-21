using dotnet_pulsar_client_poc.Producer;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_pulsar_client_poc.Controllers;

[ApiController]
[Route("[controller]")]
public class StartController(ILogger<StartController> logger, PulsarProducer producer) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] Request request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            logger.LogError("Message is null or empty");
            return BadRequest("Message can't be null or empty");
        }

        await producer.ProduceAsync(request.Message);
        
        return Ok("Sent!");
    }

    [HttpPost("topic1")]
    public async Task<IActionResult> SendMessageTopic1([FromBody] Request request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            logger.LogError("Message is null or empty");
            return BadRequest("Message can't be null or empty");
        }

        await producer.ProduceAsyncTopic1(request.Message);

        return Ok("Sent!");
    }

    [HttpPost("topic2")]
    public async Task<IActionResult> SendMessageTopic2([FromBody] Request request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            logger.LogError("Message is null or empty");
            return BadRequest("Message can't be null or empty");
        }

        await producer.ProduceAsyncTopic2(request.Message);

        return Ok("Sent!");
    }
}