using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal class HandlerActivator<T> : IAsyncDisposable where T : class
{
    private readonly object obj;

    public T Handler => (obj as T)!;

    public HandlerActivator(IServiceProvider services, Type type)
    {
        if (!type.IsAssignableTo(typeof(T)))
            throw new ArgumentException($"Type must implement {nameof(T)}", nameof(type));

        obj = ActivatorUtilities.CreateInstance(services, type);
    }

    public async ValueTask DisposeAsync()
    {
        if (obj is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (obj is IDisposable disposable)
            disposable.Dispose();
    }
}
