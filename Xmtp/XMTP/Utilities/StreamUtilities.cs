using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Xmtp
{
    public static class StreamUtilities
    {
        public static async Task<byte[]> ReadBytesAsync(Stream stream, int length)
        {
            byte[] data = new byte[length];
            int received = 0;
            while (received < length)
            {
                received += await stream.ReadAsync(data, received, length - received);
            }
            return data;
        }

        public static async Task<int> ReadIntAsync(Stream stream)
        {
            byte[] data = await ReadBytesAsync(stream, 4);
            int i = BitConverter.ToInt32(data, 0);
            return i;
        }

        public static async Task<string> ReadTextAsync(Stream stream)
        {
            return await ReadTextAsync(stream, int.MaxValue);
        }

        public static async Task<string> ReadTextAsync(Stream stream, int maxSize)
        {
            int length = await ReadIntAsync(stream);
            if (length > maxSize)
            {
                throw new ProtocolViolationException($"Received size exceeded max size: {length}/{maxSize}");
            }
            byte[] bytes = await ReadBytesAsync(stream, length);
            return Encoding.UTF8.GetString(bytes);
        }

        public static async Task WriteTextAsync(Stream stream, string message)
        {
            byte[] size = BitConverter.GetBytes(message.Length);
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            byte[] payload = new byte[size.Length + bytes.Length];
            await stream.WriteAsync(payload, 0, payload.Length);
        }
    }
}
