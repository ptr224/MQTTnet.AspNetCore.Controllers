namespace MQTTnet.AspNetCore.Controllers;

public interface IMqttPublishResult
{
    bool Accept { get; }
    bool Publish { get; }
}
