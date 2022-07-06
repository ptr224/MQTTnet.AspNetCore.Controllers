using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MQTTnet.AspNetCore.Controllers.Routes;

internal abstract class RouteTable
{
    protected readonly Route[] _routes;

    public RouteTable(IEnumerable<Type> controllers)
    {
        // Seleziona dai controller specificati tutti i metodi con un template e ritornane la route

        var routes = new List<Route>();

        foreach (var controller in controllers)
        {
            var controllerRoute = controller.GetCustomAttribute<MqttRouteAttribute>(false);

            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
            {
                // Verifica che il metodo abbia un template

                var methodRoute = method.GetCustomAttribute<MqttRouteAttribute>(false);

                if (methodRoute is null)
                    continue;

                // Prende i template del metodo e del controller e li unisce

                string template = controllerRoute is null
                    ? methodRoute.Template
                    : string.Join('/', controllerRoute.Template, methodRoute.Template);

                routes.Add(new(method, template));
            }
        }

        // Verifica che non ci siano route uguali

        if (routes.Count != routes.Distinct(new RouteComparer()).Count())
            throw new InvalidOperationException($"Cannot build route table. Two or more routes overlap.");

        _routes = routes.ToArray();
    }

    public Route Match(string[] topic)
    {
        // Trova una Route compatibile con il topic passato o null
        return _routes.Where(r => r.Match(topic)).FirstOrDefault();
    }
}
