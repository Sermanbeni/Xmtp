using Xmtp;

public static class Program
{
    static XmtpServer<string> server;

    static async Task Main(string[] args)
    {
        // Create services before registering them for controllers
        ICustomService customService = new CustomService();

        // A service container that contains the services required by the controllers. The services need to be created before adding.
        ServiceLibrary serviceLibrary = new ServiceLibrary();
        serviceLibrary.AddService<ICustomService>(customService);

        XmtpServer<string> server =
            new XmtpServer<string>(
                serverType: "server", // Server name used to log controllers
                port: 5000, // Port used by the listener
                logger: new ConsoleLogger(), // Logger object to log errors
                connectionInitializer: new ConnectionInitializer(16, 15000), // A handler for the initial handshake to update TCP to XMTP
                authenticator: new Authenticator<string>(), // An authenticator controller that processes arrived token from the handshake
                services: new ServiceLibrary(), // A library that contains all services used by the controllers
                certificate: null, // A shown up certificate used for TLS (if null, no TLS)
                certificateValidationCallback: null, // A certificate validator for validating client certificate (mTLS) (if null, uses default)
                useClientAuthentication: false // A switch to set whether mTLS is required
                );

        Program.server = server;

        await server.RunAsync();
    }

    public static async Task SendExamples()
    {
        // send simple message
        server.SendMessage("remote ID", "custom/hello");

        // send multicast (same message to multiple remotes)
        server.SendMulticast(["remote ID 1", "remote ID 2", "remote ID 3"], "custom/hello");

        // send broadcast (same message to all remotes)
        server.SendBroadcast("custom/hello");

        // send request
        XmtpMessageResponse<string> result1 = await server.SendRequest<string>("remote ID", "custom/get_hello");
        string response1;
        if (result1.ResultCode == XmtpResultCode.Success)
        {
            response1 = result1.Value!;
        }

        // send multicast request (same request to multiple remotes)
        KeyValuePair<string, XmtpMessageResponse<string>>[] result2 = 
            await server.SendMultiRequest<string>(["remote ID 1", "remote ID 2", "remote ID 3"], "custom/hello");
        for (int i = 0;  i < result2.Length; i++)
        {
            var r = result2[i];
            if (r.Value == null)
            {
                // error occurred
            }
            else
            {
                if (r.Value.ResultCode == XmtpResultCode.Success)
                {
                    string responseFromHost = r.Value.Value!;
                }
            }
        }

        // send broadcast request (same request to all remotes)
        KeyValuePair<string, XmtpMessageResponse<string>>[] result3 =
            await server.SendBroadcastRequest<string>("custom/hello");
        for (int i = 0; i < result3.Length; i++)
        {
            var r = result3[i];
            if (r.Value == null)
            {
                // error occurred
            }
            else
            {
                if (r.Value.ResultCode == XmtpResultCode.Success)
                {
                    string responseFromHost = r.Value.Value!;
                }
            }
        }
    }

    public static void StopServer()
    {
        server.StopServer();
    }
}