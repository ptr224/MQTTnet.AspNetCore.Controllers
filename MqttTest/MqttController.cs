using MQTTnet;
using MQTTnet.AspNetCore.Controllers;

namespace MqttTest;

public class ActionFilterTest(ILogger<ActionFilterTest> logger) : IMqttActionFilter
{
    public ValueTask OnActionAsync(ActionContext context, MqttActionFilterDelegate next)
    {
        logger.LogInformation("Test ActionFilter from service");
        return next();
    }
}

public class ModelBinderTest(ILogger<ModelBinderTest> logger) : IMqttModelBinder
{
    public ValueTask BindModelAsync(ModelBindingContext context)
    {
        logger.LogInformation("Test ModelBinder from service");
        return ValueTask.CompletedTask;
    }
}

[ActionFilter2]
[StringModelBinder2]
public abstract class MqttControllerDefault : MqttControllerBase
{
    [ActionFilter4]
    [StringModelBinder4]
    [MqttPublish("{test}/we")]
    public virtual void Test([StringModelBinder6] string test) { }
}

[ActionFilter3]
[StringModelBinder3]
public class MqttController(ILogger<MqttController> logger, MqttService service) : MqttControllerDefault
{
    [ActionFilter5]
    [StringModelBinder5]
    [MqttActionFilterService(typeof(ActionFilterTest))]
    [MqttModelBinderService(typeof(ModelBinderTest))]
    [MqttPublish("{test}/dai")]
    public override void Test([StringModelBinder7] string test)
    {
        logger.LogInformation("Test = {test}", test);
    }

    [MqttPublish("+/+/#")]
    public ValueTask Answer()
    {
        PublishContext.ProcessPublish = true;
        logger.LogInformation("Message from {clientId} : {payload}", PublishContext.ClientId, PublishContext.ApplicationMessage.ConvertPayloadToString());
        return service.Answer();
    }

    [MqttPublish("{serial}/kickout")]
    public void ManageKickout(string serial)
    {
        PublishContext.CloseConnection = true;
        logger.LogInformation("Message from {serial} : {payload}", serial, PublishContext.ApplicationMessage.ConvertPayloadToString());
    }

    [MqttPublish("{serial}/stop")]
    public void ManageStop(string serial)
    {
        PublishContext.ProcessPublish = false;
        logger.LogInformation("Message from {serial} : {payload}", serial, PublishContext.ApplicationMessage.ConvertPayloadToString());
    }

    [MqttPublish("{serial}/publish")]
    public void ManagePublish(string serial)
    {
        PublishContext.ProcessPublish = true;
        logger.LogInformation("Message from {serial} : {payload}", serial, PublishContext.ApplicationMessage.ConvertPayloadToString());
    }

    [MqttSubscribe("+")]
    public ValueTask Root()
    {
        SubscriptionContext.ProcessSubscription = true;
        logger.LogInformation("Accept subscription to {topic}", SubscriptionContext.TopicFilter.Topic);
        return service.Answer();
    }

    [MqttSubscribe("{serial}/si/#")]
    public void Accept()//string serial)
    {
        SubscriptionContext.ProcessSubscription = true;
        //_logger.LogDebug("Method param serial = {serial}", serial);
        logger.LogInformation("Accept subscription to {topic}", SubscriptionContext.TopicFilter.Topic);
    }

    [MqttSubscribe("+/no/#")]
    public Task Forbid()
    {
        SubscriptionContext.ProcessSubscription = false;
        logger.LogInformation("Forbid subscription to {topic}", SubscriptionContext.TopicFilter.Topic);
        return service.ClearRetainedMessages();
    }
}
