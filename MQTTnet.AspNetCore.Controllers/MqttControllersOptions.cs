using System;
using System.Collections.Generic;
using System.Reflection;

namespace MQTTnet.AspNetCore.Controllers;

public class MqttControllersOptions
{
    public IList<Assembly> Assemblies { get; } = new List<Assembly>();
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
}
