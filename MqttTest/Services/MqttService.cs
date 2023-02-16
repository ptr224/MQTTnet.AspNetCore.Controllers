using MQTTnet;
using MQTTnet.AspNetCore.Controllers;

namespace MqttTest.Services;

public class MqttService
{
    private readonly ILogger<MqttService> _logger;
    private readonly IMqttContextAccessor _mqttContextAccessor;
    private readonly IBroker _broker;

    public MqttService(ILogger<MqttService> logger, IMqttContextAccessor mqttContextAccessor, IBroker broker)
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
                .WithPayload(context.ApplicationMessage.Payload)
                .WithQualityOfServiceLevel(context.ApplicationMessage.QualityOfServiceLevel)
                .WithRetainFlag(context.ApplicationMessage.Retain)
                .Build()
            );
    }
}
