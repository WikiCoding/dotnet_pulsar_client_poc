namespace dotnet_pulsar_client_poc.Config;

public class PulsarSettings
{
    public string Url { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;
    public string InitialPosition { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}