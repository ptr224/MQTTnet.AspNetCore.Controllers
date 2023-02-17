using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal enum SegmentType
{
    Normal,
    Parametric,
    SingleLevelWildcard,
    MultiLevelWildcard
}

internal record struct TemplateSegment(string Segment, SegmentType Type);

internal sealed class Route
{
    public MethodInfo Method { get; }
    public TemplateSegment[] Template { get; }
    public IMqttActionFilter[] ActionFilters { get; }

    public Route(MethodInfo action, string template, IEnumerable<IMqttActionFilter> actionFilters)
    {
        // Analizza i singoli segmenti del template (no lazy loading)

        var segments = template
            .Split('/')
            .Select(s => s switch
            {
                "" => throw new InvalidOperationException($"Invalid template '{template}'. Empty segments are not allowed."),
                "[controller]" => action.DeclaringType!.Name.EndsWith("Controller") ? action.DeclaringType.Name[0..^10] : action.DeclaringType.Name,
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

                if (type == SegmentType.Parametric && string.IsNullOrWhiteSpace(segment))
                    throw new InvalidOperationException($"Invalid template '{template}'. Empty parameter name in segment '{s}' is not allowed.");

                return new TemplateSegment(segment, type);
            })
            .ToList();

        // Verifica che un segmento MultiLevelWildcard sia solo alla fine del topic

        for (int i = 0; i < segments.Count; i++)
            if (segments[i].Type == SegmentType.MultiLevelWildcard && i != segments.Count - 1)
                throw new InvalidOperationException($"Invalid template '{template}'. The multilevel wildcard can be placed only at the end.");

        // Verifica che i parametri corrispondano a quelli dell'azione

        var actionParams = action
            .GetParameters()
            .Select(p => p.Name);

        var templateParams = segments
            .Where(s => s.Type == SegmentType.Parametric)
            .Select(p => p.Segment);

        if (actionParams.Except(templateParams).Any())
            throw new InvalidOperationException($"Invalid template '{template}'. Missing action parameters.");

        // Inizializza

        Method = action;
        Template = segments.ToArray();
        ActionFilters = actionFilters.ToArray();
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
