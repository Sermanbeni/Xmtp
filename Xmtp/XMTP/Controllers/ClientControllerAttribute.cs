namespace Xmtp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ClientControllerAttribute : ControllerAttribute
    {
        public ClientControllerAttribute() : base() { }

        public ClientControllerAttribute(params string[] configurations) : base(configurations) { }
    }
}
