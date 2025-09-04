# Dependency
1. Pulsar.Client

# Local service
```yaml
services:
  pulsar:
    image: apachepulsar/pulsar:latest
    container_name: pulsar
    ports:
      - "6650:6650"
      - "8080:8080"
    command: >
      bin/pulsar standalone
    environment:
      PULSAR_MEM: "-Xms512m -Xmx512m -XX:MaxDirectMemorySize=1g"
```

# Program.cs

1. We need to inject the Pulsar client, producer and consumer.
```csharp
var pulsarSettings = builder.Configuration.GetSection("Pulsar").Get<PulsarSettings>() ?? new PulsarSettings();

builder.Services.AddSingleton(pulsarSettings);

var pulsarClient = await new PulsarClientBuilder()
    .ServiceUrl(pulsarSettings.Url)
    .AllowTlsInsecureConnection(true)
    .Authentication(AuthenticationFactory.Token(pulsarSettings.Token))
    .BuildAsync();

builder.Services.AddSingleton(pulsarClient);
builder.Services.AddScoped<PulsarProducer>();
builder.Services.AddHostedService<PulsarConsumer>();
```

# Producer
```csharp
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
```

# Consumer
```csharp
public class PulsarConsumer(ILogger<PulsarConsumer> logger, PulsarClient pulsarClient, PulsarSettings pulsarSettings)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Consuming pulsar events from {} position", 
            Enum.Parse<SubscriptionInitialPosition>(pulsarSettings.InitialPosition));
        
        var consumer = await pulsarClient.NewConsumer()
            .Topic(pulsarSettings.Topic)
            .SubscriptionName(pulsarSettings.SubscriptionName)
            .SubscriptionType(SubscriptionType.Exclusive)
            .ConsumerName($"Consumer-{Guid.NewGuid()}") // not required
            .SubscribeAsync();
        
        var message = await consumer.ReceiveAsync(stoppingToken);
        
        var msgString = Encoding.UTF8.GetString(message.GetValue()); // converting from byte[]
            
        logger.LogInformation("Received message: {msgString}", msgString);
        
        await consumer.AcknowledgeAsync(message.MessageId);
    }
}
```

# Reference docs
1. See here: https://github.com/fsprojects/pulsar-client-dotnet || https://github.com/fsprojects/pulsar-client-dotnet/blob/develop/examples/CsharpExamples/RealWorld.cs

# Pulsar CLI Commands to use on Docker
- imagining the topic: `persistent://gpd/smf-inbound/horseracing`
1. First you need to create the tenant with the command `pulsar-admin tenants create gpd --admin-roles superuser`
2. Then you create the namespace with the command `pulsar-admin namespaces create gpd/smf-inbound --clusters standalone` (in this case for docker is standalone)
3. Now you can create the topic with `pulsar-admin topics create persistent://gpd/smf-inbound/horseracing`. If it's partitioned you must set it now.
4. To test a producer you can set a CLI consumer with the command `pulsar-client consume persistent://gpd/smf-inbound/horseracing -s my-subscription`. The consumer will drop after getting the message, in order to avoid that use `pulsar-client consume persistent://gpd/smf-inbound/horseracing -s my-subscription -n 0`
5. To list the namespaces in a tenant use the command `pulsar-admin namespaces list gpd`
6. To list the tenants use the command `pulsar-admin tenants list`