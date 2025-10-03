using System.Text;
using dotnet_pulsar_client_poc.Config;
using Pulsar.Client.Api;
using Pulsar.Client.Common;

namespace dotnet_pulsar_client_poc.PulsarConsumer;

public class PulsarConsumer(ILogger<PulsarConsumer> logger, PulsarClient pulsarClient, PulsarSettings pulsarSettings)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Consuming pulsar events from {} position", 
            Enum.Parse<SubscriptionInitialPosition>(pulsarSettings.InitialPosition));
        
        var consumer = await pulsarClient.NewConsumer()
            .Topic(pulsarSettings.Topic)
            .SubscriptionInitialPosition(Enum.Parse<SubscriptionInitialPosition>(pulsarSettings.InitialPosition))
            .SubscriptionName(pulsarSettings.SubscriptionName)
            .SubscriptionType(SubscriptionType.Failover)
            .ConsumerName($"Consumer-{Guid.NewGuid()}") // not required
            .SubscribeAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await consumer.ReceiveAsync(stoppingToken);

            var msgString = Encoding.UTF8.GetString(message.GetValue()); // converting from byte[]

            logger.LogInformation("Received message: {msgString}", msgString);

            await consumer.AcknowledgeAsync(message.MessageId);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }
}