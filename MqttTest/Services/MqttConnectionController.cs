using MQTTnet.AspNetCore.Controllers;
using MQTTnet.Server;

namespace MqttTest.Services;

public class MqttConnectionController : IMqttConnectionController
{
    private readonly ILogger<MqttConnectionController> _logger;

    public MqttConnectionController(ILogger<MqttConnectionController> logger)
    {
        _logger = logger;
    }

    public Task ClientConnectedAsync(ClientConnectedEventArgs eventArgs)
    {
        _logger.LogInformation("Client {ClientId} connected", eventArgs.ClientId);
        return Task.CompletedTask;
    }

    public Task ClientDisconnectedAsync(ClientDisconnectedEventArgs eventArgs)
    {
        _logger.LogInformation("Client {ClientId} disconnected", eventArgs.ClientId);
        return Task.CompletedTask;
    }
}
