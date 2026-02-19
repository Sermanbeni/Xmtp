namespace Xmtp
{
    public class ControllerInfo
    {
        public string Route;
        public Type Type;
        public Attribute[] Attributes;

        public ControllerInfo(string route, Type type, IEnumerable<Attribute> attributes)
        {
            this.Route = route;
            this.Type = type;
            this.Attributes = attributes.ToArray();
        }
    }
}
