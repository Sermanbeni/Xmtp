using System.Security.AccessControl;
using System.Text;
using System.Text.Json;

namespace Xmtp
{
    public class XmtpMessage
    {
        byte[] requestID;
        byte[] endpoint;
        byte[][] objects;

        public XmtpMessage(string endpoint, object[] objects)
        {
            requestID = null;
            this.endpoint = Encoding.UTF8.GetBytes(endpoint);
            this.objects = new byte[objects.Length][];
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] is byte[])
                {
                    this.objects[i] = (byte[])objects[i];
                }
                else
                {
                    this.objects[i] = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(objects[i]));
                }
            }
        }

        public XmtpMessage(string endpoint, Guid requestID, object[] objects)
        {
            this.requestID = requestID.ToByteArray();
            this.endpoint = Encoding.UTF8.GetBytes(endpoint);
            this.objects = new byte[objects.Length][];
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] is byte[])
                {
                    this.objects[i] = (byte[])objects[i];
                }
                else
                {
                    this.objects[i] = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(objects[i]));
                }
            }
        }

        void GetLength(out int length, out int requestLength)
        {
            length = 12;
            requestLength = 0;
            if (requestID != null)
            {
                length += requestLength = requestID.Length;
            }
            length += objects.Length * 4 + endpoint.Length;
            for (int i = 0; i < objects.Length; i++)
            {
                length += objects[i].Length;
            }
        }

        public byte[] Serialize()
        {
            GetLength(out int length, out int requestLength);
            byte[] message = new byte[length + 4];
            int position = 0;

            // Write message size
            Buffer.BlockCopy(BitConverter.GetBytes(length), 0, message, position, 4); 
            position += 4;

            // Write endpoint size
            Buffer.BlockCopy(BitConverter.GetBytes(endpoint.Length), 0, message, position, 4); 
            position += 4;

            // Write endpoint
            Buffer.BlockCopy(endpoint, 0, message, position, endpoint.Length);
            position += endpoint.Length;

            // Write request ID size
            Buffer.BlockCopy(BitConverter.GetBytes(requestLength), 0, message, position, 4); 
            position += 4;

            // Write request ID (if exists)
            if (requestID != null)
            {
                Buffer.BlockCopy(requestID, 0, message, position, requestLength);
                position += requestLength;
            }
            
            // Write object count
            Buffer.BlockCopy(BitConverter.GetBytes(objects.Length), 0, message, position, 4);
            position += 4;

            // Write objects
            for (int i = 0; i < objects.Length; i++)
            {
                byte[] o = objects[i];
                // Write object size
                Buffer.BlockCopy(BitConverter.GetBytes(o.Length), 0, message, position, 4);
                position += 4;

                // Write object
                Buffer.BlockCopy(o, 0, message, position, o.Length);
                position += o.Length;
            }

            return message;
        }
    }
}
