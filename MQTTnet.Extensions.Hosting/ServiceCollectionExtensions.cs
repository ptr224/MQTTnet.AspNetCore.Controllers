using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Extensions.Hosting.Internals;
using System;
using System.Linq;

namespace MQTTnet.Extensions.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMqttBroker(this IServiceCollection services, Action<MqttBrokerOptions> configureOptions)
    {
        var options = new MqttBrokerOptions();
        configureOptions(options);

        // Aggiungi controller se impostati

        if (options.ControllerAssemblies is not null)
        {
            // Trova tutti i controller e aggiungili alla DI

            var controllers = options.ControllerAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(MqttBaseController)) && !type.IsAbstract);

            foreach (var controller in controllers)
                services.AddScoped(controller);

            // Aggiungi le RouteTable

            services.AddSingleton(new SubscriptionRouteTable(controllers.Where(type => type.IsSubclassOf(typeof(MqttSubscriptionController)))));
            services.AddSingleton(new PublishRouteTable(controllers.Where(type => type.IsSubclassOf(typeof(MqttPublishController)))));
        }

        // Aggiungi gli handler se impostati

        if (options.AuthenticationHandler is not null)
            services.AddScoped(typeof(IMqttAuthenticationHandler), options.AuthenticationHandler);

        if (options.ConnectionHandler is not null)
            services.AddScoped(typeof(IMqttConnectionHandler), options.ConnectionHandler);

        // Aggiungi le impostazioni ed il Broker

        services.AddSingleton(options);
        services.AddSingleton<Broker>();
        services.AddSingleton<IBroker>(p => p.GetRequiredService<Broker>());
        services.AddHostedService(p => p.GetRequiredService<Broker>());
        return services;
    }
}
