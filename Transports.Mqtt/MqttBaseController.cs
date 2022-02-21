using MQTTnet;

namespace Transports.Mqtt
{
    public abstract class MqttBaseController
    {
        public MqttApplicationMessageReceivedEventArgs Context { get; set; }
    }
}
