using System.Reflection;

namespace MQTTnet.Extensions.Hosting.Routes;

internal sealed class TemplateSegment
{
    public string Segment { get; }
    public bool IsParameter { get; }
    public ParameterInfo ParameterInfo { get; }

    public TemplateSegment(string segment, bool isParameter, ParameterInfo parameterInfo)
    {
        Segment = segment;
        IsParameter = isParameter;
        ParameterInfo = parameterInfo;
    }
}
