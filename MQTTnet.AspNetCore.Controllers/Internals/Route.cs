using System;
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

internal record ParametricSegmentInfo(IMqttModelBinder[] ModelBinders, ParameterInfo Info);

internal record TemplateSegment(string Segment, SegmentType Type, ParametricSegmentInfo? Parameter);

internal sealed class Route
{
    public MethodInfo Method { get; }
    public TemplateSegment[] Template { get; }
    public IMqttActionFilter[] ActionFilters { get; }
    public IMqttModelBinder[] ModelBinders { get; }

    public Route(MethodInfo action, string template, IMqttActionFilter[] actionFilters, IMqttModelBinder[] modelBinders)
    {
        Method = action;
        ActionFilters = actionFilters;
        ModelBinders = modelBinders;

        // Analizza i singoli segmenti del template (no lazy loading)

        var parameters = action.GetParameters();

        Template = template
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

                ParametricSegmentInfo? info = null;

                if (type == SegmentType.Parametric)
                {
                    // Verifica che il parametro abbia un nome valido

                    if (segment.Any(c => !char.IsLetterOrDigit(c)))
                        throw new InvalidOperationException($"Invalid template '{template}'. The parameter name in segment '{s}' is not allowed.");

                    // Verifica se il segmento abbia un parametro corrispondente

                    var param = parameters.Where(p => p.Name == segment).FirstOrDefault();
                    if (param is not null)
                    {
                        var binders = param.GetCustomAttributes<MqttModelBinderAttribute>(true)
                            .ToArray();

                        info = new(binders, param);
                    }
                }

                return new TemplateSegment(segment, type, info);
            })
            .ToArray();

        // Verifica che un segmento MultiLevelWildcard sia solo alla fine del topic

        for (int i = 0; i < Template.Length; i++)
            if (Template[i].Type == SegmentType.MultiLevelWildcard && i != Template.Length - 1)
                throw new InvalidOperationException($"Invalid template '{template}'. The multilevel wildcard can be placed only at the end.");

        // Verifica che i parametri corrispondano a quelli dell'azione

        var actionParams = parameters
            .Select(p => p.Name);

        var templateParams = Template
            .Where(s => s.Type == SegmentType.Parametric)
            .Select(p => p.Segment);

        if (actionParams.Except(templateParams).Any())
            throw new InvalidOperationException($"Invalid template '{template}'. Missing action parameters.");
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
