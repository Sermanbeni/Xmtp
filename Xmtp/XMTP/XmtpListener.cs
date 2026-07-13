using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Xmtp
{
    public class XmtpListener : IDisposable
    {
        private readonly TcpListener listener;
        private readonly Dictionary<string, IXmtpServer> servers;
        private readonly AsyncQueue<ReceivedClient> receivedClients;

        private record struct ReceivedClient(IXmtpServer server, TcpClient client);

        public XmtpListener(int port)
        {
            listener = TcpListener.Create(port);
            servers = new Dictionary<string, IXmtpServer>();
            receivedClients = new AsyncQueue<ReceivedClient>();
        }

        public void AddServer(string name, IXmtpServer server)
        {
            servers.Add(name, server);
        }

        public void Dispose()
        {
            listener.Dispose();
        }

        public async Task StartListener(CancellationToken ct)
        {
            listener.Start();
            _ = Task.Run(() => AcceptClients(ct));
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(ct);
                NetworkStream stream = client.GetStream();
                string serverName;
                try
                {
                    serverName = await StreamUtilities.ReadTextAsync(stream, 32);
                }
                catch (Exception ex)
                {
                    client.Close();
                    client.Dispose();
                    continue;
                }
                IXmtpServer server = servers[serverName];
                ReceivedClient receivedClient = new ReceivedClient(server, client);
                receivedClients.Enqueue(receivedClient);
            }
        }

        private async Task AcceptClients(CancellationToken ct)
        {
            while (true)
            {
                ReceivedClient client = await receivedClients.DequeueAsync(ct);
                await client.server.OpenConnection(client.client, ct);
            }
        }

        public void StopListener()
        {
            listener.Stop();
        }
    }
}
