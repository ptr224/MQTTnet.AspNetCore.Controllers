using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MQTTnet.AspNetCore.Controllers.Routes;

internal class RouteComparer : IEqualityComparer<Route>
{
    public bool Equals(Route x, Route y)
    {
        int count = Math.Min(x.Template.Length, y.Template.Length);

        for (int i = 0; i < count; i++)
        {
            // Se almeno uno dei due segmenti è # non importa cosa c'è dopo, sono uguali

            if (x.Template[i].Type == SegmentType.MultiLevelWildcard || y.Template[i].Type == SegmentType.MultiLevelWildcard)
                return true;

            // Se ancora non è stato trovato # non matchano se hanno entrambe almeno un segmento non parametrico con un nome diverso nella medesima posizione

            if (x.Template[i].Type == SegmentType.Normal && y.Template[i].Type == SegmentType.Normal && x.Template[i].Segment != y.Template[i].Segment)
                return false;
        }

        // Se finora erano uguali verifica da lunghezza

        return x.Template.Length == y.Template.Length;
    }

    public int GetHashCode([DisallowNull] Route obj)
    {
        return 0;
    }
}
