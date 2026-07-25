namespace Xmtp
{
    public class CompiledEndpoint
    {
        public readonly bool IsRequest;
        public readonly bool IsAsync;
        public readonly bool UdpAllowed;
        public readonly Delegate Delegate;
        public readonly int ControllerIndex;
        public readonly Type[] ParameterTypes;

        public CompiledEndpoint(bool isRequest, bool isAsync, bool updAllowed, Delegate @delegate, int controllerIndex, IEnumerable<Type> parameterTypes)
        {
            IsRequest = isRequest;
            IsAsync = isAsync;
            UdpAllowed = updAllowed;
            Delegate = @delegate;
            ControllerIndex = controllerIndex;
            ParameterTypes = parameterTypes.ToArray();
        }
    }
}
