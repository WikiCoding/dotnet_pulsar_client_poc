using Pulsar.Client.Api;
using Pulsar.Client.Common;
using System.Text;

namespace dotnet_pulsar_client_poc.PulsarConsumer;

public class PulsarMultipleConsumers : BackgroundService
{
    private readonly List<string> _topics = new List<string> { "Topic1", "Topic2" };
    private readonly List<IConsumer<byte[]>> _consumers = [];
    private readonly ILogger<PulsarMultipleConsumers> _logger;

    public PulsarMultipleConsumers(PulsarClient client, ILogger<PulsarMultipleConsumers> logger)
    {
        foreach (var topic in _topics)
        {
            var consumer = client.NewConsumer()
           .Topic(topic)
           .SubscriptionName($"{topic}-subscription-name")
           .SubscriptionType(SubscriptionType.Failover)
           .ConsumerName($"Consumer-{Guid.NewGuid()}") // not required
           .SubscribeAsync().Result;

            _consumers.Add(consumer);
        }

        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();

        foreach (var consumer in _consumers)
        {
            var consumerTask = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogWarning("Running thread {} for Topic {}", Environment.CurrentManagedThreadId, consumer.Topic);
                        await ConsumeMessage(consumer, stoppingToken);
                        _logger.LogWarning("After consumed, we're on thread {} for Topic {}", Environment.CurrentManagedThreadId, consumer.Topic);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while receiving a message.");
                    }
                }
            }, stoppingToken);

            tasks.Add(consumerTask);
        }

        await Task.WhenAll(tasks);
    }

    private async Task ConsumeMessage(IConsumer<byte[]> consumer, CancellationToken stoppingToken)
    {
        var message = await consumer.ReceiveAsync(stoppingToken);

        var msgString = Encoding.UTF8.GetString(message.GetValue());

        _logger.LogInformation("Received message from {topic}: {msgString}. On Thread {thread}", consumer.Topic, msgString, Environment.CurrentManagedThreadId);

        await consumer.AcknowledgeAsync(message.MessageId);
    }
}
