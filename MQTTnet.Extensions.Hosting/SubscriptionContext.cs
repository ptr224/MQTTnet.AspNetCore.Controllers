using MQTTnet.Packets;
using MQTTnet.Server;
using System.Threading;

namespace MQTTnet.Extensions.Hosting;

public class SubscriptionContext
{
    public CancellationToken RequestAborted { get; }
    public string ClientId { get; }
    public MqttTopicFilter TopicFilter { get; }

    public SubscriptionContext(InterceptingSubscriptionEventArgs context)
    {
        RequestAborted = context.CancellationToken;
        ClientId = context.ClientId;
        TopicFilter = context.TopicFilter;
    }
}
