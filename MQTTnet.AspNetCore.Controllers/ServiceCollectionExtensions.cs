using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet.AspNetCore.Controllers.Internals;
using System;
using System.Linq;
using System.Reflection;

namespace MQTTnet.AspNetCore.Controllers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMqttControllers(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Trova e aggiungi tutti i controller

        var controllers = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(MqttControllerBase)) && !type.IsAbstract)
            .ToList();

        foreach (var controller in controllers)
            services.TryAddScoped(controller);

        // Aggiungi RouteTable e Broker

        services.TryAddSingleton(new RouteTable(controllers));
        services.TryAddSingleton<Broker>();
        services.TryAddSingleton<IBroker>(p => p.GetRequiredService<Broker>());

        return services;
    }

    public static IServiceCollection AddMqttContextAccessor(this IServiceCollection services)
    {
        services.TryAddSingleton<IMqttContextAccessor, MqttContextAccessor>();
        return services;
    }

    public static IServiceCollection AddMqttAuthenticationController(this IServiceCollection services, Type type)
    {
        if (!type.IsAssignableTo(typeof(IMqttAuthenticationController)))
            throw new ArgumentException($"Type must implement {nameof(IMqttAuthenticationController)}", nameof(type));

        services.TryAddScoped(typeof(IMqttAuthenticationController), type);
        return services;
    }

    public static IServiceCollection AddMqttAuthenticationController<T>(this IServiceCollection services) where T : IMqttAuthenticationController
    {
        return services.AddMqttAuthenticationController(typeof(T));
    }

    public static IServiceCollection AddMqttConnectionController(this IServiceCollection services, Type type)
    {
        if (!type.IsAssignableTo(typeof(IMqttConnectionController)))
            throw new ArgumentException($"Type must implement {nameof(IMqttConnectionController)}", nameof(type));

        services.TryAddScoped(typeof(IMqttConnectionController), type);
        return services;
    }

    public static IServiceCollection AddMqttConnectionController<T>(this IServiceCollection services) where T : IMqttConnectionController
    {
        return services.AddMqttConnectionController(typeof(T));
    }

    public static IApplicationBuilder UseMqttControllers(this IApplicationBuilder app)
    {
        app.UseMqttServer(server =>
        {
            var broker = app.ApplicationServices.GetRequiredService<Broker>();
            broker.UseMqttServer(server);
        });

        return app;
    }
}
