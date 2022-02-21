using MQTTnet.Server;

namespace Transports.Mqtt
{
    public sealed class MqttBrokerOptions : MqttServerOptionsBuilder
    {
        internal int MaxParallelRequests { get; private set; } = 4;

        public MqttBrokerOptions WithMaxParallelRequests(int value)
        {
            MaxParallelRequests = value;
            return this;
        }
    }
}
