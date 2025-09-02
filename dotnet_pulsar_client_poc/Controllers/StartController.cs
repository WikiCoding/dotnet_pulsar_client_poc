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
}