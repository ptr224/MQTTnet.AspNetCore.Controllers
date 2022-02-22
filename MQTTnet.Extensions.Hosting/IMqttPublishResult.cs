namespace MQTTnet.Extensions.Hosting;

public interface IMqttPublishResult
{
    bool Accept { get; }
    bool Publish { get; }
}
