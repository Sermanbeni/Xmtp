using System.Collections.ObjectModel;
using System.Reflection;

namespace Xmtp
{
    public class ControllerFactory<T> : IControllerFactory
    {
        private readonly List<CompiledConstructible> compiledConstructors;

        private class CompiledConstructible
        {
            public Type TypeToInstantiate { get; }
            public ConstructorInfo Constructor { get; }
            public object[] Parameters { get; }

            public CompiledConstructible(Type typeToInstantiate, ConstructorInfo constructor, object[] parameters)
            {
                TypeToInstantiate = typeToInstantiate;
                Constructor = constructor;
                Parameters = parameters ?? Array.Empty<object>();
            }

            public object Construct()
            {
                return Parameters.Length == 0
                    ? Activator.CreateInstance(TypeToInstantiate)!
                    : Constructor.Invoke(Parameters);
            }

        }

        public ControllerFactory(List<Type> controllers, ReadOnlyDictionary<Type, object> services)
        {
            if (controllers == null) throw new ArgumentNullException(nameof(controllers));
            if (services == null) throw new ArgumentNullException(nameof(services));

            compiledConstructors = new List<CompiledConstructible>();

            foreach (var controllerType in controllers)
            {
                var compiled = CompileConstructor(controllerType, services);
                compiledConstructors.Add(compiled);
            }
        }

        public List<object> InstantiateControllers()
        {
            var instances = new List<object>(compiledConstructors.Count);

            foreach (var compiled in compiledConstructors)
            {
                instances.Add(compiled.Construct());
            }

            return instances;
        }

        private CompiledConstructible CompileConstructor(Type controllerType, ReadOnlyDictionary<Type, object> services)
        {
            Type typeToInstantiate = GetControllerTypeToInstantiate(controllerType);

            var constructor = GetConstructor(typeToInstantiate);

            var parameters = ResolveConstructorParameters(constructor, services);

            return new CompiledConstructible(typeToInstantiate, constructor, parameters);
        }

        private Type GetControllerTypeToInstantiate(Type controllerType)
        {
            if (controllerType.IsGenericTypeDefinition)
            {
                return controllerType.MakeGenericType(typeof(T));
            }

            return controllerType;
        }

        private ConstructorInfo GetConstructor(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            if (constructors.Length == 0)
            {
                var parameterlessConstructor = type.GetConstructor(Type.EmptyTypes);
                if (parameterlessConstructor == null)
                {
                    throw new InvalidOperationException(
                        $"Type {type.Name} has no suitable constructor.");
                }
                return parameterlessConstructor;
            }

            if (constructors.Length == 1)
            {
                return constructors[0];
            }

            return constructors
                .OrderByDescending(c => c.GetParameters().Length)
                .First();
        }

        private object[] ResolveConstructorParameters(ConstructorInfo constructor, ReadOnlyDictionary<Type, object> services)
        {
            var parameterInfos = constructor.GetParameters();

            if (parameterInfos.Length == 0)
            {
                return Array.Empty<object>();
            }

            var parameters = new object[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                var paramType = parameterInfos[i].ParameterType;

                if (services.TryGetValue(paramType, out object service))
                {
                    parameters[i] = service;
                    continue;
                }

                var assignableService = FindAssignableService(paramType, services);
                if (assignableService == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot resolve service for type {paramType.Name} required by {constructor.DeclaringType.Name}");
                }

                parameters[i] = assignableService;
            }

            return parameters;
        }

        private object FindAssignableService(Type requestedType, ReadOnlyDictionary<Type, object> services)
        {
            foreach (var kvp in services)
            {
                if (requestedType.IsAssignableFrom(kvp.Key))
                {
                    return kvp.Value;
                }
            }

            return null;
        }
    }
}
