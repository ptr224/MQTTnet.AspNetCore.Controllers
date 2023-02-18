using MQTTnet.Server;
using System;
using System.Collections.Generic;

namespace MQTTnet.AspNetCore.Controllers;

public sealed class MqttContext
{
    public InterceptingPublishEventArgs? PublishEventArgs { get; set; }
    public InterceptingSubscriptionEventArgs? SubscriptionEventArgs { get; set; }
}

public class ActionContext
{
    public MqttControllerBase Controller { get; }
    public IServiceProvider Services { get; }
    public IReadOnlyDictionary<string, string> Parameters { get; }

    public MqttContext MqttContext => Controller.MqttContext;

    public ActionContext(MqttControllerBase controller, IServiceProvider serviceProvider, IReadOnlyDictionary<string, string> parameters)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(parameters);

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
