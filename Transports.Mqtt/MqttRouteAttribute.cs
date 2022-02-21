using System;

namespace Transports.Mqtt
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class MqttRouteAttribute : Attribute
    {
        public string Template { get; }

        public MqttRouteAttribute(string template)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
        }
    }
}
