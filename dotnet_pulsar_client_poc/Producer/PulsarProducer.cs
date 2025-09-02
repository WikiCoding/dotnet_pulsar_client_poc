using System.Text;
using dotnet_pulsar_client_poc.Config;
using Pulsar.Client.Api;
using Pulsar.Client.Common;

namespace dotnet_pulsar_client_poc.Producer;

public class PulsarProducer(PulsarClient pulsarClient, ILogger<PulsarProducer> logger, PulsarSettings pulsarSettings)
{
    public async Task ProduceAsync(string message)
    {
        var producer = await pulsarClient.NewProducer()
            .Topic(pulsarSettings.Topic)
            .CompressionType(CompressionType.None) // not required, I'm setting the default value anyway
            .EnableBatching(false)
            .ProducerName($"Producer-{Guid.NewGuid()}") // not required
            .CreateAsync();

        var msg = Encoding.UTF8.GetBytes(message);
        
        logger.LogInformation("Sending message {message}", message);

        await producer.SendAsync(msg);
    }
}