using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet.AspNetCore.Controllers.Internals;
using System;

namespace MQTTnet.AspNetCore.Controllers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMqttControllers(this IServiceCollection services, Action<MqttControllersOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Configura servizio

        var options = new MqttControllersOptions();
        configure(options);

        // Aggiungi RouteTable e Broker

        services.TryAddSingleton(new RouteTable(options));
        services.TryAddSingleton<MqttBroker>();
        services.TryAddSingleton<IMqttBroker>(p => p.GetRequiredService<MqttBroker>());

        return services;
    }

    public static IServiceCollection AddMqttControllers(this IServiceCollection services)
        => services.AddMqttControllers(options => options.AddAssembliesFromCurrentDomain());

    public static IServiceCollection AddMqttContextAccessor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMqttContextAccessor, MqttContextAccessor>();
        return services;
    }

    public static IApplicationBuilder UseMqttControllers(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMqttServer(server =>
        {
            var broker = app.ApplicationServices.GetRequiredService<MqttBroker>();
            broker.UseMqttServer(server);
        });

        return app;
    }
}
