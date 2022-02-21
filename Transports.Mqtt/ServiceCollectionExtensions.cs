using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Transports.Mqtt
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMqttControllers(this IServiceCollection services, params Assembly[] assemblies)
        {
            // Trova tutti i controller e aggiungili alla DI
            var controllers = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(type => type.GetCustomAttribute<MqttControllerAttribute>(false) is not null && type.IsSubclassOf(typeof(MqttBaseController)));

            foreach (var controller in controllers)
                services.AddScoped(controller);

            // Aggiungi la RouteTable
            services.AddSingleton(p => RouteTable.Create(controllers));
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
        
        public static IServiceCollection AddMqttAuthorizationPolicy<T>(this IServiceCollection services) where T : IBrokerAuthorizationPolicy
        {
            services.AddSingleton(typeof(IBrokerAuthorizationPolicy), typeof(T));
            return services;
        }

        public static IServiceCollection AddMqttConnectionHandler<T>(this IServiceCollection services) where T : IBrokerConnectionHandler
        {
            services.AddSingleton(typeof(IBrokerConnectionHandler), typeof(T));
            return services;
        }
    }
}
