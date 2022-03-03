using MQTTnet.Protocol;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.Hosting;

public interface IMqttAuthenticationHandler
{
    Task<MqttConnectReasonCode> AuthenticateAsync(AuthenticationContext context);
}
