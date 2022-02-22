using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using MQTTnet.Extensions.Hosting.Internals;

namespace MQTTnet.Extensions.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMqttControllers(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Trova tutti i controller e aggiungili alla DI
        var controllers = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(type => type.GetCustomAttribute<MqttControllerAttribute>(false) is not null);

        foreach (var controller in controllers)
            services.AddScoped(controller);

        // Aggiungi le RouteTable
        services.AddSingleton(p => new SubscriptionRouteTable(controllers.Where(type => type.IsSubclassOf(typeof(MqttBaseSubscriptionController)))));
        services.AddSingleton(p => new PublishRouteTable(controllers.Where(type => type.IsSubclassOf(typeof(MqttBasePublishController)))));
        return services;
    }

    public static IServiceCollection AddMqttBroker(this IServiceCollection services, Action<MqttBrokerOptions> configureOptions)
    {
        var options = new MqttBrokerOptions();
        configureOptions(options);

        // Aggiungi le impostazioni ed il Broker
        services.AddSingleton(options);
        services.AddSingleton<Broker>();
        services.AddSingleton<IBroker>(p => p.GetRequiredService<Broker>());
        services.AddHostedService(p => p.GetRequiredService<Broker>());
        return services;
    }

    public static IServiceCollection AddMqttConnectionHandler<T>(this IServiceCollection services) where T : IBrokerConnectionHandler
    {
        services.AddSingleton(typeof(IBrokerConnectionHandler), typeof(T));
        return services;
    }
}
