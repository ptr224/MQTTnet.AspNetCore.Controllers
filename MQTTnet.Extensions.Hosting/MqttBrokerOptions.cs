using MQTTnet.Server;
using System;
using System.Reflection;

namespace MQTTnet.Extensions.Hosting;

public sealed class MqttBrokerOptions : MqttServerOptionsBuilder
{
    internal int MaxParallelRequests { get; private set; } = 4;
    internal Assembly[] ControllerAssemblies { get; private set; } = null;
    internal Type AuthenticationHandler { get; private set; } = null;
    internal Type ConnectionHandler { get; private set; } = null;

    public MqttBrokerOptions WithMaxParallelRequests(int value)
    {
        MaxParallelRequests = value;
        return this;
    }

    public MqttBrokerOptions WithControllers(params Assembly[] assemblies)
    {
        ControllerAssemblies = assemblies;
        return this;
    }

    public MqttBrokerOptions WithAuthenticationHandler<T>() where T : IMqttAuthenticationHandler
    {
        AuthenticationHandler = typeof(T);
        return this;
    }

    public MqttBrokerOptions WithConnectionHandler<T>() where T : IMqttConnectionHandler
    {
        ConnectionHandler = typeof(T);
        return this;
    }
}
