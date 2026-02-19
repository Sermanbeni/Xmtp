using System.Linq.Expressions;
using System.Reflection;

namespace Xmtp
{
    public static class EndpointCompiler
    {
        public static Dictionary<string, CompiledEndpoint> CompileEndpoints<T>(List<EndpointInfo> endpointInfos)
        {
            var endpoints = new Dictionary<string, CompiledEndpoint>();

            foreach (var endpoint in endpointInfos)
            {
                var compiled = CompileEndpoint<T>(endpoint);
                endpoints[endpoint.Route] = compiled;
            }

            return endpoints;
        }

        private static CompiledEndpoint CompileEndpoint<T>(EndpointInfo endpoint)
        {
            var method = endpoint.Method;
            var returnType = method.ReturnType;
            var parameterTypes = method.GetParameters().Select(p => p.ParameterType);

            if (returnType == typeof(void))
            {
                var compiled = CompileVoidMethod<T>(method);
                return new CompiledEndpoint(false, false, compiled, endpoint.ControllerIndex, parameterTypes);
            }
            else if (returnType == typeof(Task))
            {
                var compiled = CompileTaskMethod<T>(method);
                return new CompiledEndpoint(false, true, compiled, endpoint.ControllerIndex, parameterTypes);
            }
            else if (returnType == typeof(Task<object>))
            {
                var compiled = CompileTaskObjectMethod<T>(method);
                return new CompiledEndpoint(true, true, compiled, endpoint.ControllerIndex, parameterTypes);
            }
            else
            {
                var compiled = CompileObjectMethod<T>(method);
                return new CompiledEndpoint(true, false, compiled, endpoint.ControllerIndex, parameterTypes);
            }
        }

        private static Action<object, object[]> CompileVoidMethod<T>(MethodInfo method)
        {
            return CompileCore<Action<object, object[]>, T>(method, call => call);
        }

        private static Func<object, object[], Task> CompileTaskMethod<T>(MethodInfo method)
        {
            return CompileCore<Func<object, object[], Task>, T>(method, call => call);
        }

        private static Func<object, object[], Task<object>> CompileTaskObjectMethod<T>(MethodInfo method)
        {
            return CompileCore<Func<object, object[], Task<object>>, T>(method, call => call);
        }

        private static Func<object, object[], object> CompileObjectMethod<T>(MethodInfo method)
        {
            return CompileCore<Func<object, object[], object>, T>(method, call => Expression.Convert(call, typeof(object)));
        }

        private static TDelegate CompileCore<TDelegate, TController>(
            MethodInfo method,
            Func<MethodCallExpression, Expression> transformResult)
            where TDelegate : Delegate
        {
            var controllerParam = Expression.Parameter(typeof(object), "controller");
            var parametersParam = Expression.Parameter(typeof(object[]), "parameters");

            var castController = Expression.Convert(controllerParam, method.DeclaringType!.MakeGenericType(typeof(TController)));

            var methodParams = method.GetParameters();
            var paramExpressions = new Expression[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++)
            {
                var arrayAccess = Expression.ArrayIndex(parametersParam, Expression.Constant(i));
                paramExpressions[i] = Expression.Convert(arrayAccess, methodParams[i].ParameterType);
            }

            var constructedMethod = method.DeclaringType.MakeGenericType(typeof(TController))
                .GetMethod(method.Name, method.GetParameters().Select(p => p.ParameterType).ToArray())!;
            var methodCall = Expression.Call(castController, constructedMethod, paramExpressions);

            var body = transformResult(methodCall);

            var lambda = Expression.Lambda<TDelegate>(body, controllerParam, parametersParam);
            return lambda.Compile();
        }
    }
}
