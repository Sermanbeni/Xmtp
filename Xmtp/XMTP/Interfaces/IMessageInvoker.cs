namespace Xmtp
{
    public interface IMessageInvoker
    {
        public void InvokeMessage(CompiledEndpoint endpoint, object controller, object[] parameters);
    }
}
