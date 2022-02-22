using System.Text;
using System.Text.Json;
using MQTTnet.Extensions.Hosting;

namespace MqttTest.MqttControllers;

[MqttController]
public class PublishController : MqttBasePublishController
{
    private readonly ILogger<PublishController> _logger;

    public PublishController(ILogger<PublishController> logger)
    {
        _logger = logger;
    }

    [MqttRoute("{serial}/stop")]
    public async Task<IMqttPublishResult> ManageStop(string serial)
    {
        _logger.LogInformation("Message from " + serial + " : " + Encoding.UTF8.GetString(Context.ApplicationMessage.Payload));
        return Stop();
    }

    [MqttRoute("{serial}/forbid")]
    public async Task<IMqttPublishResult> ManageForbid(string serial)
    {
        _logger.LogInformation("Message from " + serial + " : " + Encoding.UTF8.GetString(Context.ApplicationMessage.Payload));
        return Forbid();
    }

    [MqttRoute("{serial}/publish")]
    public async Task<IMqttPublishResult> ManagePublish(string serial)
    {
        _logger.LogInformation("Message from " + serial + " : " + Encoding.UTF8.GetString(Context.ApplicationMessage.Payload));
        return Publish();
    }
}
