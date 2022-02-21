using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Transports.Mqtt
{
    internal sealed class RouteTable
    {
        private static Route ParseRoute(MethodInfo action)
        {
            // Verifica che il tipo riportato dal metodo sia supportato
            if (action.ReturnType != typeof(void) && action.ReturnType != typeof(Task))
                throw new InvalidOperationException($"Invalid action. All routed methods must return either void or Task.");

            // Prende i template dell'azione e del controller e li unisce
            string controllerTemplate = action.DeclaringType.GetCustomAttribute<MqttRouteAttribute>(false)?.Template;
            string actionTemplate = action.GetCustomAttribute<MqttRouteAttribute>(false).Template;
            string template = controllerTemplate is null ? actionTemplate : string.Join('/', controllerTemplate, actionTemplate);

            // Analizza i singoli segmenti del template (no lazy loading)
            var actionParams = action.GetParameters();
            var segments = template.Split('/')
                .Select(s => s switch
                {
                    "" => throw new InvalidOperationException($"Invalid template '{template}'. Empty segments are not allowed."),
                    "[controller]" => action.DeclaringType.Name.EndsWith("Controller") ? action.DeclaringType.Name[0..^10] : action.DeclaringType.Name,
                    "[action]" => action.Name,
                    _ => s
                })
                .Select(s =>
                {
                    // Verifica se il segmento sia un parametro e la sua correttezza
                    bool isParameter = s[0] == '{' && s[^1] == '}';

                    if (isParameter && s.Length < 3)
                        throw new InvalidOperationException($"Invalid template '{template}'. Empty parameter name in segment '{s}' is not allowed.");

                    string segment = isParameter ? s[1..^1] : s;
                    ParameterInfo parameterInfo = null;

                    if (isParameter && (parameterInfo = actionParams.Where(p => p.Name == segment).FirstOrDefault()) is null)
                        throw new InvalidOperationException($"Invalid template '{template}'. The parameter '{s}' is not defined.");

                    return new TemplateSegment(segment, isParameter, parameterInfo);
                })
                .ToArray();

            // Verifica che i parametri corrispondano a quelli dell'azione
            var templateParams = segments.Where(s => s.IsParameter);

            if (actionParams.Length != templateParams.Count())
                throw new InvalidOperationException($"Invalid template '{template}'. The number of parameters do not correspond.");

            if (actionParams.Select(p => p.Name).Except(templateParams.Select(p => p.Segment)).Any())
                throw new InvalidOperationException($"Invalid template '{template}'. The template parameters do not correspond with the action parameters.");

            return new(segments, action);
        }

        public static RouteTable Create(IEnumerable<Type> controllers)
        {
            // Seleziona dai controller specificati tutti i metodi con un MqttRoute e parsane il template
            var routes = controllers
                .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
                .Where(method => method.GetCustomAttribute<MqttRouteAttribute>(false) is not null)
                .Select(ParseRoute)
                .ToArray();

            // Verifica che non ci siano route uguali
            if (routes.Length != routes.Distinct(new RouteComparer()).Count())
                throw new InvalidOperationException($"Cannot build route table. Two or more routes overlap.");

            return new RouteTable(routes);
        }

        private readonly Route[] _routes;

        private RouteTable(Route[] routes)
        {
            _routes = routes;
        }

        internal Route Match(string[] topic)
        {
            // Trova una Route compatibile con il topic passato o null
            foreach (var route in _routes)
            {
                if (topic.Length != route.Template.Length)
                    continue;

                int i;
                bool match = true;

                for (i = 0; i < topic.Length; i++)
                {
                    if (!route.Template[i].IsParameter && route.Template[i].Segment != topic[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return route;
            }

            return null;
        }
    }
}
