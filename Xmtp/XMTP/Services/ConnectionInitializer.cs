using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Xmtp
{
    public class ConnectionInitializer : IConnectionInitializer
    {
        readonly int tokenSize;
        readonly int timeoutDuration;

        public ConnectionInitializer(int tokenSize, int timeoutDuration)
        {
            this.tokenSize = tokenSize;
            this.timeoutDuration = timeoutDuration;
        }

        public byte[] CreateInitializationMessage(object remoteID)
        {
            byte[] ID = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(remoteID));
            byte[] message = new byte[ID.Length + 4];
            byte[] l = BitConverter.GetBytes(ID.Length);
            Buffer.BlockCopy(l, 0, message, 0, 4);
            Buffer.BlockCopy(ID, 0, message, 4, ID.Length);
            return message;
        }

        public async Task<byte[]?> InitiateConnection(Stream stream, CancellationToken ct)
        {
            byte[] message = Encoding.UTF8.GetBytes("AUTH");
            await stream.WriteAsync(message, ct);

            byte[] buffer = new byte[tokenSize];

            Task authRequest = Task.Run(async () =>
            {
                int read = 0;
                while (read < tokenSize)
                {
                    read += await stream.ReadAsync(buffer, read, tokenSize - read, ct);
                }
            });

            bool success = authRequest == await Task.WhenAny(authRequest, Task.Delay(timeoutDuration));

            if (success) return buffer;
            else return null;
        }
    }
}
