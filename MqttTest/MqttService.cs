using MQTTnet;
using MQTTnet.AspNetCore.Controllers;

namespace MqttTest;

public class MqttService(ILogger<MqttService> logger, IMqttContextAccessor mqttContextAccessor, IMqttBroker broker)
{
    private readonly string serverId = Guid.NewGuid().ToString("N");

    public async ValueTask Answer()
    {
        var context = mqttContextAccessor.PublishContext;

        if (context is null)
            logger.LogWarning("Not a publish event");
        else
            await broker.SendMessageAsync(serverId, new MqttApplicationMessageBuilder()
                .WithTopic($"{context.ApplicationMessage.Topic}/ans")
                .WithPayload(context.ApplicationMessage.PayloadSegment)
                .WithQualityOfServiceLevel(context.ApplicationMessage.QualityOfServiceLevel)
                .WithRetainFlag(context.ApplicationMessage.Retain)
                .Build()
            );
    }

    public Task ClearRetainedMessages()
    {
        return broker.ClearRetainedMessagesAsync();
    }
}
