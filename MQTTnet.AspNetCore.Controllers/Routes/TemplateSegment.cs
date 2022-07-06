using System.Reflection;

namespace MQTTnet.AspNetCore.Controllers.Routes;

internal enum SegmentType
{
    Normal,
    Parametric,
    SingleLevelWildcard,
    MultiLevelWildcard
}

internal record struct TemplateSegment(string Segment, SegmentType Type, ParameterInfo ParameterInfo);
