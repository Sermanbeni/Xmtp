using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Xmtp
{
    public interface IXmtpServer
    {
        Task OpenConnection(TcpClient tcpClient, CancellationToken ct);

        IEnumerable<Task> StopServer();
    }
}
