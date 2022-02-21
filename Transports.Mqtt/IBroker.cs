using MQTTnet;
using System.Threading;
using System.Threading.Tasks;

namespace Transports.Mqtt
{
    public interface IBroker
    {
        Task Send(MqttApplicationMessage message, CancellationToken cancellationToken);
    }
}
