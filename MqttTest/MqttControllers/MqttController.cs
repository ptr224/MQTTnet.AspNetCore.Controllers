using MQTTnet;
using MQTTnet.AspNetCore.Controllers;
using MqttTest.Services;

namespace MqttTest.MqttControllers;

internal abstract class ActionFilterAttribute : MqttActionFilterAttribute
{
    private readonly int _value;

    public ActionFilterAttribute(int value)
    {
        _value = value;
    }

    public override async ValueTask OnActionAsync(ActionContext context, MqttActionFilterDelegate next)
    {
        var logger = context.Services.GetRequiredService<ILogger<ActionFilterAttribute>>();

        if (context.Parameters.TryGetValue("serial", out var serial) && serial is not null)
        {
            logger.LogDebug("Serial = {serial}", serial);
        }

        logger.LogDebug("Filter {value} begin", _value);
        await next();
        logger.LogDebug("Filter {value} end", _value);
    }
}

internal class ActionFilter1Attribute : ActionFilterAttribute
{
    public ActionFilter1Attribute() : base(1)
    { }
}

internal class ActionFilter2Attribute : ActionFilterAttribute
{
    public ActionFilter2Attribute() : base(2)
    { }
}

internal class ActionFilter3Attribute : ActionFilterAttribute
{
    public ActionFilter3Attribute() : base(3)
    { }
}

internal class ActionFilter4Attribute : ActionFilterAttribute
{
    public ActionFilter4Attribute() : base(4)
    { }
}

[ActionFilter4(Order = 1)]
public abstract class MqttControllerDefault : MqttControllerBase
{
    [ActionFilter2(Order = 1)]
    [MqttPublish("+/we")]
    public virtual void Test() { }
}

[ActionFilter3]
public class MqttController : MqttControllerDefault
{
    private readonly ILogger<MqttController> _logger;
    private readonly MqttService _service;

    public MqttController(ILogger<MqttController> logger, MqttService service)
    {
        _logger = logger;
        _service = service;
    }

    [ActionFilter1]
    [MqttPublish("+/dai")]
    public override void Test()
    {
        base.Test();
    }

    [MqttPublish("+/+/#")]
    public ValueTask Answer()
    {
        PublishContext.ProcessPublish = true;
        _logger.LogInformation("Message from {clientId} : {payload}", PublishContext.ClientId, PublishContext.ApplicationMessage.ConvertPayloadToString());
        return _service.Answer();
    }

    [MqttPublish("{serial}/kickout")]
    public void ManageKickout(string serial)
    {
        PublishContext.CloseConnection = true;
        _logger.LogInformation("Message from {serial} : {payload}", serial, PublishContext.ApplicationMessage.ConvertPayloadToString());
    }

    [MqttPublish("{serial}/stop")]
    public void ManageStop(string serial)
    {
        PublishContext.ProcessPublish = false;
        _logger.LogInformation("Message from {serial} : {payload}", serial, PublishContext.ApplicationMessage.ConvertPayloadToString());
    }

    [MqttPublish("{serial}/publish")]
    public void ManagePublish(string serial)
    {
        PublishContext.ProcessPublish = true;
        _logger.LogInformation("Message from {serial} : {payload}", serial, PublishContext.ApplicationMessage.ConvertPayloadToString());
    }

    [MqttSubscribe("+")]
    public ValueTask Root()
    {
        SubscriptionContext.ProcessSubscription = true;
        _logger.LogInformation("Accept subscription to {topic}", SubscriptionContext.TopicFilter.Topic);
        return _service.Answer();
    }

    [MqttSubscribe("{serial}/si/#")]
    public void Accept(string serial)
    {
        SubscriptionContext.ProcessSubscription = true;
        _logger.LogInformation("Accept subscription to {topic}", SubscriptionContext.TopicFilter.Topic);
    }

    [MqttSubscribe("+/no/#")]
    public void Forbid()
    {
        SubscriptionContext.ProcessSubscription = false;
        _logger.LogInformation("Forbid subscription to {topic}", SubscriptionContext.TopicFilter.Topic);
    }
}
