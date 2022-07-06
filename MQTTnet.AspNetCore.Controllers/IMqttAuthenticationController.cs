using MQTTnet.Protocol;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers;

public interface IMqttAuthenticationController
{
    Task<MqttConnectReasonCode> AuthenticateAsync(AuthenticationContext context);
}
