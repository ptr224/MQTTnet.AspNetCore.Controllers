using MQTTnet.Server;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers;

public interface IMqttConnectionController
{
    Task ClientConnectedAsync(ClientConnectedEventArgs eventArgs);
    Task ClientDisconnectedAsync(ClientDisconnectedEventArgs eventArgs);
}
