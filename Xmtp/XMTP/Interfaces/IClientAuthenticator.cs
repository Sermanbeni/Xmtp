using System.Net.Sockets;

namespace Xmtp
{
    public interface IClientAuthenticator<T>
    {
        Task<T?> LogInAsync(TcpClient client, Stream stream, byte[] token, CancellationToken cancellationToken = default);
    }
}
