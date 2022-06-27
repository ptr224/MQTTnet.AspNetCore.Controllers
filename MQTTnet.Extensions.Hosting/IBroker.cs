using System.Threading.Tasks;

namespace MQTTnet.Extensions.Hosting;

public interface IBroker
{
    Task Send(MqttApplicationMessage message);
}
