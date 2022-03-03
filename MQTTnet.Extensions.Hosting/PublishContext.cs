using MQTTnet.Server;

namespace MQTTnet.Extensions.Hosting;

public class PublishContext
{
    public string ClientId { get; }
    public MqttApplicationMessage ApplicationMessage { get; }

    public PublishContext(MqttApplicationMessageInterceptorContext context)
    {
        ClientId = context.ClientId;
        ApplicationMessage = context.ApplicationMessage;
    }
}
