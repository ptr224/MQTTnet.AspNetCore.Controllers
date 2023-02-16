using MQTTnet.Server;
using System;

namespace MQTTnet.AspNetCore.Controllers;

public abstract class MqttControllerBase
{
    private ControllerContext controllerContext;
    public ControllerContext ControllerContext
    {
        get
        {
            controllerContext ??= new ControllerContext();
            return controllerContext;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            controllerContext = value;
        }
    }

    public InterceptingPublishEventArgs PublishContext => ControllerContext.PublishEventArgs;
    public InterceptingSubscriptionEventArgs SubscriptionContext => ControllerContext.SubscriptionEventArgs;
}
