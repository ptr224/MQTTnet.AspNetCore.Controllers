using System;
using System.Reflection;

namespace MQTTnet.AspNetCore.Controllers;

public sealed class MqttControllersOptions
{
    internal Assembly[]? ControllerAssemblies { get; private set; } = null;
    internal Type? AuthenticationController { get; private set; } = null;
    internal Type? ConnectionController { get; private set; } = null;

    public MqttControllersOptions WithControllers(params Assembly[] assemblies)
    {
        ControllerAssemblies = assemblies;
        return this;
    }

    public MqttControllersOptions WithAuthenticationController(Type type)
    {
        if (!type.IsAssignableTo(typeof(IMqttAuthenticationController)))
            throw new ArgumentException($"Type must implement {nameof(IMqttAuthenticationController)}", nameof(type));

        AuthenticationController = type;
        return this;
    }

    public MqttControllersOptions WithAuthenticationController<T>() where T : IMqttAuthenticationController
    {
        AuthenticationController = typeof(T);
        return this;
    }

    public MqttControllersOptions WithConnectionController(Type type)
    {
        if (!type.IsAssignableTo(typeof(IMqttConnectionController)))
            throw new ArgumentException($"Type must implement {nameof(IMqttConnectionController)}", nameof(type));

        ConnectionController = type;
        return this;
    }

    public MqttControllersOptions WithConnectionController<T>() where T : IMqttConnectionController
    {
        ConnectionController = typeof(T);
        return this;
    }
}
