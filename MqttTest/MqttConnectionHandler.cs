using MQTTnet.AspNetCore.Controllers;
using MQTTnet.Server;

namespace MqttTest;

public class MqttConnectionHandler : IMqttConnectionHandler
{
    private readonly ILogger<MqttConnectionHandler> _logger;

    public MqttConnectionHandler(ILogger<MqttConnectionHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask ClientConnectedAsync(ClientConnectedEventArgs context)
    {
        _logger.LogInformation("Client {clientId} connected", context.ClientId);
        return ValueTask.CompletedTask;
    }

    public ValueTask ClientDisconnectedAsync(ClientDisconnectedEventArgs context)
    {
        _logger.LogInformation("Client {clientId} disconnected", context.ClientId);
        return ValueTask.CompletedTask;
    }
}
