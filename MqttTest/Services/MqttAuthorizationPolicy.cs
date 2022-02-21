using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Threading.Tasks;
using Transports.Mqtt;

namespace MqttTest.Services;

public class MqttAuthorizationPolicy : IBrokerAuthorizationPolicy
{
    private readonly ILogger<MqttAuthorizationPolicy> _logger;

    public MqttAuthorizationPolicy(ILogger<MqttAuthorizationPolicy> logger)
    {
        _logger = logger;
    }

    public Task ValidateConnectionAsync(MqttConnectionValidatorContext context)
    {
        /*if (context.ClientId.Length < 10)
            context.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
        else if (context.Username != "mySecretUser")
            context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
        else if (context.Password != "mySecretPassword")
            context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;*/

        context.ReasonCode = MqttConnectReasonCode.Success;
        return Task.CompletedTask;
    }

    public Task InterceptSubscriptionAsync(MqttSubscriptionInterceptorContext context)
    {
        // Allow clients to subscribe only to their own subtopics (always allow server)
        //context.AcceptSubscription = context.ClientId is null || context.TopicFilter.Topic.StartsWith($"{context.ClientId}/");
        _logger.LogInformation(context.ClientId + " subscribed to " + context.TopicFilter.Topic);
        return Task.CompletedTask;
    }

    public Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
    {
        // Allow clients to send only to their own subtopics (always allow server)
        //context.AcceptPublish = context.ClientId is null || context.ApplicationMessage.Topic.StartsWith($"{context.ClientId}/");
        _logger.LogInformation(context.ClientId + " published to " + context.ApplicationMessage.Topic);
        return Task.CompletedTask;
    }
}
