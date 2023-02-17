using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal class ActionActivator
{
    private readonly object?[]? parameters;
    private readonly Route _route;
    private readonly ActionContext _context;

    public ActionActivator(string[] topic, Route route, MqttControllerBase controller, IServiceScope scope)
    {
        _route = route;

        // Costruisci dizionario ed array parametri

        var actionParams = new Dictionary<string, string>();

        var methodParams = route.Method.GetParameters();
        if (methodParams.Length > 0)
            parameters = new object?[methodParams.Length];

        for (int i = 0; i < route.Template.Length; i++)
        {
            var segment = route.Template[i];
            if (segment.Type == SegmentType.Parametric)
            {
                actionParams[segment.Segment] = topic[i];

                var info = methodParams.Where(p => p.Name == segment.Segment).FirstOrDefault();
                if (info is not null)
                    parameters![info.Position] = info.ParameterType.IsEnum ? Enum.Parse(info.ParameterType, topic[i]) : Convert.ChangeType(topic[i], info.ParameterType);
            }
        }

        // Assegna contesto

        _context = new(controller, scope.ServiceProvider, actionParams);
    }

    private async ValueTask Activate(int step)
    {
        // Esegui filtro se presente, altrimenti invoca azione

        if (step < _route.ActionFilters.Length)
        {
            await _route.ActionFilters[step].OnActionAsync(_context, () => Activate(step + 1));
        }
        else
        {
            var returnValue = _route.Method.Invoke(_context.Controller, parameters);

            if (returnValue is Task task)
            {
                await task;
            }
            else if (returnValue is ValueTask valueTask)
            {
                await valueTask;
            }
        }
    }

    public ValueTask Activate()
    {
        return Activate(0);
    }
}
