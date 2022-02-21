using MQTTnet.Server;

namespace Transports.Mqtt
{
    public interface IBrokerConnectionHandler : IMqttServerClientConnectedHandler, IMqttServerClientDisconnectedHandler
    {
    }
}
