using MQTTnet.Server;

namespace MQTTnet.AspNetCore.Controllers;

public abstract class MqttControllerBase
{
    public InterceptingPublishEventArgs PublishContext { get; internal set; }
    public InterceptingSubscriptionEventArgs SubscriptionContext { get; internal set; }
}
