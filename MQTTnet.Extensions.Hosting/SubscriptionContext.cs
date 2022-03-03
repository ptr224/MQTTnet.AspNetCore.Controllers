using MQTTnet.Server;

namespace MQTTnet.Extensions.Hosting;

public class SubscriptionContext
{
    public string ClientId { get; }
    public MqttTopicFilter TopicFilter { get; }

    public SubscriptionContext(MqttSubscriptionInterceptorContext context)
    {
        ClientId = context.ClientId;
        TopicFilter = context.TopicFilter;
    }
}
