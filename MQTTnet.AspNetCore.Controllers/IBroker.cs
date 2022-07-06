using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers;

public interface IBroker
{
    Task Send(MqttApplicationMessage message);
}
