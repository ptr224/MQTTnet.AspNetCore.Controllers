﻿using MQTTnet.Server;
using System;

namespace MQTTnet.Extensions.Hosting;

public sealed class MqttBrokerOptions : MqttServerOptionsBuilder
{
    internal int MaxParallelRequests { get; private set; } = 4;
    internal bool DefaultSubscriptionAccept { get; private set; } = false;
    internal bool DefaultPublishAccept { get; private set; } = false;
    internal Type AuthenticationHandler { get; private set; } = null;
    internal Type ConnectionHandler { get; private set; } = null;

    public MqttBrokerOptions WithMaxParallelRequests(int value)
    {
        MaxParallelRequests = value;
        return this;
    }

    public MqttBrokerOptions WithDefaultSubscriptionAccept(bool value)
    {
        DefaultSubscriptionAccept = value;
        return this;
    }

    public MqttBrokerOptions WithDefaultPublishAccept(bool value)
    {
        DefaultPublishAccept = value;
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
