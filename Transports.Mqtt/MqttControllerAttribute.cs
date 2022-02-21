using System;

namespace Transports.Mqtt
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class MqttControllerAttribute : Attribute
    { }
}
