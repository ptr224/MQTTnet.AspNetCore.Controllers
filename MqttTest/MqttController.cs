using MQTTnet;
using MQTTnet.AspNetCore.Controllers;

namespace MqttTest;

public class ActionFilterTest : IMqttActionFilter
{
    private readonly ILogger<ActionFilterTest> _logger;

    public ActionFilterTest(ILogger<ActionFilterTest> logger)
    {
        _logger = logger;
    }

    public ValueTask OnActionAsync(ActionContext context, MqttActionFilterDelegate next)
    {
        _logger.LogInformation("Test ActionFilter from service");
        return next();
    }
}

public class ModelBinderTest : IMqttModelBinder
{
    private readonly ILogger<ModelBinderTest> _logger;

    public ModelBinderTest(ILogger<ModelBinderTest> logger)
    {
        _logger = logger;
    }

    public ValueTask BindModelAsync(ModelBindingContext context)
    {
        _logger.LogInformation("Test ModelBinder from service");
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
public class MqttController : MqttControllerDefault
{
    private readonly ILogger<MqttController> _logger;
    private readonly MqttService _service;

    public MqttController(ILogger<MqttController> logger, MqttService service)
    {
        _logger = logger;
        _service = service;
    }

    [ActionFilter5]
    [StringModelBinder5]
    [MqttActionFilterService(typeof(ActionFilterTest))]
    [MqttModelBinderService(typeof(ModelBinderTest))]
    [MqttPublish("{test}/dai")]
    public override void Test([StringModelBinder7] string test)
    {
        _logger.LogInformation("Test = {test}", test);
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
    public void Accept()//string serial)
    {
        SubscriptionContext.ProcessSubscription = true;
        //_logger.LogDebug("Method param serial = {serial}", serial);
        _logger.LogInformation("Accept subscription to {topic}", SubscriptionContext.TopicFilter.Topic);
    }

    [MqttSubscribe("+/no/#")]
    public void Forbid()
    {
        SubscriptionContext.ProcessSubscription = false;
        _logger.LogInformation("Forbid subscription to {topic}", SubscriptionContext.TopicFilter.Topic);
    }
}
