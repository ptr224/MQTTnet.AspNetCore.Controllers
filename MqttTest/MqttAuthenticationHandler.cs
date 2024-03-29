﻿using MQTTnet.AspNetCore.Controllers;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MqttTest;

public class MqttAuthenticationHandler : IMqttAuthenticationHandler
{
    public ValueTask AuthenticateAsync(ValidatingConnectionEventArgs context)
    {
        /*if (context.ClientId.Length < 10)
            context.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
        else if (context.Username != "mySecretUser")
            context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
        else if (context.Password != "mySecretPassword")
            context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;*/

        context.ReasonCode = MqttConnectReasonCode.Success;

        return ValueTask.CompletedTask;
    }
}
