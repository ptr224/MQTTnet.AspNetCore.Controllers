using MQTTnet.AspNetCore.Controllers;
using MQTTnet.Server;

namespace MqttTest;

public class MqttRetentionHandler : IMqttRetentionHandler
{
    private readonly ILogger<MqttRetentionHandler> _logger;

    public MqttRetentionHandler(ILogger<MqttRetentionHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask LoadingRetainedMessagesAsync(LoadingRetainedMessagesEventArgs context)
    {
        _logger.LogInformation("Loading retained messages");
        return ValueTask.CompletedTask;
    }

    public ValueTask RetainedMessageChangedAsync(RetainedMessageChangedEventArgs context)
    {
        _logger.LogInformation("Retained message changed");
        return ValueTask.CompletedTask;
    }

    public ValueTask RetainedMessagesClearedAsync(EventArgs context)
    {
        _logger.LogInformation("Retained messages cleared");
        return ValueTask.CompletedTask;
    }
}
