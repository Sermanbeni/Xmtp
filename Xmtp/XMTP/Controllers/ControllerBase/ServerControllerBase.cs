namespace Xmtp
{
    public abstract class ServerControllerBase<T> : ControllerBase<T>
    {
        public T RemoteID;
        public XmtpServer<T> Server;
    }
}
