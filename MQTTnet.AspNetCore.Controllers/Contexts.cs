using MQTTnet.Server;

namespace MQTTnet.AspNetCore.Controllers;

public sealed class ControllerContext
{
    public InterceptingPublishEventArgs? PublishEventArgs { get; set; }
    public InterceptingSubscriptionEventArgs? SubscriptionEventArgs { get; set; }
}
