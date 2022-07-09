using MQTTnet;
using MQTTnet.AspNetCore.Controllers;

namespace MqttTest.MqttControllers;

public class MqttController : MqttControllerBase
{
    private readonly ILogger<MqttController> _logger;

    public MqttController(ILogger<MqttController> logger)
    {
        _logger = logger;
    }

    [MqttPublish("+/+/#")]
    public IMqttPublishResult Answer()
    {
        _logger.LogInformation("Message from \"{clientId}\": {payload}", PublishContext.ClientId, PublishContext.ApplicationMessage.ConvertPayloadToString());
        return Publish();
    }

    [MqttPublish("{serial}/kickout")]
    public IMqttPublishResult ManageKickout(string serial)
    {
        _logger.LogInformation("Message from {serial} : {payload}", serial, PublishContext.ApplicationMessage.ConvertPayloadToString());
        return KickOutPublish();
    }

    [MqttPublish("{serial}/stop")]
    public IMqttPublishResult ManageStop(string serial)
    {
        _logger.LogInformation("Message from {serial} : {payload}", serial, PublishContext.ApplicationMessage.ConvertPayloadToString());
        return StopPublish();
    }

    [MqttPublish("{serial}/forbid")]
    public IMqttPublishResult ManageForbid(string serial)
    {
        _logger.LogInformation("Message from {serial} : {payload}", serial, PublishContext.ApplicationMessage.ConvertPayloadToString());
        return ForbidPublish();
    }

    [MqttPublish("{serial}/publish")]
    public IMqttPublishResult ManagePublish(string serial)
    {
        _logger.LogInformation("Message from {serial} : {payload}", serial, PublishContext.ApplicationMessage.ConvertPayloadToString());
        return Publish();
    }

    [MqttSubscribe("+")]
    public IMqttSubscribeResult Root()
    {
        _logger.LogInformation("Accept subscription to: {topic}", SubscriptionContext.TopicFilter.Topic);
        return Subscribe();
    }

    [MqttSubscribe("+/si/#")]
    public IMqttSubscribeResult Accept()
    {
        _logger.LogInformation("Accept subscription to: {topic}", SubscriptionContext.TopicFilter.Topic);
        return Subscribe();
    }

    [MqttSubscribe("+/no/#")]
    public IMqttSubscribeResult Forbid()
    {
        _logger.LogInformation("Forbid subscription to: {topic}", SubscriptionContext.TopicFilter.Topic);
        return ForbidSubscribe();
    }
}
