namespace MQTTnet.Extensions.Hosting;

public abstract class MqttPublishController : MqttBaseController
{
    public PublishContext Context { get; internal set; }

    private class MqttPublishResult : IMqttPublishResult
    {
        public bool Accept { get; init; }
        public bool Publish { get; init; }
    }

    protected IMqttPublishResult Stop()
    {
        return new MqttPublishResult
        {
            Accept = true,
            Publish = false
        };
    }

    protected IMqttPublishResult Forbid()
    {
        return new MqttPublishResult
        {
            Accept = false,
            Publish = false
        };
    }

    protected IMqttPublishResult Publish()
    {
        return new MqttPublishResult
        {
            Accept = true,
            Publish = true
        };
    }
}
