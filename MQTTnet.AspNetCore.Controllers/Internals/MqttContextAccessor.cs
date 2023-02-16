using MQTTnet.Server;
using System.Threading;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal class MqttContextAccessor : IMqttContextAccessor
{
    private static readonly AsyncLocal<MqttContextHolder> mqttContextCurrent = new();

    public InterceptingPublishEventArgs? PublishContext
    {
        get
        {
            return mqttContextCurrent.Value?.PublishContext;
        }
        set
        {
            var holder = mqttContextCurrent.Value;
            if (holder != null)
            {
                // Clear current HttpContext trapped in the AsyncLocals, as its done.
                holder.PublishContext = null;
            }

            if (value != null)
            {
                // Use an object indirection to hold the HttpContext in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                mqttContextCurrent.Value = new() { PublishContext = value };
            }
        }
    }

    public InterceptingSubscriptionEventArgs? SubscriptionContext
    {
        get
        {
            return mqttContextCurrent.Value?.SubscriptionContext;
        }
        set
        {
            var holder = mqttContextCurrent.Value;
            if (holder != null)
            {
                // Clear current HttpContext trapped in the AsyncLocals, as its done.
                holder.SubscriptionContext = null;
            }

            if (value != null)
            {
                // Use an object indirection to hold the HttpContext in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                mqttContextCurrent.Value = new() { SubscriptionContext = value };
            }
        }
    }

    private sealed class MqttContextHolder
    {
        public InterceptingPublishEventArgs? PublishContext;
        public InterceptingSubscriptionEventArgs? SubscriptionContext;
    }
}
