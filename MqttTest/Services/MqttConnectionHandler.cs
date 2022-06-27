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
