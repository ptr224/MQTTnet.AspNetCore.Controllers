using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers;

public interface IBroker
{
    Task Send(MqttApplicationMessage message);
}

public interface IMqttAuthenticationController
{
    Task<MqttConnectReasonCode> AuthenticateAsync(AuthenticationContext context);
}

public interface IMqttConnectionController
{
    Task ClientConnectedAsync(ClientConnectedEventArgs eventArgs);
    Task ClientDisconnectedAsync(ClientDisconnectedEventArgs eventArgs);
}

public interface IMqttPublishResult
{
    bool CloseConnection { get; }
    bool Process { get; }
    bool Publish { get; }
}

public interface IMqttSubscribeResult
{
    bool CloseConnection { get; }
    bool Process { get; }
}
