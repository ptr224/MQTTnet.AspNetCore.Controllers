using MQTTnet.AspNetCore.Controllers;
using MQTTnet.Protocol;

namespace MqttTest.Services;

public class MqttAuthenticationController : IMqttAuthenticationController
{
    public Task<MqttConnectReasonCode> AuthenticateAsync(AuthenticationContext context)
    {
        /*if (context.ClientId.Length < 10)
            return MqttConnectReasonCode.ClientIdentifierNotValid;
        else if (context.Username != "mySecretUser")
            return MqttConnectReasonCode.BadUserNameOrPassword;
        else if (context.Password != "mySecretPassword")
            return MqttConnectReasonCode.BadUserNameOrPassword;*/

        return Task.FromResult(MqttConnectReasonCode.Success);
    }
}
