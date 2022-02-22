namespace MQTTnet.Extensions.Hosting;

public abstract class MqttSubscriptionController : MqttBaseController
{
    public SubscriptionContext Context { get; internal set; }
}
