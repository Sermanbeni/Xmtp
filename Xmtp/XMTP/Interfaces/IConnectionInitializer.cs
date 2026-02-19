using System.Net.Sockets;

namespace Xmtp
{
    public interface IConnectionInitializer
    {
        /// <returns>Token of the client</returns>
        Task<byte[]?> InitiateConnection(Stream stream, CancellationToken ct);

        byte[] CreateInitializationMessage(object remoteID);
    }
}
