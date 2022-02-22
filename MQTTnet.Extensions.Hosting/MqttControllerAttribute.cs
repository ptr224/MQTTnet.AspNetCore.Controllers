using System;

namespace MQTTnet.Extensions.Hosting;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MqttControllerAttribute : Attribute
{ }
