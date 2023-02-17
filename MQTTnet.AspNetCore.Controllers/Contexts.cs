using MQTTnet.Server;
using System;

namespace MQTTnet.AspNetCore.Controllers;

public sealed class MqttContext
{
    public InterceptingPublishEventArgs? PublishEventArgs { get; set; }
    public InterceptingSubscriptionEventArgs? SubscriptionEventArgs { get; set; }
}

public class ActionContext
{
    public IServiceProvider Services { get; }
    public MqttControllerBase Controller { get; }

    public MqttContext MqttContext => Controller.MqttContext;

    public ActionContext(MqttControllerBase controller, IServiceProvider serviceProvider)
    {
        Controller = controller;
        Services = serviceProvider;
    }
}
