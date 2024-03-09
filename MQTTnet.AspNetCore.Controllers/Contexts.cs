using MQTTnet.Server;

namespace MQTTnet.AspNetCore.Controllers;

public sealed class MqttContext
{
    public InterceptingPublishEventArgs? PublishEventArgs { get; set; }
    public InterceptingSubscriptionEventArgs? SubscriptionEventArgs { get; set; }
}

public class ActionContext
{
    public MqttContext MqttContext { get; }
    public object Controller { get; }
    public IServiceProvider Services { get; }
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    public ActionContext(MqttContext context, object controller, IServiceProvider serviceProvider, IReadOnlyDictionary<string, object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(parameters);

        MqttContext = context;
        Controller = controller;
        Services = serviceProvider;
        Parameters = parameters;
    }
}

public readonly record struct ModelBindingValue(Type Type, string Value);

public readonly record struct ModelBindingResult(bool IsSet, object? Model)
{
    public static ModelBindingResult Failed()
    {
        return new(false, null);
    }

    public static ModelBindingResult Success(object? model)
    {
        return new(true, model);
    }
}

public class ModelBindingContext
{
    public IServiceProvider Services { get; }
    public ModelBindingValue Value { get; }
    public ModelBindingResult Result { get; set; }

    public ModelBindingContext(IServiceProvider services, Type type, string value)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(value);

        Services = services;

        Value = new(type, value);
        Result = ModelBindingResult.Failed();
    }
}
