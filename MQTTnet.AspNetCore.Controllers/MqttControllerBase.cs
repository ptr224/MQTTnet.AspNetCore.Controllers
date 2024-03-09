using MQTTnet.Server;

namespace MQTTnet.AspNetCore.Controllers;

public abstract class MqttControllerBase
{
    private MqttContext? mqttContext;
    public MqttContext MqttContext
    {
        get
        {
            mqttContext ??= new MqttContext();
            return mqttContext;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            mqttContext = value;
        }
    }

    public InterceptingPublishEventArgs PublishContext => MqttContext.PublishEventArgs ?? throw new InvalidOperationException("Not a publish event");
    public InterceptingSubscriptionEventArgs SubscriptionContext => MqttContext.SubscriptionEventArgs ?? throw new InvalidOperationException("Not a subscription event");
}
