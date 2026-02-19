using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xmtp;

public class AdvancedConsoleLogger : ILogger
{
    string header;

    public AdvancedConsoleLogger(string header)
    {
        this.header = header;
    }

    public void Log(object message)
    {
        Console.WriteLine($"[{header} {DateTime.UtcNow.ToString("yyyy.MM.dd HH:mm:ss")}] {message}");
    }
}
