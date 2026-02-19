using System.Text;

namespace Xmtp
{
    public class XmtpDeliveredMessage
    {
        public readonly string Endpoint;
        public readonly Guid? RequestID;
        public readonly byte[][] Objects;

        public XmtpDeliveredMessage(string endpoint, Guid? requestID, byte[][] objects)
        {
            Endpoint = endpoint;
            RequestID = requestID;
            Objects = objects;
        }

        public XmtpDeliveredMessage(byte[] bytes)
        {
            int position = 0;
            int size;
            byte[] buffer;

            // Read endpoint size
            size = BitConverter.ToInt32(bytes, position);
            position += 4;

            // Read endpoint
            buffer = new byte[size];
            Buffer.BlockCopy(bytes, position, buffer, 0, size);
            position += size;
            Endpoint = Encoding.UTF8.GetString(buffer);

            // Read request ID size
            size = BitConverter.ToInt32(bytes, position);
            position += 4;

            // Read request ID (if exists)
            if (size == 0)
            {
                RequestID = default;
            }
            else
            {
                buffer = new byte[size];
                Buffer.BlockCopy(bytes, position, buffer, 0, size);
                position += size;
                RequestID = new Guid(buffer);
            }

            // Read object count
            int objectCount = BitConverter.ToInt32(bytes, position);
            position += 4;

            // Create objects
            Objects = new byte[objectCount][];

            // Read objects
            for (int i = 0; i < objectCount; i++)
            {
                // Read object size
                size = BitConverter.ToInt32(bytes, position);
                position += 4;

                // Read object
                buffer = new byte[size];
                Buffer.BlockCopy(bytes, position, buffer, 0, size);
                position += size;
                Objects[i] = buffer;
            }
        }
    }
}
