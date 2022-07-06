namespace MQTTnet.AspNetCore.Controllers;

public abstract class MqttSubscriptionController : MqttBaseController
{
    public SubscriptionContext Context { get; internal set; }
}
