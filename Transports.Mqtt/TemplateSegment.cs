using System.Reflection;

namespace Transports.Mqtt
{
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
}
