using MQTTnet.Extensions.Hosting;
using MQTTnet.Server;

namespace MqttTest.Services;

public class MqttConnectionHandler : IMqttConnectionHandler
{
    private readonly ILogger<MqttConnectionHandler> _logger;

    public MqttConnectionHandler(ILogger<MqttConnectionHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs eventArgs)
    {
        _logger.LogInformation("Client " + eventArgs.ClientId + " connected");
        return Task.CompletedTask;
    }

    public Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
    {
        _logger.LogInformation("Client " + eventArgs.ClientId + " disconnected");
        return Task.CompletedTask;
    }
}
