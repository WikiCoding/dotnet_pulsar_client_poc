using System.Text;
using dotnet_pulsar_client_poc.Config;
using dotnet_pulsar_client_poc.Producer;
using dotnet_pulsar_client_poc.PulsarConsumer;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Moq;
using Pulsar.Client.Api;
using Pulsar.Client.Common;

namespace PulsarTests;

public class PulsarIntegrationTests : IAsyncLifetime
{
    private IContainer _pulsarContainer;
    private const int PulsarPort = 6650;
    
    public async Task InitializeAsync()
    {
        _pulsarContainer = new ContainerBuilder()
            .WithImage("apachepulsar/pulsar:latest")
            .WithPortBinding(PulsarPort, true)
            .WithCommand("bin/pulsar", "standalone")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
            .Build();
        
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        await _pulsarContainer.StartAsync(timeoutCts.Token);
    }

    public async Task DisposeAsync()
    {
        await _pulsarContainer.DisposeAsync();
    }
    
    [Fact]
    public async Task Should_Consume_Message_From_Pulsar()
    {
        var pulsarUrl = $"pulsar://localhost:{_pulsarContainer.GetMappedPublicPort(PulsarPort)}";
    
        var pulsarSettings = new PulsarSettings
        {
            SubscriptionName = $"dotnet_pulsar_poc-{Guid.NewGuid()}:N",
            InitialPosition = "Earliest",
            Token = "",
            Topic = "persistent://public/default/mytopic",
            Url = pulsarUrl
        };
        
        var logger = new Mock<ILogger<PulsarConsumer>>();
        
        var pulsarClient = await new PulsarClientBuilder()
            .ServiceUrl(pulsarSettings.Url)
            .AllowTlsInsecureConnection(true)
            .Authentication(AuthenticationFactory.Token(pulsarSettings.Token))
            .BuildAsync();
        
        var pulsarConsumer = new PulsarConsumer(logger.Object, pulsarClient, pulsarSettings);
        
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await pulsarConsumer.StartAsync(cts.Token);
        
        var producer = await pulsarClient.NewProducer()
            .Topic(pulsarSettings.Topic)
            .CompressionType(CompressionType.None) // not required, I'm setting the default value anyway
            .EnableBatching(false)
            .ProducerName($"Producer-{Guid.NewGuid()}") // not required
            .CreateAsync();
        
        const string message = "Hello World";
    
        await producer.SendAsync(Encoding.UTF8.GetBytes(message));

        const string firstLogInfo = "Consuming pulsar events from Earliest position";
        const string expected = "Received message: Hello World";
        
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString().Contains(firstLogInfo) || o.ToString().Contains(expected)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Should_Produce_Message_To_Pulsar()
    {
        var pulsarUrl = $"pulsar://localhost:{_pulsarContainer.GetMappedPublicPort(PulsarPort)}";
    
        var pulsarSettings = new PulsarSettings
        {
            SubscriptionName = $"dotnet_pulsar_poc-{Guid.NewGuid()}:N",
            InitialPosition = "Earliest",
            Token = "",
            Topic = "persistent://public/default/mytopic",
            Url = pulsarUrl
        };
        
        var logger = new Mock<ILogger<PulsarProducer>>();
        
        var pulsarClient = await new PulsarClientBuilder()
            .ServiceUrl(pulsarSettings.Url)
            .AllowTlsInsecureConnection(true)
            .Authentication(AuthenticationFactory.Token(pulsarSettings.Token))
            .BuildAsync();
        
        var pulsarProducer = new PulsarProducer(pulsarClient, logger.Object, pulsarSettings);
        
        const string message = "Hello World";
    
        await pulsarProducer.ProduceAsync(message);
        
        const string expected = "Sending message Hello World";
        
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString().Contains(expected)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}