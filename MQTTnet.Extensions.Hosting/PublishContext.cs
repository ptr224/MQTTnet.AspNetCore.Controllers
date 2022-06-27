using MQTTnet.Server;
using System.Threading;

namespace MQTTnet.Extensions.Hosting;

public class PublishContext
{
    public CancellationToken RequestAborted { get; }
    public string ClientId { get; }
    public MqttApplicationMessage ApplicationMessage { get; }

    public PublishContext(InterceptingPublishEventArgs context)
    {
        RequestAborted = context.CancellationToken;
        ClientId = context.ClientId;
        ApplicationMessage = context.ApplicationMessage;
    }
}
