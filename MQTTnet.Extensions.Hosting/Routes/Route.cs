using System;
using System.Linq;
using System.Reflection;

namespace MQTTnet.Extensions.Hosting.Routes;

internal sealed class Route : IEquatable<Route>
{
    public TemplateSegment[] Template { get; }
    public MethodInfo Method { get; }

    public Route(MethodInfo action, string template)
    {
        // Analizza i singoli segmenti del template (no lazy loading)
        var actionParams = action.GetParameters();
        var segments = template.Split('/')
            .Select(s => s switch
            {
                "" => throw new InvalidOperationException($"Invalid template '{template}'. Empty segments are not allowed."),
                "[controller]" => action.DeclaringType.Name.EndsWith("Controller") ? action.DeclaringType.Name[0..^10] : action.DeclaringType.Name,
                "[action]" => action.Name,
                _ => s
            })
            .Select(s =>
            {
                // Verifica il tipo di segmento e la sua correttezza

                var (type, segment) = s switch
                {
                    "+" => (SegmentType.SingleLevelWildcard, s),
                    "#" => (SegmentType.MultiLevelWildcard, s),
                    _ when s[0] == '{' && s[^1] == '}' => (SegmentType.Parametric, s[1..^1]),
                    _ => (SegmentType.Normal, s)
                };

                ParameterInfo parameterInfo = null;

                if (type == SegmentType.Parametric)
                {
                    if (string.IsNullOrEmpty(segment))
                        throw new InvalidOperationException($"Invalid template '{template}'. Empty parameter name in segment '{s}' is not allowed.");

                    parameterInfo = actionParams.Where(p => p.Name == segment).FirstOrDefault();

                    if (parameterInfo is null)
                        throw new InvalidOperationException($"Invalid template '{template}'. The parameter '{s}' is not defined.");
                }

                return new TemplateSegment(segment, type, parameterInfo);
            })
            .ToList();

        // Verifica che un segmento MultiLevelWildcard sia solo alla fine del topic

        for (int i = 0; i < segments.Count; i++)
            if (segments[i].Type == SegmentType.MultiLevelWildcard && i != segments.Count - 1)
                throw new InvalidOperationException($"Invalid template '{template}'. The multilevel wildcard can be placed only at the end.");

        // Verifica che i parametri corrispondano a quelli dell'azione

        var templateParams = segments.Where(s => s.Type == SegmentType.Parametric);

        if (actionParams.Length != templateParams.Count())
            throw new InvalidOperationException($"Invalid template '{template}'. The number of parameters do not correspond.");

        if (actionParams.Select(p => p.Name).Except(templateParams.Select(p => p.Segment)).Any())
            throw new InvalidOperationException($"Invalid template '{template}'. The template parameters do not correspond with the action parameters.");

        // Inizializza

        Template = segments.ToArray();
        Method = action;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is Route other ? Equals(other) : base.Equals(obj);
    }

    public bool Equals(Route other)
    {
        int count = Math.Min(Template.Length, other.Template.Length);

        for (int i = 0; i < count; i++)
        {
            // Se almeno uno dei due segmenti è # non importa cosa c'è dopo, sono uguali

            if (Template[i].Type == SegmentType.MultiLevelWildcard || other.Template[i].Type == SegmentType.MultiLevelWildcard)
                return true;

            // Se ancora non è stato trovato # non matchano se hanno entrambe almeno un segmento non parametrico con un nome diverso nella medesima posizione

            if (Template[i].Type == SegmentType.Normal && other.Template[i].Type == SegmentType.Normal && Template[i].Segment != other.Template[i].Segment)
                return false;
        }

        // Se finora erano uguali verifica da lunghezza

        return Template.Length == other.Template.Length;
    }

    public bool Match(string[] topic)
    {
        int count = Math.Min(Template.Length, topic.Length);

        for (int i = 0; i < count; i++)
        {
            // Se il segmento è # allora fa match

            if (Template[i].Type == SegmentType.MultiLevelWildcard)
                return true;

            // Se il segmento è normale ma il nome è diverso non fa match

            if (Template[i].Type == SegmentType.Normal && Template[i].Segment != topic[i])
                return false;
        }

        // Se finora facevano match verifica da lunghezza

        return Template.Length == topic.Length;
    }
}
