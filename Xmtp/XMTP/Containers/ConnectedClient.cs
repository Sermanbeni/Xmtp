using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Xmtp;

namespace Xmtp
{
    public class ConnectedClient<T>
    {
        public readonly T RemoteID;
        public readonly TcpClient Client;
        public readonly Stream Stream;
        public readonly CancellationTokenSource ReaderCancellationTokenSource;
        public readonly CancellationTokenSource WriterCancellationTokenSource;
        public readonly AsyncQueue<byte[]> SenderQueue;
        public readonly AsyncQueue<ReceivedMessage> ReceiverQueue;
        public readonly ConcurrentArrayMap<Guid, TaskCompletionSource<byte[]>> PendingRequests;
        public readonly ServerControllerBase<T>[] Controllers;
        public readonly IPAddress IPAddress;
        public bool Connected { get; private set; }
        public EndPoint UdpEndpoint;
        public Task HandlerTasks;
        public CancellationTokenSource CloseCt;

        public ConnectedClient(T remoteID, TcpClient client, Stream stream, ServerControllerBase<T>[] controllers,
            CancellationTokenSource readerCancellationTokenSource, CancellationTokenSource writerCancellationTokenSource)
        {
            RemoteID = remoteID;
            Client = client;
            Stream = stream;
            Controllers = controllers;
            ReaderCancellationTokenSource = readerCancellationTokenSource;
            WriterCancellationTokenSource = writerCancellationTokenSource;
            SenderQueue = new AsyncQueue<byte[]>();
            ReceiverQueue = new AsyncQueue<ReceivedMessage>();
            PendingRequests = new ConcurrentArrayMap<Guid, TaskCompletionSource<byte[]>>();
            Connected = true;
            IPEndPoint iep = (IPEndPoint)client.Client.RemoteEndPoint!;
            IPAddress = iep.Address;
        }
        
        public void Disconnect()
        {
            Connected = false;
        }

        ~ConnectedClient()
        {
            Client.Dispose();
            Stream.Dispose();
            ReaderCancellationTokenSource.Dispose();
            WriterCancellationTokenSource.Dispose();
        }
    }
}
