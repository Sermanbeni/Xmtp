using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xmtp
{
    public struct ReceivedMessage
    {
        public readonly byte[] Message;
        public readonly bool Tcp;

        public ReceivedMessage(byte[] message, bool tcp = true)
        {
            Message = message;
            Tcp = tcp;
        }
    }
}
