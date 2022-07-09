using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.AspNetCore.Controllers.Internals;
using System;
using System.Linq;

namespace MQTTnet.AspNetCore.Controllers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMqttControllers(this IServiceCollection services, Action<MqttControllersOptions> configureOptions)
    {
        var options = new MqttControllersOptions();
        configureOptions(options);

        // Aggiungi controller se impostati

        if (options.ControllerAssemblies is not null)
        {
            // Trova tutti i controller e aggiungili alla DI

            var controllers = options.ControllerAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(MqttControllerBase)) && !type.IsAbstract);

            foreach (var controller in controllers)
                services.AddScoped(controller);

            // Aggiungi la RouteTable

            services.AddSingleton(new RouteTable(controllers));
        }

        // Aggiungi gli handler se impostati

        if (options.AuthenticationController is not null)
            services.AddScoped(typeof(IMqttAuthenticationController), options.AuthenticationController);

        if (options.ConnectionController is not null)
            services.AddScoped(typeof(IMqttConnectionController), options.ConnectionController);

        // Aggiungi le impostazioni ed il Broker

        services.AddSingleton(options);
        services.AddSingleton<Broker>();
        services.AddSingleton<IBroker>(p => p.GetRequiredService<Broker>());

        return services;
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
