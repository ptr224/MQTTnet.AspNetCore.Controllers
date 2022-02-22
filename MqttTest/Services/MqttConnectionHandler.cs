using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Extensions.Hosting;

namespace MqttTest.Services;

public class MqttConnectionHandler : IBrokerConnectionHandler
{
    private readonly ILogger<MqttConnectionHandler> _logger;

    public MqttConnectionHandler(ILogger<MqttConnectionHandler> logger)
    {
        _logger = logger;
    }

    public Task ValidateConnectionAsync(MqttConnectionValidatorContext context)
    {
        /*if (context.ClientId.Length < 10)
            context.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
        else if (context.Username != "mySecretUser")
            context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
        else if (context.Password != "mySecretPassword")
            context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;*/

        context.ReasonCode = MqttConnectReasonCode.Success;
        return Task.CompletedTask;
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
