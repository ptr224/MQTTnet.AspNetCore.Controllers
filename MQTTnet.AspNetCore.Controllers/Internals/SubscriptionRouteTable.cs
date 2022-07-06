using MQTTnet.AspNetCore.Controllers.Routes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal sealed class SubscriptionRouteTable : RouteTable
{
    public SubscriptionRouteTable(IEnumerable<Type> controllers) : base(controllers)
    {
        // Verifica che i tipi riportati dai metodi siano supportati

        foreach (var route in _routes)
        {
            var type = route.Method.ReturnType;
            if (type != typeof(bool) && type != typeof(ValueTask<bool>) && type != typeof(Task<bool>))
                throw new InvalidOperationException("Invalid action. All routed subscription methods must return either bool or ValueTask<bool> or Task<bool>.");
        }
    }
}
