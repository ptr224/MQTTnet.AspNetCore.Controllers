using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal class RouteActivator : IAsyncDisposable
{
    private class DefaultModelBinder : IMqttModelBinder
    {
        public static DefaultModelBinder Instance { get; } = new();

        public ValueTask BindModelAsync(ModelBindingContext context)
        {
            var value = context.Value.Type.IsEnum ? Enum.Parse(context.Value.Type, context.Value.Value) : Convert.ChangeType(context.Value.Value, context.Value.Type);
            context.Result = ModelBindingResult.Success(value);
            return ValueTask.CompletedTask;
        }
    }

    private static object?[]? GetParameters(Route route, ActionContext context)
    {
        var paramsCount = route.Method.GetParameters().Length;
        if (paramsCount == 0)
            return null;

        var parameters = new object?[paramsCount];

        foreach (var segment in route.Template.Where(s => s.Type == SegmentType.Parametric && s.Parameter is not null))
            parameters[segment.Parameter!.Info.Position] = context.Parameters[segment.Segment];

        return parameters;
    }

    private static async ValueTask Activate(Route route, ActionContext context, int step)
    {
        // Esegui filtro se presente, altrimenti invoca azione

        if (step < route.ActionFilters.Length)
        {
            await route.ActionFilters[step].OnActionAsync(context, () => Activate(route, context, step + 1));
        }
        else
        {
            var parameters = GetParameters(route, context);
            var returnValue = route.Method.Invoke(context.Controller, parameters);

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

    private readonly Route _route;
    private readonly string[] _topic;
    private readonly MqttContext _context;
    private readonly IServiceProvider _services;
    private readonly object _controller;

    public RouteActivator(Route route, string[] topic, MqttContext context, IServiceProvider services)
    {
        _route = route;
        _topic = topic;
        _context = context;
        _services = services;

        // Istanzia controller e assegna contesto

        _controller = ActivatorUtilities.CreateInstance(services, route.Method.DeclaringType!);

        if (_controller is MqttControllerBase controller)
            controller.MqttContext = context;
        else
            throw new InvalidOperationException($"Controller must inherit from {nameof(MqttControllerBase)}");
    }

    public async ValueTask ActivateAsync()
    {
        // Costruisci dizionario parametri

        var parameters = new Dictionary<string, object?>();

        for (int i = 0; i < _route.Template.Length; i++)
        {
            var segment = _route.Template[i];
            if (segment.Type == SegmentType.Parametric && segment.Parameter is not null)
            {
                // Scorri binder finché non viene tornato un risultato
                // Prima binder del parametro, poi gli altri, infine default

                var binderContext = new ModelBindingContext(_services, segment.Parameter.Info.ParameterType, _topic[i]);
                var binders = segment.Parameter.ModelBinders
                    .Concat(_route.ModelBinders)
                    .Append(DefaultModelBinder.Instance);

                foreach (var binder in binders)
                {
                    await binder.BindModelAsync(binderContext);
                    if (binderContext.Result.IsSet)
                        break;
                }

                parameters[segment.Segment] = binderContext.Result.Model;
            }
        }

        // Finalizza

        var context = new ActionContext(_context, _controller, _services, parameters);
        await Activate(_route, context, 0);
    }

    public async ValueTask DisposeAsync()
    {
        if (_controller is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_controller is IDisposable disposable)
            disposable.Dispose();
    }
}
