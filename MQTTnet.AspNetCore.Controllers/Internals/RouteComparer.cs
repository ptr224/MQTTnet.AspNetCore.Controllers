using System.Diagnostics.CodeAnalysis;

namespace MQTTnet.AspNetCore.Controllers.Internals;

class RouteComparer : EqualityComparer<Route>
{
    public override bool Equals(Route? x, Route? y)
    {
        if (x is null && y is null)
            return true;
        else if (x is null || y is null)
            return false;

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

    public override int GetHashCode([DisallowNull] Route obj)
    {
        return 0;
    }
}
