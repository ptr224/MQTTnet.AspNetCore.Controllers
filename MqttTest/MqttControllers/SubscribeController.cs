using MQTTnet.Extensions.Hosting;

namespace MqttTest.MqttControllers
{
    [MqttController]
    public class SubscribeController : MqttBaseSubscriptionController
    {
        private readonly ILogger<SubscribeController> _logger;

        public SubscribeController(ILogger<SubscribeController> logger)
        {
            _logger = logger;
        }

        [MqttRoute("{serial}/#")]
        public async Task<bool> Accept(string serial)
        {
            _logger.LogInformation("Accept subscription to " + serial + " : " + Context.TopicFilter.Topic);
            return true;
        }

        [MqttRoute("#")]
        public async Task<bool> Forbid()
        {
            _logger.LogInformation("Forbid subscription to: " + Context.TopicFilter.Topic);
            return false;
        }
    }
}
