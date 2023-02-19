using MQTTnet.Server;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers;

public interface IMqttBroker
{
    Task Send(MqttApplicationMessage message);
}

public interface IMqttAuthenticationHandler
{
    ValueTask AuthenticateAsync(ValidatingConnectionEventArgs context);
}

public interface IMqttConnectionHandler
{
    ValueTask ClientConnectedAsync(ClientConnectedEventArgs context);
    ValueTask ClientDisconnectedAsync(ClientDisconnectedEventArgs context);
}

public interface IMqttContextAccessor
{
    InterceptingPublishEventArgs? PublishContext { get; set; }
    InterceptingSubscriptionEventArgs? SubscriptionContext { get; set; }
}

public delegate ValueTask MqttActionFilterDelegate();

public interface IMqttActionFilter
{
    ValueTask OnActionAsync(ActionContext context, MqttActionFilterDelegate next);
}

public interface IMqttModelBinder
{
    ValueTask BindModelAsync(ModelBindingContext context);
}
