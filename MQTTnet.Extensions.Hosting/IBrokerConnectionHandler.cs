using MQTTnet.Server;

namespace MQTTnet.Extensions.Hosting;

public interface IBrokerConnectionHandler : IMqttServerConnectionValidator, IMqttServerClientConnectedHandler, IMqttServerClientDisconnectedHandler
{ }
