﻿using MQTTnet;
using MQTTnet.AspNetCore.Controllers;

namespace MqttTest.MqttControllers;

public class MqttController : MqttControllerBase
{
    private readonly ILogger<MqttController> _logger;
    private readonly IBroker _broker;

    public MqttController(ILogger<MqttController> logger, IBroker broker)
    {
        _logger = logger;
        _broker = broker;
    }

    [MqttPublish("+/+/#")]
    public async Task Answer()
    {
        PublishContext.ProcessPublish = true;
        _logger.LogInformation("Message from {clientId} : {payload}", PublishContext.ClientId, PublishContext.ApplicationMessage.ConvertPayloadToString());
        await _broker.Send(new MqttApplicationMessageBuilder()
                .WithTopic($"{PublishContext.ApplicationMessage.Topic}/ans")
                .WithPayload(PublishContext.ApplicationMessage.Payload)
                .WithQualityOfServiceLevel(PublishContext.ApplicationMessage.QualityOfServiceLevel)
                .WithRetainFlag(PublishContext.ApplicationMessage.Retain)
                .Build()
                );
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
    public void Root()
    {
        SubscriptionContext.ProcessSubscription = true;
        _logger.LogInformation("Accept subscription to {topic}", SubscriptionContext.TopicFilter.Topic);
    }

    [MqttSubscribe("+/si/#")]
    public void Accept()
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
