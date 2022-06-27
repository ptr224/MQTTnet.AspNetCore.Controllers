using MQTTnet.Server;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.Hosting;

public interface IMqttConnectionHandler
{
    Task ClientConnectedAsync(ClientConnectedEventArgs eventArgs);
    Task ClientDisconnectedAsync(ClientDisconnectedEventArgs eventArgs);
}
