using System.Reflection;

namespace Xmtp
{
    public static class ControllerRegistry
    {
        public static Dictionary<string, CompiledEndpoint> CompileEndpoints<T>(List<ControllerInfo> controllers)
        {
            List<EndpointInfo> methods = RegisterEndpointMethods(controllers);

            Dictionary<string, CompiledEndpoint> compiledEndpoints = EndpointCompiler.CompileEndpoints<T>(methods);

            return compiledEndpoints;
        }

        public static List<ControllerInfo> RegisterControllers<TAttribute>(string serverType, Type BaseType)
            where TAttribute : ControllerAttribute
        {
            Assembly assembly = Assembly.GetEntryAssembly()!;
            Type[] types = assembly.GetTypes();
            List<ControllerInfo> controllers = new List<ControllerInfo>();

            foreach (Type type in types)
            {
                if (type.IsClass && !type.IsAbstract && type.IsPublic)
                {
                    bool isController =
                        type.BaseType != null &&
                        type.BaseType.IsGenericType &&
                        type.BaseType.GetGenericTypeDefinition() == BaseType;
                    if (!isController) continue;

                    ControllerAttribute? controllerAttribute = type.GetCustomAttribute<TAttribute>();
                    if (controllerAttribute == null) continue;
                    if (!controllerAttribute.universal && !controllerAttribute.configurations.Contains(serverType)) continue;
                    RouteAttribute? routeAttribute = type.GetCustomAttribute<RouteAttribute>();
                    if (routeAttribute == null) continue;

                    List<Attribute> attributes = type.GetCustomAttributes().ToList();
                    attributes.Remove(routeAttribute);
                    attributes.Remove(controllerAttribute);

                    controllers.Add(new ControllerInfo(routeAttribute.Root, type, attributes));
                }
            }

            return controllers;
        }

        static List<EndpointInfo> RegisterEndpointMethods(List<ControllerInfo> controllers)
        {
            List<EndpointInfo> endpoints = new List<EndpointInfo>();
            for (int i = 0; i < controllers.Count; i++)
            {
                ControllerInfo controller = controllers[i];
                MethodInfo[] methods = controller.Type.GetMethods();
                foreach (MethodInfo method in methods)
                {
                    EndpointAttribute? endpointAttribute = method.GetCustomAttribute<EndpointAttribute>();
                    if (endpointAttribute == null) continue;

                    string route = controller.Route + endpointAttribute.Endpoint;

                    ParameterInfo[] parameters = method.GetParameters();
                    bool ok = true;
                    foreach (ParameterInfo parameter in parameters)
                    {
                        if (parameter.ParameterType.IsByRef)
                        {
                            ok = false;
                            break;
                        }
                    }
                    if (!ok) continue;

                    endpoints.Add(new EndpointInfo(method, route, i));
                }
            }
            return endpoints;
        }
    }
}
