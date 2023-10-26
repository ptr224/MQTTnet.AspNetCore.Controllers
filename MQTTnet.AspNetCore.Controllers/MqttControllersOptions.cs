using System;
using System.Collections.Generic;
using System.Reflection;

namespace MQTTnet.AspNetCore.Controllers;

public class MqttControllersOptions
{
    internal IList<Assembly> Assemblies { get; } = new List<Assembly>();
    internal Type? AuthenticationHandler { get; set; }
    internal Type? ConnectionHandler { get; set; }
    internal Type? RetentionHandler { get; set; }

    public IList<IMqttActionFilter> Filters { get; } = new List<IMqttActionFilter>();
    public IList<IMqttModelBinder> Binders { get; } = new List<IMqttModelBinder>();

    public MqttControllersOptions AddAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
            Assemblies.Add(assembly);

        return this;
    }

    public MqttControllersOptions AddAssembliesFromCurrentDomain()
        => AddAssemblies(AppDomain.CurrentDomain.GetAssemblies());

    public MqttControllersOptions AddAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        Assemblies.Add(assembly);
        return this;
    }

    public MqttControllersOptions AddAssemblyContainingType(Type type)
        => AddAssembly(type.Assembly);

    public MqttControllersOptions AddAssemblyContainingType<T>()
        => AddAssemblyContainingType(typeof(T));

    public MqttControllersOptions WithAuthenticationHandler(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsAssignableTo(typeof(IMqttAuthenticationHandler)))
            AuthenticationHandler = type;
        else
            throw new ArgumentException($"Type must implement {nameof(IMqttAuthenticationHandler)}", nameof(type));

        return this;
    }

    public MqttControllersOptions WithAuthenticationHandler<T>() where T : IMqttAuthenticationHandler
    {
        return WithAuthenticationHandler(typeof(T));
    }

    public MqttControllersOptions WithConnectionHandler(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsAssignableTo(typeof(IMqttConnectionHandler)))
            ConnectionHandler = type;
        else
            throw new ArgumentException($"Type must implement {nameof(IMqttConnectionHandler)}", nameof(type));

        return this;
    }
    
    public MqttControllersOptions WithConnectionHandler<T>() where T : IMqttConnectionHandler
    {
        return WithConnectionHandler(typeof(T));
    }

    public MqttControllersOptions WithRetentionHandler(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsAssignableTo(typeof(IMqttRetentionHandler)))
            RetentionHandler = type;
        else
            throw new ArgumentException($"Type must implement {nameof(IMqttRetentionHandler)}", nameof(type));

        return this;
    }

    public MqttControllersOptions WithRetentionHandler<T>() where T : IMqttRetentionHandler
    {
        return WithRetentionHandler(typeof(T));
    }
}
