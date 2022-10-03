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
            throw new InvalidOperationException($"Invalid action. All routed publish methods must return either {string.Join(" or ", validTypes.Select(t => t.Name))}.");
    }

    private static void ThrowIfOverlappingRoutes(IEnumerable<Route> routes)
    {
        if (routes.Count() != routes.Distinct(new RouteComparer()).Count())
            throw new InvalidOperationException("Cannot build route table. Two or more routes overlap.");
    }

    private static Route Match(string[] topic, Route[] routes)
    {
        // Trova una Route compatibile con il topic passato
        return routes.Where(r => r.Match(topic)).FirstOrDefault();
    }

    protected readonly Route[] _publishRoutes;
    protected readonly Route[] _subscribeRoutes;

    public RouteTable(IEnumerable<Type> controllers)
    {
        // Seleziona dai controller specificati tutti i metodi con un template e ritornane la route

        var publishRoutes = new List<Route>();
        var subscribeRoutes = new List<Route>();

        foreach (var controller in controllers)
        {
            var routeAttribute = controller.GetCustomAttribute<MqttRouteAttribute>(false);

            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
            {
                // Verifica che il metodo abbia un template

                var publishAttribute = method.GetCustomAttribute<MqttPublishAttribute>(false);
                if (publishAttribute is not null)
                {
                    ThrowIfInvalidReturnType(method);

                    // Prende i template del metodo e del controller e li unisce

                    string template = routeAttribute is null
                        ? publishAttribute.Template
                        : string.Join('/', routeAttribute.Template, publishAttribute.Template);

                    publishRoutes.Add(new(method, template));
                    continue;
                }

                var subscribeAttribute = method.GetCustomAttribute<MqttSubscribeAttribute>(false);
                if (subscribeAttribute is not null)
                {
                    ThrowIfInvalidReturnType(method);

                    // Prende i template del metodo e del controller e li unisce

                    string template = routeAttribute is null
                        ? subscribeAttribute.Template
                        : string.Join('/', routeAttribute.Template, subscribeAttribute.Template);

                    subscribeRoutes.Add(new(method, template));
                    continue;
                }
            }
        }

        // Verifica che non ci siano route uguali

        ThrowIfOverlappingRoutes(publishRoutes);
        ThrowIfOverlappingRoutes(subscribeRoutes);

        _publishRoutes = publishRoutes.ToArray();
        _subscribeRoutes = subscribeRoutes.ToArray();
    }

    public Route MatchPublish(string[] topic)
    {
        return Match(topic, _publishRoutes);
    }

    public Route MatchSubscribe(string[] topic)
    {
        return Match(topic, _subscribeRoutes);
    }
}
