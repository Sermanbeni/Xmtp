using System.Net.Sockets;

namespace Xmtp
{
    public interface IAuthenticator<T>
    {
        bool Authenticate(byte[] token, out T? ID);
    }
}
