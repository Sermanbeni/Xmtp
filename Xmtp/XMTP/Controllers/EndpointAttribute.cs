namespace Xmtp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class EndpointAttribute : Attribute
    {
        public readonly string Endpoint;

        public EndpointAttribute(string endpoint) => Endpoint = endpoint;
    }
}
