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

    private readonly Route _route;
    private readonly ActionContext _context;

    public RouteActivator(Route route, string[] topic, MqttContext context, IServiceProvider services)
    {
        // Istanzia controller e assegna contesto

        var obj = ActivatorUtilities.CreateInstance(services, route.Method.DeclaringType!);

        if (obj is MqttControllerBase controller)
            controller.MqttContext = context;
        else
            throw new InvalidOperationException($"Controller must inherit from {nameof(MqttControllerBase)}");

        // Costruisci dizionario parametri

        var parameters = new Dictionary<string, string>();

        for (int i = 0; i < route.Template.Length; i++)
        {
            var segment = route.Template[i];
            if (segment.Type == SegmentType.Parametric)
                parameters[segment.Segment] = topic[i];
        }

        // Finalizza

        _route = route;
        _context = new(context, obj, services, parameters);
    }

    private async ValueTask<object?[]?> GetParameters()
    {
        var paramsCount = _route.Method.GetParameters().Length;
        if (paramsCount == 0)
            return null;

        var parameters = new object?[paramsCount];

        foreach (var segment in _route.Template.Where(s => s.Type == SegmentType.Parametric && s.Parameter is not null))
        {
            var param = segment.Parameter!.Info;

            // Scorri binder finché non viene tornato un risultato
            // Prima binder del parametro, poi gli altri, infine default

            var binderContext = new ModelBindingContext(_context.Services, param.ParameterType, _context.Parameters[segment.Segment]);
            var binders = segment.Parameter.ModelBinders
                .Concat(_route.ModelBinders)
                .Append(DefaultModelBinder.Instance);

            foreach (var binder in binders)
            {
                await binder.BindModelAsync(binderContext);
                if (binderContext.Result.IsSet)
                    break;
            }

            parameters[param.Position] = binderContext.Result.Model;
        }

        return parameters;
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
            var parameters = await GetParameters();
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

    public async ValueTask DisposeAsync()
    {
        if (_context.Controller is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_context.Controller is IDisposable disposable)
            disposable.Dispose();
    }
}
