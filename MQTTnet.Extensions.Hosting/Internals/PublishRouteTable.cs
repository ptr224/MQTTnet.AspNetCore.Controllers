using System;
using System.Collections.Generic;
using MQTTnet.Extensions.Hosting.Routes;

namespace MQTTnet.Extensions.Hosting.Internals;

internal sealed class PublishRouteTable : RouteTable<IMqttPublishResult>
{
    public PublishRouteTable(IEnumerable<Type> controllers) : base(controllers)
    { }
}
