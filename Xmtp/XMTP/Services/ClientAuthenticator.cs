using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Xmtp
{
    public class ClientAuthenticator<T> : IClientAuthenticator<T>
    {
        public async Task<T?> LogInAsync(Stream stream, byte[] token, CancellationToken ct = default)
        {
            byte[] buffer = new byte[4];
            int received = 0;
            while (received < 4)
            {
                received += await stream.ReadAsync(buffer, received, 4 - received);
            }

            string message = Encoding.UTF8.GetString(buffer);

            if (message != "AUTH")
            {
                return default;
            }
            
            await stream.WriteAsync(token, 0, token.Length);

            byte[] remoteIdBuffer;
            received = 0;

            while (received < 4)
            {
                int r = await stream.ReadAsync(buffer, received, 4 - received, ct);
                if (r == 0) return default;
                received += r;
            }
            int size = BitConverter.ToInt32(buffer, 0);
            remoteIdBuffer = new byte[size];
            received = 0;
            while (received < size)
            {
                int r = await stream.ReadAsync(remoteIdBuffer, received, size - received, ct);
                if (r == 0) return default;
                received += r;
            }
            string m = Encoding.UTF8.GetString(remoteIdBuffer);
            
            return JsonSerializer.Deserialize<T>(m);
        }
    }
}
