using MQTTnet.AspNetCore.Controllers.Routes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal sealed class PublishRouteTable : RouteTable
{
    public PublishRouteTable(IEnumerable<Type> controllers) : base(controllers)
    {
        // Verifica che i tipi riportati dai metodi siano supportati

        foreach (var route in _routes)
        {
            var type = route.Method.ReturnType;
            if (type != typeof(IMqttPublishResult) && type != typeof(ValueTask<IMqttPublishResult>) && type != typeof(Task<IMqttPublishResult>))
                throw new InvalidOperationException("Invalid action. All routed publish methods must return either IMqttPublishResult or ValueTask<IMqttPublishResult> or Task<IMqttPublishResult>.");
        }
    }
}
