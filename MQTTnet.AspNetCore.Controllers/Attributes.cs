using Microsoft.Extensions.DependencyInjection;

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
public sealed class MqttPublishAttribute(string template) : MqttRouteAttribute(template);

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class MqttSubscribeAttribute(string template) : MqttRouteAttribute(template);

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class MqttActionFilterAttribute : Attribute, IMqttActionFilter
{
    public abstract ValueTask OnActionAsync(ActionContext context, MqttActionFilterDelegate next);
}

public class MqttActionFilterServiceAttribute : MqttActionFilterAttribute
{
    private readonly Type _service;

    public MqttActionFilterServiceAttribute(Type service)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (service.IsAssignableTo(typeof(IMqttActionFilter)))
            _service = service;
        else
            throw new ArgumentException($"Type must implement {nameof(IMqttActionFilter)}", nameof(service));
    }

    public override ValueTask OnActionAsync(ActionContext context, MqttActionFilterDelegate next)
    {
        var service = (IMqttActionFilter)context.Services.GetRequiredService(_service);
        return service.OnActionAsync(context, next);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
public abstract class MqttModelBinderAttribute : Attribute, IMqttModelBinder
{
    public abstract ValueTask BindModelAsync(ModelBindingContext context);
}

public class MqttModelBinderServiceAttribute : MqttModelBinderAttribute
{
    private readonly Type _service;

    public MqttModelBinderServiceAttribute(Type service)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (service.IsAssignableTo(typeof(IMqttModelBinder)))
            _service = service;
        else
            throw new ArgumentException($"Type must implement {nameof(IMqttModelBinder)}", nameof(service));
    }

    public override ValueTask BindModelAsync(ModelBindingContext context)
    {
        var service = (IMqttModelBinder)context.Services.GetRequiredService(_service);
        return service.BindModelAsync(context);
    }
}
