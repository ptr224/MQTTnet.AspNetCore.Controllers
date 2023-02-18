using MQTTnet;
using MQTTnet.AspNetCore.Controllers;

namespace MqttTest;

[ActionFilter2(Order = 1)]
[StringModelBinder2]
public abstract class MqttControllerDefault : MqttControllerBase
{
    [ActionFilter4(Order = 1)]
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
