﻿using System;

namespace MQTTnet.AspNetCore.Controllers;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class MqttRouteAttribute : Attribute
{
    public string Template { get; }

    public MqttRouteAttribute(string template)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MqttPublishAttribute : MqttRouteAttribute
{
    public MqttPublishAttribute(string template) : base(template)
    { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MqttSubscribeAttribute : MqttRouteAttribute
{
    public MqttSubscribeAttribute(string template) : base(template)
    { }
}