using System;
using System.Collections.Concurrent;
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
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly ConcurrentDictionary<string, IXmtpServer> servers;
        private readonly AsyncQueue<ReceivedClient> receivedClients;

        private record struct ReceivedClient(IXmtpServer server, TcpClient client);

        public XmtpListener(int port)
        {
            listener = TcpListener.Create(port);
            cancellationTokenSource = new CancellationTokenSource();
            servers = new ConcurrentDictionary<string, IXmtpServer>();
            receivedClients = new AsyncQueue<ReceivedClient>();
        }

        public void AddServer(string name, IXmtpServer server)
        {
            servers.TryAdd(name, server);
        }

        public void RemoveServer(string name)
        {
            servers.TryRemove(name, out _);
        }

        public void Dispose()
        {
            listener.Dispose();
        }

        public async Task RunAsync()
        {
            try
            {
                await StartListener(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) { }

            IEnumerable<Task> handlers = servers.Values.SelectMany(s => s.StopServer()).ToArray();
            await Task.WhenAny
                (
                    Task.WhenAll(handlers),
                    Task.Delay(30 * 1000)
                );
        }

        private async Task StartListener(CancellationToken ct)
        {
            listener.Start();
            _ = Task.Run(() => AcceptClients(ct));
            while (!ct.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(ct);
                NetworkStream stream = client.GetStream();
                string serverName;
                IXmtpServer server;
                try
                {
                    serverName = await StreamUtilities.ReadTextAsync(stream, 32);
                    server = servers[serverName];
                }
                catch (Exception ex)
                {
                    client.Close();
                    client.Dispose();
                    continue;
                }
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
            cancellationTokenSource.Cancel();
            listener.Stop();
        }
    }
}
