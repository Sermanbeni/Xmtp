# Xmtp Server class

`public class XmtpServer<T>` is a server type where `T` Type is the remote ID Type. When connecting to the server each remote gets an ID. 
The server contains a TCP listener, which runs on the configured port. Multiple remotes can connect and establish persistent connection with the server.

## Features:

### Server constructor
```C#
XmtpServer(string serverType, int port, ILogger logger,  IConnectionInitializer connectionInitializer, IAuthenticator<T> authenticator, ServiceLibrary services,
X509Certificate2 certificate = null, RemoteCertificateValidationCallback certificateValidationCallback = null, bool useClientAuthentication = false)
```

#### Parameters:
1. `ServerType`:
    - A server name used to identify the controllers for the server.
        - To add a controller to the server, the controller must list the server type in the attribute list: `[ServerAttribute("serverType")]`
        - ServerAttribute may get multiple server types in the parameter list. If none is defined, the controller will be used by all servers from the application.
2. `Port`:
    - The network port used by the TCP listener
3. `Logger`:
    - A `Logger` object that implements `ILogger` interface. 
    - The logger logs internal errors and information from the server.
4. `ConnectionInitializer`:
    - A `ConnectionInitializer` object that implements `IConnectionInitializer` interface.
    - The connection initializer handles a server side handshake after the TCP handshake with the connecting remote.
    - It makes sure the remote is connecting to this protocol, otherwise disconnects.
    - In the end of the handshake receives a `byte[]` authentication token from the remote.
    - Custom Connection Initializers can be created to handle different custom handshakes.
    - The client side pair is the `IClientAuthenticator` interface, which responds to the handshake.
        - For custom implementations these two must match!
    - It returns a `byte[]` token after the handshake that will be validated or rejected.
    - **NOTE**:
        - A simple `ConnectionInitializer` object is attached to the package. It is compatible with the attached client side `ClientAuthenticator`.
        - Override only if you need a custom handshake.
5. `Authenticator`:
    - An `Authenticator` object that implements `IAuthenticator` interface.
    - Validates the `byte[]` token received from the handshake from `ConnectionInitializer`:
        - On success, the client connects
        - On failure, the client is rejected
6. `Services`:
    - A `ServiceLibrary` object that contains all services required by the controllers.
    - The services must be generated before starting the server and assigned to the library.
7. `Certificate`:
    - A Certificate that is used for TLS.
8. `CertificateValidationCallback`:
    - A Certificate validator used to validate client certificate if using mTLS.
    - If `null` the default validator is used.
9. `UseClientAuthentication`:
    - A bool to select whether mTLS is required or not.

#### On calling the constructor:
1. Compiles the controller constructors. They will be ready to be instantiated when a new remote connects.
2. Compiles the controller endpoint methods into delegates and creates the endpoint routing table.
3. Creates TCP listener.

#### On remote connecting:
1. Remote connects to TCP listener.
2. Upgrades to TLS `SslStream`. (optional)
3. Finishes custom handshake from `ClientAuthenticator`.
4. Receives authentication token from remote.
5. Validate token and return remote ID.
6. Create controllers for the new remote.
7. Starts message listener, sender and invoker tasks.

### API Methods:
1. `void AddOnConnectAction(Action<T> action)`
    - Add an action that is invoked when a remote successfully connects.
2. `void AddOnDisconnectAction(Action<T> action)`
    - Add an action that is invoked when a remote disconnects.
3. `void CloseConnection(T remoteID)`
    - Disconnects from the selected remote
4. `bool GetRemoteIP(T remoteID, out IPAddress? ip)`
    - Gets the IP address of the selected remote
    - Returns false if the remote is not connected
5. `void RemoveOnConnectActions()`
    - Removes all connect actions. Everything that was added to be invoked when a remote connects will be removed.
6. `void RemoveOnDisconnectActions()`
    - Removes all disconnect actions. Everything that was added to be invoked when a remote disconnects will be removed.
7. `Task RunAsync()`
    - Start the server listener. Awaits while the listener is active or there are active connections.
8. `void SendMessage(T remoteID, string endpoint, params object[] objects)`
    - Sends a message to one selected remote to the selected endpoint with the listed parameters.
9. `void SendMulticast(IEnumerable<T> remoteIDs, string endpoint, params object[] objects)`
    - Sends a message to multiple selected remotes to the selected endpoint with the listed parameters.
10. `void SendBroadcast(string endpoint, params object[] objects)`
    - Sends a message to all remotes to the selected endpoint with the listed parameters.
11. `Task<XmtpMessageResponse<TResponse>> SendRequest<TResponse>(T remoteID, string endpoint, params object[] objects)`
    - Send a request to one selected remote to the selected endpoint with the listed parameters.
    - Waits for a `TResponse` type value to be returned.
12. `Task<KeyValuePair<T, XmtpMessageResponse<TResponse>>[]> SendMultiRequest<TResponse>(IEnumerable<T> remoteIDs, string endpoint, params object[] objects)`
    - Send a request to multiple selected remotes to the selected endpoint with the listed parameters.
    - Waits for a `TResponse` type value to be returned from all remotes.
    - Waits until all responses have arrived or timed out.
13. `Task<KeyValuePair<T, XmtpMessageResponse<TResponse>>[]> SendBroadcastRequest<TResponse>(string endpoint, params object[] objects)`
    - Sends a request to all remotes to the selected endpoint with the listed parameters.
    - Waits for a `TResponse` type value to be returned.
    - Waits until all responses have arrived or timed out.
14. `void StopServer()`
    - Disconnects from all remotes and stops the server.