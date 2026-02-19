namespace Xmtp
{
    public abstract class ClientControllerBase<T> : ControllerBase<T>
    {
        public XmtpClient<T> Client;
    }
}
