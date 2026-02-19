using System.Threading.Tasks;
using Xmtp;

public static class Program
{
    static async Task Main(string[] args)
    {
        // Create services before registering them
        ICustomService customService = new CustomService();

        ServiceLibrary serviceLibrary = new ServiceLibrary();
        serviceLibrary.AddService<ICustomService>(customService);

        XmtpClient<string> client = new XmtpClient<string>(
            clientType: "client", // Client name used to log controllers
            logger: new ConsoleLogger(), // Logger object to log errors
            clientAuthenticator: new ClientAuthenticator<string>(), // A handler for the initial handshake to update TCP to XMTP on client side
            services: new ServiceLibrary, // A library that contains all services used by the controllers
            useTls: false, // A switch to try connecting to server with TLS
            certificateValidationCallback: null, // A certificate validator for validating server certificate (if null, uses default)
            certificate: null // A shown up certificate used for mTLS
            );

        byte[] authToken = new byte[1] { 1 }; // sent for authentication
        try
        {
            await client.ConnectAsync(
                address: "server.com", // server IP address or domain name
                port: 5000, // server port
                token: authToken, // token used for authentication - the server can be accept any token, if configured that way
                targetHost: "server.com" // expected domain name for TLS handshake
                );
        }
        catch
        {
            // Failed to connect
        }

        // Send messages
        client.SendMessage("custom/hello");

        // Send requests
        XmtpMessageResponse<string> result = await client.SendRequest<string>("custom/get_hello_async", 10);
        string response;
        if (result.ResultCode == XmtpResultCode.Success)
        {
            response = result.Value!;
        }
    }
}

