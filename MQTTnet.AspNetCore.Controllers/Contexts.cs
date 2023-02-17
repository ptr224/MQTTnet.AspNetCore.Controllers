using MQTTnet.Server;
using System;
using System.Collections.Generic;

namespace MQTTnet.AspNetCore.Controllers;

public sealed class MqttContext
{
    public InterceptingPublishEventArgs? PublishEventArgs { get; set; }
    public InterceptingSubscriptionEventArgs? SubscriptionEventArgs { get; set; }
}

public class ActionContext
{
    public MqttControllerBase Controller { get; }
    public IServiceProvider Services { get; }
    public IDictionary<string, object?> Parameters { get; }

    public MqttContext MqttContext => Controller.MqttContext;

    public ActionContext(MqttControllerBase controller, IServiceProvider serviceProvider, IDictionary<string, object?> parameters)
    {
        Controller = controller;
        Services = serviceProvider;
        Parameters = parameters;
    }
}
