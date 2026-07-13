using System.Net;
using System.Net.Sockets;

namespace Xmtp
{
    public interface IAuthenticator<T>
    {
        bool Authenticate(IPAddress iPAddress, byte[] token, out T? ID);
    }
}
