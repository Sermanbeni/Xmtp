using System.Reflection;

namespace Xmtp
{
    public class EndpointInfo
    {
        public MethodInfo Method;
        public string Route;
        public int ControllerIndex;

        public EndpointInfo(MethodInfo method, string route, int controllerIndex)
        {
            Method = method;
            Route = route;
            ControllerIndex = controllerIndex;
        }
    }
}
