using MQTTnet.Server;

namespace MQTTnet.Extensions.Hosting;

public sealed class MqttBrokerOptions : MqttServerOptionsBuilder
{
    internal int MaxParallelRequests { get; private set; } = 4;
    internal bool DefaultSubscriptionAccept { get; private set; } = false;
    internal bool DefaultPublishAccept { get; private set; } = false;

    public MqttBrokerOptions WithMaxParallelRequests(int value)
    {
        MaxParallelRequests = value;
        return this;
    }

    public MqttBrokerOptions WithDefaultSubscriptionAccept(bool value)
    {
        DefaultSubscriptionAccept = value;
        return this;
    }

    public MqttBrokerOptions WithDefaultPublishAccept(bool value)
    {
        DefaultPublishAccept = value;
        return this;
    }
}
