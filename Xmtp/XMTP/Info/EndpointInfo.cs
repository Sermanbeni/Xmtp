using System.Reflection;

namespace Xmtp
{
    public class EndpointInfo
    {
        public MethodInfo Method;
        public string Route;
        public int ControllerIndex;
        public List<Attribute> Attributes;

        public EndpointInfo(MethodInfo method, string route, int controllerIndex, List<Attribute> attributes)
        {
            Method = method;
            Route = route;
            ControllerIndex = controllerIndex;
            Attributes = attributes;
        }
    }
}
