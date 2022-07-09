namespace MQTTnet.AspNetCore.Controllers;

public abstract class MqttControllerBase
{
    private record struct MqttPublishResult(bool CloseConnection, bool Process, bool Publish) : IMqttPublishResult;
    private record struct MqttSubscribeResult(bool CloseConnection, bool Process) : IMqttSubscribeResult;

    public PublishContext PublishContext { get; internal set; }
    public SubscriptionContext SubscriptionContext { get; internal set; }

    protected IMqttPublishResult KickOutPublish()
        => new MqttPublishResult(true, false, false);

    protected IMqttPublishResult ForbidPublish()
        => new MqttPublishResult(false, false, false);

    protected IMqttPublishResult StopPublish()
        => new MqttPublishResult(false, true, false);

    protected IMqttPublishResult Publish()
        => new MqttPublishResult(false, true, true);

    protected IMqttSubscribeResult KickOutSubscribe()
        => new MqttSubscribeResult(true, false);

    protected IMqttSubscribeResult ForbidSubscribe()
        => new MqttSubscribeResult(false, false);

    protected IMqttSubscribeResult Subscribe()
        => new MqttSubscribeResult(false, true);
}
 