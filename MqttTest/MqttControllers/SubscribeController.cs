using MQTTnet.Extensions.Hosting;

namespace MqttTest.MqttControllers;

public class SubscribeController : MqttSubscriptionController
{
    private readonly ILogger<SubscribeController> _logger;

    public SubscribeController(ILogger<SubscribeController> logger)
    {
        _logger = logger;
    }

    [MqttRoute("+")]
    public bool Root()
    {
        _logger.LogInformation("Accept subscription to: " + Context.TopicFilter.Topic);
        return true;
    }

    [MqttRoute("+/si/#")]
    public bool Accept()
    {
        _logger.LogInformation("Accept subscription to: " + Context.TopicFilter.Topic);
        return true;
    }

    [MqttRoute("+/no/#")]
    public bool Forbid()
    {
        _logger.LogInformation("Forbid subscription to: " + Context.TopicFilter.Topic);
        return false;
    }
}
