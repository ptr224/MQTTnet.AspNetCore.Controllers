using MQTTnet.AspNetCore.Controllers;
using MQTTnet.Server;

namespace MqttTest;

public class MqttRetentionHandler(ILogger<MqttRetentionHandler> logger) : IMqttRetentionHandler
{
    public ValueTask LoadingRetainedMessagesAsync(LoadingRetainedMessagesEventArgs context)
    {
        logger.LogInformation("Loading retained messages");
        return ValueTask.CompletedTask;
    }

    public ValueTask RetainedMessageChangedAsync(RetainedMessageChangedEventArgs context)
    {
        logger.LogInformation("Retained message changed");
        return ValueTask.CompletedTask;
    }

    public ValueTask RetainedMessagesClearedAsync(EventArgs context)
    {
        logger.LogInformation("Retained messages cleared");
        return ValueTask.CompletedTask;
    }
}
