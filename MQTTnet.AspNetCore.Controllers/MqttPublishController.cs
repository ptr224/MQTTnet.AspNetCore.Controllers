namespace MQTTnet.AspNetCore.Controllers;

public abstract class MqttPublishController : MqttBaseController
{
    public PublishContext Context { get; internal set; }

    private record struct MqttPublishResult(bool Accept, bool Publish) : IMqttPublishResult;

    protected IMqttPublishResult Stop()
        => new MqttPublishResult(true, false);

    protected IMqttPublishResult Forbid()
        => new MqttPublishResult(false, false);

    protected IMqttPublishResult Publish()
        => new MqttPublishResult(true, true);
}
