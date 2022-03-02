using System.Reflection;

namespace MQTTnet.Extensions.Hosting.Routes;

internal enum SegmentType
{
    Normal,
    Parametric,
    SingleLevelWildcard,
    MultiLevelWildcard
}

internal record struct TemplateSegment(string Segment, SegmentType Type, ParameterInfo ParameterInfo);
