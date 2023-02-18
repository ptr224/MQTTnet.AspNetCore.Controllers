using MQTTnet.AspNetCore.Controllers;
using MQTTnet.Server;

namespace MqttTest;

public class MqttConnectionController : IMqttConnectionController
{
    private readonly ILogger<MqttConnectionController> _logger;

    public MqttConnectionController(ILogger<MqttConnectionController> logger)
    {
        _logger = logger;
    }

    public Task ClientConnectedAsync(ClientConnectedEventArgs context)
    {
        _logger.LogInformation("Client {ClientId} connected", context.ClientId);
        return Task.CompletedTask;
    }

    public Task ClientDisconnectedAsync(ClientDisconnectedEventArgs context)
    {
        _logger.LogInformation("Client {ClientId} disconnected", context.ClientId);
        return Task.CompletedTask;
    }
}
