using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet.AspNetCore.Controllers.Internals;
using System;
using System.Linq;

namespace MQTTnet.AspNetCore.Controllers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMqttControllers(this IServiceCollection services, Action<MqttControllersOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);

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
                services.TryAddScoped(controller);

            // Aggiungi la RouteTable

            services.TryAddSingleton(new RouteTable(controllers));
        }

        // Aggiungi gli handler se impostati

        if (options.AuthenticationController is not null)
            services.TryAddScoped(typeof(IMqttAuthenticationController), options.AuthenticationController);

        if (options.ConnectionController is not null)
            services.TryAddScoped(typeof(IMqttConnectionController), options.ConnectionController);

        // Aggiungi le impostazioni ed il Broker

        services.TryAddSingleton(options);
        services.TryAddSingleton<Broker>();
        services.TryAddSingleton<IBroker>(p => p.GetRequiredService<Broker>());

        return services;
    }

    public static IServiceCollection AddMqttContextAccessor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMqttContextAccessor, MqttContextAccessor>();
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
