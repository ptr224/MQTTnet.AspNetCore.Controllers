using MQTTnet.Server;

namespace MQTTnet.Extensions.Hosting;

public interface IMqttConnectionHandler : IMqttServerClientConnectedHandler, IMqttServerClientDisconnectedHandler
{ }
