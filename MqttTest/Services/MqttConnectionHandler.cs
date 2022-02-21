using MQTTnet.Server;
using Transports.Mqtt;

namespace MqttTest.Services;

public class MqttConnectionHandler : IBrokerConnectionHandler
{
    private readonly ILogger<MqttConnectionHandler> _logger;

    public MqttConnectionHandler(ILogger<MqttConnectionHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs eventArgs)
    {
        _logger.LogInformation("Client " + eventArgs.ClientId + " connected");
    }

    public async Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
    {
        _logger.LogInformation("Client " + eventArgs.ClientId + " disconnected");
    }
}
