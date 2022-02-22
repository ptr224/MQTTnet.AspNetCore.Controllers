using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MQTTnet.Extensions.Hosting.Routes;

internal sealed class Route : IEquatable<Route>
{
    public TemplateSegment[] Template { get; }
    public MethodInfo Method { get; }

    public Route(TemplateSegment[] template, MethodInfo method)
    {
        Template = template;
        Method = method;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Template, Method);
    }

    public override bool Equals(object obj)
    {
        if (obj is Route other)
            return Equals(other);
        else
            return base.Equals(obj);
    }

    public bool Equals(Route other)
    {
        if (Template.Length != other.Template.Length)
            return false;
        else
        {
            // Non matchano se hanno entrambe almeno un segmento non parametrico con un nome diverso nella medesima posizione
            bool match = true;

            for (int i = 0; i < Template.Length; i++)
            {
                if (!Template[i].IsParameter && !other.Template[i].IsParameter && Template[i].Segment != other.Template[i].Segment)
                {
                    match = false;
                    break;
                }
            }

            return match;
        }
    }
}

internal class RouteComparer : IEqualityComparer<Route>
{
    public bool Equals(Route x, Route y)
    {
        return x.Equals(y);
    }

    public int GetHashCode([DisallowNull] Route obj)
    {
        return 0;
    }
}
