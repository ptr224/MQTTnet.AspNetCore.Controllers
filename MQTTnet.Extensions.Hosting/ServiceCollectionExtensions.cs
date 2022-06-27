using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Extensions.Hosting.Internals;
using MQTTnet.Server;
using System;
using System.Linq;

namespace MQTTnet.Extensions.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMqttBroker(this IServiceCollection services, Action<MqttServerOptionsBuilder, MqttHandlingOptionsBuilder> configureOptions)
    {
        var serverOptions = new MqttServerOptionsBuilder();
        var handlingOptions = new MqttHandlingOptionsBuilder();
        configureOptions(serverOptions, handlingOptions);

        // Aggiungi controller se impostati

        if (handlingOptions.ControllerAssemblies is not null)
        {
            // Trova tutti i controller e aggiungili alla DI

            var controllers = handlingOptions.ControllerAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(MqttBaseController)) && !type.IsAbstract);

            foreach (var controller in controllers)
                services.AddScoped(controller);

            // Aggiungi le RouteTable

            services.AddSingleton(new SubscriptionRouteTable(controllers.Where(type => type.IsSubclassOf(typeof(MqttSubscriptionController)))));
            services.AddSingleton(new PublishRouteTable(controllers.Where(type => type.IsSubclassOf(typeof(MqttPublishController)))));
        }

        // Aggiungi gli handler se impostati

        if (handlingOptions.AuthenticationHandler is not null)
            services.AddScoped(typeof(IMqttAuthenticationHandler), handlingOptions.AuthenticationHandler);

        if (handlingOptions.ConnectionHandler is not null)
            services.AddScoped(typeof(IMqttConnectionHandler), handlingOptions.ConnectionHandler);

        // Aggiungi le impostazioni ed il Broker

        services.AddSingleton(serverOptions);
        services.AddSingleton(handlingOptions);
        services.AddSingleton<Broker>();
        services.AddSingleton<IBroker>(p => p.GetRequiredService<Broker>());
        services.AddHostedService(p => p.GetRequiredService<Broker>());
        return services;
    }
}
