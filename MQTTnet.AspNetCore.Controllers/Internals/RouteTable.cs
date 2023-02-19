using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal class RouteTable
{
    private static void ThrowIfInvalidReturnType(MethodInfo method)
    {
        var validTypes = new[]
        {
            typeof(void),
            typeof(Task),
            typeof(ValueTask)
        };

        if (!validTypes.Contains(method.ReturnType))
            throw new InvalidOperationException($"Invalid return type. All actions must return either {string.Join(" or ", validTypes.Select(t => t.Name))}.");
    }

    private static void ThrowIfOverlappingRoutes(IEnumerable<Route> routes)
    {
        if (routes.Count() != routes.Distinct(new RouteComparer()).Count())
            throw new InvalidOperationException("Cannot build route table. Two or more routes overlap.");
    }

    private static Route? Match(string[] topic, Route[] routes)
    {
        // Trova una Route compatibile con il topic passato
        return routes.Where(r => r.Match(topic)).FirstOrDefault();
    }

    private readonly Route[] _publishRoutes;
    private readonly Route[] _subscribeRoutes;

    public Type? AuthenticationHandler { get; }
    public Type? ConnectionHandler { get; }

    public RouteTable(MqttControllersOptions options)
    {
        // Seleziona dai controller specificati tutti i metodi con un template e ritornane la route

        var publishRoutes = new List<Route>();
        var subscribeRoutes = new List<Route>();

        var controllers = options.Assemblies
            .SelectMany(a => a.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(MqttControllerBase)) && !type.IsAbstract);

        foreach (var controller in controllers)
        {
            var routeAttribute = controller.GetCustomAttribute<MqttRouteAttribute>(true);
            var controllerFilters = controller.GetCustomAttributes<MqttActionFilterAttribute>(true);
            var controllerBinders = controller.GetCustomAttributes<MqttModelBinderAttribute>(true);

            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                // Crea coda filtri
                // Prima i filtri globali, poi del controller base, poi del controller derivato, poi dell'azione base, poi dell'azione derivata
                // Infine riordina secondo i parametri utente

                var actionFilters = method.GetCustomAttributes<MqttActionFilterAttribute>(true)
                    .Concat(controllerFilters)
                    .Concat(options.Filters)
                    .Reverse()
                    .OrderBy(f => f.Order)
                    .ToArray();

                // Crea coda binders
                // Prima i filtri dell'azione, poi quelli del controller, poi quelli globali

                var modelBinders = method.GetCustomAttributes<MqttModelBinderAttribute>(true)
                    .Concat(controllerBinders)
                    .Concat(options.Binders)
                    .ToArray();

                // Verifica che il metodo abbia un template

                var publishAttribute = method.GetCustomAttribute<MqttPublishAttribute>(true);
                if (publishAttribute is not null)
                {
                    ThrowIfInvalidReturnType(method);

                    // Prende i template del metodo e del controller e li unisce

                    string template = routeAttribute is null
                        ? publishAttribute.Template
                        : string.Join('/', routeAttribute.Template, publishAttribute.Template);

                    publishRoutes.Add(new(method, template, actionFilters, modelBinders));
                    continue;
                }

                var subscribeAttribute = method.GetCustomAttribute<MqttSubscribeAttribute>(true);
                if (subscribeAttribute is not null)
                {
                    ThrowIfInvalidReturnType(method);

                    // Prende i template del metodo e del controller e li unisce

                    string template = routeAttribute is null
                        ? subscribeAttribute.Template
                        : string.Join('/', routeAttribute.Template, subscribeAttribute.Template);

                    subscribeRoutes.Add(new(method, template, actionFilters, modelBinders));
                    continue;
                }
            }
        }

        // Verifica che non ci siano route uguali

        ThrowIfOverlappingRoutes(publishRoutes);
        ThrowIfOverlappingRoutes(subscribeRoutes);

        _publishRoutes = publishRoutes.ToArray();
        _subscribeRoutes = subscribeRoutes.ToArray();

        AuthenticationHandler = options.AuthenticationHandler;
        ConnectionHandler = options.ConnectionHandler;
    }

    public Route? MatchPublish(string[] topic)
    {
        return Match(topic, _publishRoutes);
    }

    public Route? MatchSubscribe(string[] topic)
    {
        return Match(topic, _subscribeRoutes);
    }
}
