using System;
using System.Collections.Generic;
using MQTTnet.Extensions.Hosting.Routes;

namespace MQTTnet.Extensions.Hosting.Internals;

internal sealed class SubscriptionRouteTable : RouteTable<bool>
{
    public SubscriptionRouteTable(IEnumerable<Type> controllers) : base(controllers)
    { }
}
