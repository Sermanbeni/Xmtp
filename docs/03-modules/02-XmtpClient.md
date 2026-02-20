# Xmtp Client class

`public class XmtpClient<T>` is a client type where `T` Type is the remote ID Type. When connecting to the server, the remote gets an ID.

## Features:

### Client Constructor:
```C#
XmtpClient(string clientType, ILogger logger, IClientAuthenticator<T> clientAuthenticator, ServiceLibrary services,
        bool useTls = false, RemoteCertificateValidationCallback certificateValidationCallback = null, X509Certificate2 certificate = null)
```

#### Parameters:
1. `ClientType`
    - A client name used to identify the controllers for the client.
        - To add a controller to the client, the controller must list the client type in the attribute list: `[ClientAttribute("clientType")]`
        - ClientAttribute may get multiple client types in the parameter list. If none is defined, the controller will be used by all clients from the application.
2. `Logger`
    - A `Logger` object that implements `ILogger` interface. 
    - The logger logs internal errors and information from the client.
3. `ClientAuthenticator`
    - A `ClientAuthenticator` object that implements `IClientAuthenticator` interface.
    - The client authenticator handles a client side handshake after the TCP handshake to the server.
    - It makes sure the client is connecting to this protocol, otherwise disconnects.
    - Custom Client Authenticators can be created to handle different custom handshakes.
    - In the end of the handshake the client must send a `byte[]` authentication token to the server side.
    - The server side pair is the `IConnectionInitializer` interface, which responds to the handshake.
        - For custom implementations these two must match!
    - It returns a `byte[]` token after the handshake that will be validated or rejected.
    - **NOTE**:
        - A simple `ClientAuthenticator` object is attached to the package. It is compatible with the attached server side `ConnectionInitializer`.
        - Override only if you need a custom handshake.
4. `Services`
    - A `ServiceLibrary` object that contains all services required by the controllers.
    - The services must be generated before starting the client and assigned to the library.
5. `UseTLS`
    - A bool to select whether to use TLS when connecting or not.
        - When connecting without TLS to a server with TLS the connection will fail
        - When connecting with TLS to a server without TLS the connection will fail
6. `CertificateValidationCallback`
    - A custom certificate validator to validate server certificate if using TLS.
    - If not assigned uses default validator.
7. `Certificate`
    - A certificate used for mTLS

#### On calling the constructor:
1. Compiles the controller constructors.
2. Creates controllers from constructors.
3. Compiles the controller endpoint methods into delegates and creates the endpoint routing table.
4. Prepares the client for connecting.

#### On connecting to server:
1. Connects to the server TCP listener.
2. Upgrades to TLS `SslStream`. (optional)
3. Finishes custom handshake from `ClientAuthenticator`.
4. Receives remote ID from server.
5. Starts message listener, sender and invoker tasks.

### API Methods:
1. `void AddOnConnectEvent(Action action)`
    - Add an action that is invoked when successfully connected to the server.
2. `void AddOnDisconnectEvent(Action action)`
    - Add an action that is invoked when disconnected from the server.
3. `void CloseConnection()`
    - Disconnects from the server
4. `Task ConnectAsync(string address, int port, byte[] token, string targetHost = null)`
    - Connect to a server with address + port combination
    - Sending the token at the end of the handshake
    - Target Host is used for Common name in the remote certificate
5. `void RemoveOnConnectEvents()`
    - Removes all connect actions. Everything that was added to be invoked when the client connects to the server will be removed.
6. `void RemoveOnDisconnectEvents()`
    - Removes all disconnect actions. Everything that was added to be invoked when the client disconnects from the server will be removed.
7. `void SendMessage(string endpoint, params object[] objects)`
    - Sends a message to the server to the selected endpoint with the listed parameters.
8. `Task<XmtpMessageResponse<TResponse>> SendRequest<TResponse>(string endpoint, params object[] objects)`
    - Send a request to the server to the selected endpoint with the listed parameters.
    - Waits for a `TResponse` type value to be returned.