using dotnet_pulsar_client_poc.Config;
using dotnet_pulsar_client_poc.Producer;
using dotnet_pulsar_client_poc.PulsarConsumer;
using Pulsar.Client.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
builder.Services.AddHostedService<PulsarMultipleConsumers>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();