namespace Xmtp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ServerControllerAttribute : ControllerAttribute
    {
        public ServerControllerAttribute() : base() { }

        public ServerControllerAttribute(params string[] configurations) : base(configurations) { }
    }
}
