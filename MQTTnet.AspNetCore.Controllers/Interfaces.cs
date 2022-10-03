using MQTTnet.Server;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers;

public interface IBroker
{
    Task Send(MqttApplicationMessage message);
}

public interface IMqttAuthenticationController
{
    Task AuthenticateAsync(ValidatingConnectionEventArgs context);
}

public interface IMqttConnectionController
{
    Task ClientConnectedAsync(ClientConnectedEventArgs context);
    Task ClientDisconnectedAsync(ClientDisconnectedEventArgs context);
}
