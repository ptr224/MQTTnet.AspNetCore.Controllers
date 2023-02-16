using System;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class MqttRouteAttribute : Attribute
{
    public string Template { get; }

    public MqttRouteAttribute(string template)
    {
        ArgumentNullException.ThrowIfNull(template);

        Template = template.Trim('/');
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class MqttPublishAttribute : MqttRouteAttribute
{
    public MqttPublishAttribute(string template) : base(template)
    { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class MqttSubscribeAttribute : MqttRouteAttribute
{
    public MqttSubscribeAttribute(string template) : base(template)
    { }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class MqttActionFilterAttribute : Attribute
{
    public delegate ValueTask ActionDelegate();

    public int Order { get; set; }

    public abstract ValueTask InvokeAsync(ControllerContext context, ActionDelegate next);
}
