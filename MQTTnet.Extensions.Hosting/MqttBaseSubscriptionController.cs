namespace MQTTnet.Extensions.Hosting;

public abstract class MqttBaseSubscriptionController
{
    public SubscriptionContext Context { get; internal set; }
}
