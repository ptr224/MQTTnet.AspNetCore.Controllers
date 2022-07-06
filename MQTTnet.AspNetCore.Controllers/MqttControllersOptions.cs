using System;
using System.Reflection;

namespace MQTTnet.AspNetCore.Controllers;

public sealed class MqttControllersOptions
{
    internal int MaxParallelRequests { get; private set; } = 4;
    internal Assembly[] ControllerAssemblies { get; private set; } = null;
    internal Type AuthenticationController { get; private set; } = null;
    internal Type ConnectionController { get; private set; } = null;

    public MqttControllersOptions WithMaxParallelRequests(int value)
    {
        MaxParallelRequests = value;
        return this;
    }

    public MqttControllersOptions WithControllers(params Assembly[] assemblies)
    {
        ControllerAssemblies = assemblies;
        return this;
    }

    public MqttControllersOptions WithAuthenticationController<T>() where T : IMqttAuthenticationController
    {
        AuthenticationController = typeof(T);
        return this;
    }

    public MqttControllersOptions WithConnectionController<T>() where T : IMqttConnectionController
    {
        ConnectionController = typeof(T);
        return this;
    }
}
