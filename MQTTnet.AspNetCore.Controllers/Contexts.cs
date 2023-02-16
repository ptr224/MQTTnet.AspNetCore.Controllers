using MQTTnet.Server;

namespace MQTTnet.AspNetCore.Controllers;

public sealed class MqttContext
{
    public InterceptingPublishEventArgs? PublishEventArgs { get; set; }
    public InterceptingSubscriptionEventArgs? SubscriptionEventArgs { get; set; }
}

public class ActionContext
{
    public MqttControllerBase Controller { get; }

    public MqttContext MqttContext => Controller.MqttContext;

    public ActionContext(MqttControllerBase controller)
    {
        Controller = controller;
    }
}
