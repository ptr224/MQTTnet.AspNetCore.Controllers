using MQTTnet;
using MQTTnet.AspNetCore.Controllers;

namespace MqttTest;

public class MqttService
{
    private readonly ILogger<MqttService> _logger;
    private readonly IMqttContextAccessor _mqttContextAccessor;
    private readonly IMqttBroker _broker;

    public MqttService(ILogger<MqttService> logger, IMqttContextAccessor mqttContextAccessor, IMqttBroker broker)
    {
        _logger = logger;
        _mqttContextAccessor = mqttContextAccessor;
        _broker = broker;
    }

    public async ValueTask Answer()
    {
        var context = _mqttContextAccessor.PublishContext;

        if (context is null)
            _logger.LogWarning("Not a publish event");
        else
            await _broker.Send(new MqttApplicationMessageBuilder()
                .WithTopic($"{context.ApplicationMessage.Topic}/ans")
                .WithPayload(context.ApplicationMessage.PayloadSegment)
                .WithQualityOfServiceLevel(context.ApplicationMessage.QualityOfServiceLevel)
                .WithRetainFlag(context.ApplicationMessage.Retain)
                .Build()
            );
    }
}
