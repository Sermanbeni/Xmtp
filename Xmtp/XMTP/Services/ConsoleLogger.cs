namespace Xmtp
{
    public class ConsoleLogger : ILogger
    {
        public void Log(object message)
        {
            Console.WriteLine(message);
        }
    }
}
