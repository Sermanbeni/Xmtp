namespace Xmtp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ControllerAttribute : Attribute
    {
        public readonly string[] configurations;
        public readonly bool universal;

        public ControllerAttribute(params string[] configurations)
        {
            this.configurations = configurations;
            universal = false;
        }

        public ControllerAttribute()
        {
            this.configurations = new string[0];
            universal = true;
        }
    }
}
