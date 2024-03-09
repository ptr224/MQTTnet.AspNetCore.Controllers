using MQTTnet.AspNetCore.Controllers;
using MQTTnet.Server;

namespace MqttTest;

public class MqttConnectionHandler(ILogger<MqttConnectionHandler> logger) : IMqttConnectionHandler
{
    public ValueTask ClientConnectedAsync(ClientConnectedEventArgs context)
    {
        logger.LogInformation("Client {clientId} connected", context.ClientId);
        return ValueTask.CompletedTask;
    }

    public ValueTask ClientDisconnectedAsync(ClientDisconnectedEventArgs context)
    {
        logger.LogInformation("Client {clientId} disconnected", context.ClientId);
        return ValueTask.CompletedTask;
    }
}
