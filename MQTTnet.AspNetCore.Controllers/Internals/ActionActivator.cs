using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal class ActionActivator
{
    private readonly string[] _topic;
    private readonly Route _route;
    private readonly ActionContext _context;

    public ActionActivator(string[] topic, Route route, MqttControllerBase controller, IServiceScope scope)
    {
        _topic = topic;
        _route = route;
        _context = new(controller, scope.ServiceProvider);
    }

    private static object?[]? GetParams(string[] topic, Route route)
    {
        var paramsCount = route.Method.GetParameters().Length;

        if (paramsCount == 0)
        {
            return null;
        }
        else
        {
            var parameters = new object?[paramsCount];

            for (int i = 0; i < route.Template.Length; i++)
            {
                var segment = route.Template[i];
                if (segment.Type == SegmentType.Parametric)
                {
                    var info = segment.ParameterInfo!;
                    parameters[info.Position] = info.ParameterType.IsEnum ? Enum.Parse(info.ParameterType, topic[i]) : Convert.ChangeType(topic[i], info.ParameterType);
                }
            }

            return parameters;
        }
    }

    private async ValueTask Activate(int step)
    {
        if (step < _route.ActionFilters.Length)
        {
            await _route.ActionFilters[step].OnActionAsync(_context, () => Activate(step + 1));
        }
        else
        {
            var parameters = GetParams(_topic, _route);
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
