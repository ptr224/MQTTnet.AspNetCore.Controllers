using MQTTnet.Server;

namespace Transports.Mqtt
{
    public interface IBrokerAuthorizationPolicy : IMqttServerConnectionValidator, IMqttServerSubscriptionInterceptor, IMqttServerApplicationMessageInterceptor
    {
    }
}
