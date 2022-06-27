using System;
using System.Reflection;

namespace MQTTnet.Extensions.Hosting;

public sealed class MqttHandlingOptionsBuilder
{
    internal int MaxParallelRequests { get; private set; } = 4;
    internal Assembly[] ControllerAssemblies { get; private set; } = null;
    internal Type AuthenticationHandler { get; private set; } = null;
    internal Type ConnectionHandler { get; private set; } = null;

    public MqttHandlingOptionsBuilder WithMaxParallelRequests(int value)
    {
        MaxParallelRequests = value;
        return this;
    }

    public MqttHandlingOptionsBuilder WithControllers(params Assembly[] assemblies)
    {
        ControllerAssemblies = assemblies;
        return this;
    }

    public MqttHandlingOptionsBuilder WithAuthenticationHandler<T>() where T : IMqttAuthenticationHandler
    {
        AuthenticationHandler = typeof(T);
        return this;
    }

    public MqttHandlingOptionsBuilder WithConnectionHandler<T>() where T : IMqttConnectionHandler
    {
        ConnectionHandler = typeof(T);
        return this;
    }
}
