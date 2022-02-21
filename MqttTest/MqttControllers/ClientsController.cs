using System.Text;
using System.Text.Json;
using Transports.Mqtt;

namespace MqttTest.MqttControllers;

[MqttController]
public class ClientsController : MqttBaseController
{
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(ILogger<ClientsController> logger)
    {
        _logger = logger;
    }

    [MqttRoute("{serial}/status")]
    public async Task SetStatus(string serial)
    {
        _logger.LogInformation("Message from " + serial + " : " + Encoding.UTF8.GetString(Context.ApplicationMessage.Payload));
    }
}
