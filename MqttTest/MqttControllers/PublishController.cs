using MQTTnet;
using MQTTnet.AspNetCore.Controllers;

namespace MqttTest.MqttControllers;

public class PublishController : MqttPublishController
{
    private readonly ILogger<PublishController> _logger;

    public PublishController(ILogger<PublishController> logger)
    {
        _logger = logger;
    }

    [MqttRoute("+/+/#")]
    public IMqttPublishResult Answer()
    {
        _logger.LogInformation($"Message from \"{Context.ClientId}\": {Context.ApplicationMessage.ConvertPayloadToString()}");
        return Publish();
    }

    [MqttRoute("{serial}/stop")]
    public IMqttPublishResult ManageStop(string serial)
    {
        _logger.LogInformation("Message from " + serial + " : " + Context.ApplicationMessage.ConvertPayloadToString());
        return Stop();
    }

    [MqttRoute("{serial}/forbid")]
    public IMqttPublishResult ManageForbid(string serial)
    {
        _logger.LogInformation("Message from " + serial + " : " + Context.ApplicationMessage.ConvertPayloadToString());
        return Forbid();
    }

    [MqttRoute("{serial}/publish")]
    public IMqttPublishResult ManagePublish(string serial)
    {
        _logger.LogInformation("Message from " + serial + " : " + Context.ApplicationMessage.ConvertPayloadToString());
        return Publish();
    }
}
