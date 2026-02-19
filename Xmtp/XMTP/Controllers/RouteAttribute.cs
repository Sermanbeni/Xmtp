namespace Xmtp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RouteAttribute : Attribute
    {
        public readonly string Root;

        public RouteAttribute(string root) => Root = root + "/";

        public RouteAttribute() => Root = "";
    }
}
