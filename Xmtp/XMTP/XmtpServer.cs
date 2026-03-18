using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Xmtp
{
    public class XmtpServer<T> where T : notnull
    {
        readonly ConcurrentDictionary<T, ConnectedClient<T>> Clients;
        readonly ReadOnlyDictionary<string, CompiledEndpoint> endpoints;
        readonly TcpListener listener;
        readonly CancellationTokenSource listenerToken;

        readonly string serverType;
        readonly int port;
        readonly bool useClientAuthentication;

        readonly ILogger logger;
        readonly IConnectionInitializer connectionInitializer;
        readonly IAuthenticator<T> authenticator;
        readonly IControllerFactory controllerFactory;

        readonly ReadOnlyDictionary<Type, object> registeredServices;

        readonly X509Certificate2 certificate;
        readonly RemoteCertificateValidationCallback certificateValidationCallback;

        readonly ReaderWriterLockSlim connectionLock;

        Action<T> Connected = delegate { };
        Action<T> Disconnected = delegate { };

        public XmtpServer(string serverType, int port, 
            ILogger logger, IConnectionInitializer connectionInitializer, IAuthenticator<T> authenticator,
            ServiceLibrary services, X509Certificate2 certificate = null,
            RemoteCertificateValidationCallback certificateValidationCallback = null,
            bool useClientAuthentication = false)
        {
            this.serverType = serverType;
            this.port = port;
            this.useClientAuthentication = useClientAuthentication;

            List<ControllerInfo> controllerInfos = 
                ControllerRegistry.RegisterControllers<ServerControllerAttribute>(serverType, typeof(ServerControllerBase<>), typeof(T));
            endpoints = ControllerRegistry.CompileEndpoints<T>(controllerInfos).AsReadOnly();
            var controllers = controllerInfos.Select(c => c.Type).ToList();

            this.registeredServices = services.RegisteredServices;

            listener = TcpListener.Create(port);
            listenerToken = new CancellationTokenSource();
            Clients = new ConcurrentDictionary<T, ConnectedClient<T>>();

            connectionLock = new ReaderWriterLockSlim();

            this.logger = logger;
            this.connectionInitializer = connectionInitializer;
            this.authenticator = authenticator;
            this.controllerFactory = new ControllerFactory<T>(controllers, registeredServices);

            this.certificate = certificate;
            this.certificateValidationCallback = certificateValidationCallback;

            logger.Log($"Server successfully configured");
            foreach (var endpoint in endpoints)
            {
                logger.Log($"Endpoint logged: {endpoint.Key}");
            }
        }

        public async Task RunAsync()
        {
            await RunConnectionListener(listenerToken.Token);
        }

        async Task RunConnectionListener(CancellationToken ct)
        {
            listener.Start();
            logger.Log($"Server started on port {port}");
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(ct);
                    if (client == null)
                    {
                        continue;
                    }
                    _ = Task.Run(() => OpenConnection(client, ct));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.Log(ex.ToString());
            }
            finally
            {
                listener.Stop();
                await Task.WhenAny(
                    Task.WhenAll(Clients.Select(c => c.Value.HandlerTasks)),
                    Task.Delay(30 * 1000));
            }
        }

        async Task OpenConnection(TcpClient client, CancellationToken ct)
        {
            Stream stream = client.GetStream();
            try
            {
                if (certificate != null)
                {
                    SslStream sslStream;
                    if (certificateValidationCallback == null)
                    {
                        sslStream = new SslStream(stream, false);
                    }
                    else
                    {
                        sslStream = new SslStream(stream, false, certificateValidationCallback);
                    }

                    Task timer = Task.Delay(15000);
                    Task r = await Task.WhenAny(sslStream.AuthenticateAsServerAsync(certificate, useClientAuthentication,
                        System.Security.Authentication.SslProtocols.Tls12, false), timer);

                    if (r == timer)
                    {
                        sslStream.Dispose();
                        client.Dispose();
                        return;
                    }

                    stream = sslStream;
                }

                byte[]? token = await connectionInitializer.InitiateConnection(stream, ct);
                if (token != null && authenticator.Authenticate(token, out T remoteID))
                {
                    byte[] message = connectionInitializer.CreateInitializationMessage(remoteID);
                    await stream.WriteAsync(message, 0, message.Length, ct);
                    RegisterConnection(remoteID, client, stream);
                }
                else
                {
                    //client.Client.LingerState = new LingerOption(true, 0);
                    //client.Close();
                    client.Dispose();
                    stream.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                client.Dispose();
                stream.Dispose();
            }
            catch (AuthenticationException)
            {
                client.Dispose();
                stream.Dispose();
            }
            catch (Exception e)
            {
                client.Dispose();
                stream.Dispose();

                logger.Log(e);
            }
        }

        void RegisterConnection(T remoteID, TcpClient client, Stream stream)
        {
            if (!connectionLock.TryEnterReadLock(1))
            {
                client.Client.LingerState = new LingerOption(true, 0);
                client.Close();
                client.Dispose();
                stream.Dispose();
                return;
            }
            try
            {
                CancellationTokenSource readerTokenSource = new CancellationTokenSource();
                CancellationTokenSource writerTokenSource = new CancellationTokenSource();

                ServerControllerBase<T>[] controllers = 
                    controllerFactory.InstantiateControllers().Select(c => (ServerControllerBase<T>)c).ToArray();

                foreach (var controller in controllers)
                {
                    controller.RemoteID = remoteID;
                    controller.Server = this;
                }

                ConnectedClient<T> connectedClient = 
                    new ConnectedClient<T>(remoteID, client, stream, controllers, readerTokenSource, writerTokenSource);

                Clients[remoteID] = connectedClient;

                CancellationToken rct = readerTokenSource.Token;
                CancellationToken wct = writerTokenSource.Token;

                Task listener = Task.Run(() => RunMessageListener(connectedClient, rct));
                Task sender = Task.Run(() => RunMessageSender(connectedClient, wct));
                Task invoker = Task.Run(() => RunMessageInvoker(connectedClient, rct));

                Connected.Invoke(remoteID);

                connectedClient.HandlerTasks = Task.WhenAll(listener, sender, invoker);
            }
            finally
            {
                connectionLock.ExitReadLock();
            }
        }

        async Task RunMessageListener(ConnectedClient<T> client, CancellationToken ct)
        {
            Stream stream = client.Stream;
            byte[] sizeBuffer = new byte[4];
            int messageSize;
            int arrivedBytes = 0;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int arrivedLength = 0;
                    while (arrivedLength < 4)
                    {
                        arrivedBytes = await stream.ReadAsync(sizeBuffer, arrivedLength, 4 - arrivedLength, ct);
                        if (arrivedBytes == 0) break;
                        arrivedLength += arrivedBytes;
                    }
                    if (arrivedBytes == 0) break;
                    messageSize = BitConverter.ToInt32(sizeBuffer, 0);

                    byte[] messageBuffer = new byte[messageSize];
                    int receivedSize = 0;
                    int remainingSize;
                    while (receivedSize < messageSize)
                    {
                        remainingSize = messageSize - receivedSize;
                        arrivedBytes = await stream.ReadAsync(messageBuffer, receivedSize, remainingSize, ct);
                        if (arrivedBytes == 0) break;
                        receivedSize += arrivedBytes;
                    }
                    if (arrivedBytes == 0) break;
                    client.ReceiverQueue.Enqueue(messageBuffer);
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException) { }
            catch (Exception ex)
            {
                logger.Log(ex);
            }
            finally
            {
                RemoteDisconnected(client);
            }
        }

        async Task RunMessageInvoker(ConnectedClient<T> client, CancellationToken ct)
        {
            AsyncQueue<byte[]> queue = client.ReceiverQueue;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    byte[] bytes = await queue.DequeueAsync(ct);
                    XmtpDeliveredMessage message = new XmtpDeliveredMessage(bytes);
                    await InvokeEndpoint(client, message);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.Log(ex.ToString());
            }
        }

        async Task InvokeEndpoint(ConnectedClient<T> client, XmtpDeliveredMessage message)
        {
            try
            {
                if (message.Endpoint == "response")
                {
                    HandleResponse(client, message);
                    return;
                }
                if (!endpoints.TryGetValue(message.Endpoint, out CompiledEndpoint? endpoint))
                {
                    logger.Log($"Endpoint {message.Endpoint} not found");
                    InvocationFailed(client, message);
                    return;
                }
                if (message.Objects.Length != endpoint.ParameterTypes.Length)
                {
                    logger.Log($"Parameter count mismatch at endpoint {message.Endpoint}");
                    InvocationFailed(client, message);
                    return;
                }
                if ((endpoint.IsRequest && message.RequestID == null))
                {
                    logger.Log($"Invalid request format at endpoint {message.Endpoint}");
                    InvocationFailed(client, message);
                    return;
                }
                object controller = client.Controllers[endpoint.ControllerIndex];
                object[] parameters = new object[endpoint.ParameterTypes.Length];
                for (int i = 0; i < endpoint.ParameterTypes.Length; i++)
                {
                    if (endpoint.ParameterTypes[i] == typeof(byte[]))
                    {
                        parameters[i] = message.Objects[i];
                    }
                    else
                    {
                        parameters[i] = JsonSerializer.Deserialize(Encoding.UTF8.GetString(message.Objects[i]), endpoint.ParameterTypes[i])!;
                    }
                }
                if (endpoint.IsRequest)
                {
                    if (endpoint.IsAsync)
                    {
                        Func<object, object[], Task<object>> Delegate = (Func<object, object[], Task<object>>)endpoint.Delegate;
                        object result = await Delegate(controller, parameters);
                        SendResponse(client.RemoteID, message.RequestID!.Value, result);
                    }
                    else
                    {
                        Func<object, object[], object> Delegate = (Func<object, object[], object>)endpoint.Delegate;
                        object result = Delegate(controller, parameters);
                        SendResponse(client.RemoteID, message.RequestID!.Value, result);
                    }
                }
                else
                {
                    if (endpoint.IsAsync)
                    {
                        Func<object, object[], Task> Delegate = (Func<object, object[], Task>)endpoint.Delegate;
                        await Delegate(controller, parameters);
                    }
                    else
                    {
                        Action<object, object[]> Delegate = (Action<object, object[]>)endpoint.Delegate;
                        Delegate(controller, parameters);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                InvocationFailed(client, message);
            }
        }

        void InvocationFailed(ConnectedClient<T> client, XmtpDeliveredMessage message)
        {
            if (message.RequestID != null)
            {
                SendResponseError(client.RemoteID, message.RequestID.Value);
            }
        }

        async Task RunMessageSender(ConnectedClient<T> client, CancellationToken ct)
        {
            AsyncQueue<byte[]> messageQueue = client.SenderQueue;
            Stream stream = client.Stream;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    byte[] message = await messageQueue.DequeueAsync(ct);
                    await stream.WriteAsync(message, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException) { }
            catch (SocketException)
            {
                RemoteDisconnected(client);
            }
            catch (Exception ex)
            {
                logger.Log(ex.ToString());
            }
        }

        public void SendMessage(T remoteID, string endpoint, params object[] objects)
        {
            XmtpMessage message = new XmtpMessage(endpoint, objects);
            byte[] bytes = message.Serialize();
            Clients[remoteID].SenderQueue.Enqueue(bytes);
        }

        public void SendMulticast(IEnumerable<T> remoteIDs, string endpoint, params object[] objects)
        {
            XmtpMessage message = new XmtpMessage(endpoint, objects);
            byte[] bytes = message.Serialize();
            foreach (T remoteID in remoteIDs)
            {
                Clients[remoteID].SenderQueue.Enqueue(bytes);
            }
        }

        public void SendBroadcast(string endpoint, params object[] objects)
        {
            XmtpMessage message = new XmtpMessage(endpoint, objects);
            byte[] bytes = message.Serialize();
            foreach (var client in Clients)
            {
                client.Value.SenderQueue.Enqueue(bytes);
            }
        }

        public async Task<XmtpMessageResponse<TResponse>> SendRequest<TResponse>(T remoteID, string endpoint, params object[] objects)
        {
            ConnectedClient<T> client = Clients[remoteID];

            Guid requestID = Guid.NewGuid();
            XmtpMessage message = new XmtpMessage(endpoint, requestID, objects);
            byte[] bytes = message.Serialize();

            return await HandleRequest<TResponse>(client, requestID, bytes);
        }

        public async Task<KeyValuePair<T, XmtpMessageResponse<TResponse>>[]> SendMultiRequest<TResponse>
            (IEnumerable<T> remoteIDs, string endpoint, params object[] objects)
        {
            Guid requestID = Guid.NewGuid();
            XmtpMessage message = new XmtpMessage(endpoint, requestID, objects);
            byte[] bytes = message.Serialize();

            KeyValuePair<T, XmtpMessageResponse<TResponse>>[] responses = 
                new KeyValuePair<T, XmtpMessageResponse<TResponse>>[remoteIDs.Count()];

            List<Task> requests = new List<Task>(remoteIDs.Count());

            int i = 0;
            foreach (T remoteID in remoteIDs)
            {
                requests.Add(WrapRequest<TResponse>(responses, i, remoteID, requestID, bytes));
                i++;
            }

            await Task.WhenAll(requests);

            return responses;
        }

        public async Task<KeyValuePair<T, XmtpMessageResponse<TResponse>>[]> SendBroadcastRequest<TResponse>
            (string endpoint, params object[] objects)
        {
            Guid requestID = Guid.NewGuid();
            XmtpMessage message = new XmtpMessage(endpoint, requestID, objects);
            byte[] bytes = message.Serialize();

            KeyValuePair<T, XmtpMessageResponse<TResponse>>[] responses =
                new KeyValuePair<T, XmtpMessageResponse<TResponse>>[Clients.Count];

            List<Task> requests = new List<Task>(Clients.Count());

            int i = 0;
            foreach (var client in Clients)
            {
                requests.Add(WrapRequest<TResponse>(responses, i, client.Value, client.Key, requestID, bytes));
                i++;
            }

            await Task.WhenAll(requests);

            return responses;
        }

        async Task WrapRequest<TResponse>(KeyValuePair<T, XmtpMessageResponse<TResponse>>[] responses, int index,
            ConnectedClient<T> client, T remoteID, Guid requestID, byte[] bytes)
        {
            try
            {
                XmtpMessageResponse<TResponse> response = await HandleRequest<TResponse>(client, requestID, bytes);
                responses[index] = new KeyValuePair<T, XmtpMessageResponse<TResponse>>(remoteID, response);
            }
            catch
            {
                responses[index] = new KeyValuePair<T, XmtpMessageResponse<TResponse>>(remoteID, null!);
            }
        }

        async Task WrapRequest<TResponse>
            (KeyValuePair<T, XmtpMessageResponse<TResponse>>[] responses, int index,
            T remoteID, Guid requestID, byte[] bytes)
        {
            try
            {
                ConnectedClient<T> client = Clients[remoteID];
                XmtpMessageResponse<TResponse> response = await HandleRequest<TResponse>(client, requestID, bytes);
                responses[index] = new KeyValuePair<T, XmtpMessageResponse<TResponse>>(remoteID, response);
            }
            catch
            {
                responses[index] = new KeyValuePair<T, XmtpMessageResponse<TResponse>>(remoteID, null!);
            }
        }

        async Task<XmtpMessageResponse<TResponse>> HandleRequest<TResponse>(ConnectedClient<T> client, Guid requestID, byte[] bytes)
        {
            TaskCompletionSource<byte[]> task = new();
            client.PendingRequests.Add(requestID, task);

            client.SenderQueue.Enqueue(bytes);

            Task result = await Task.WhenAny(task.Task, Task.Delay(15000));

            bool timeout = result != task.Task;
            if (timeout)
            {
                client.PendingRequests.TryRemove(requestID, out _);
                return new XmtpMessageResponse<TResponse>(XmtpResultCode.Timeout, default);
            }

            byte[] responseBytes = task.Task.Result;

            if (responseBytes == null)
            {
                return new XmtpMessageResponse<TResponse>(XmtpResultCode.Blocked, default);
            }

            if (typeof(TResponse).Equals(typeof(byte[])))
            {
                return new XmtpMessageResponse<TResponse>(XmtpResultCode.Success, (TResponse)(object)responseBytes);
            }

            bool conversionFailed = false;
            TResponse? response = default;
            try
            {
                response = JsonSerializer.Deserialize<TResponse>(responseBytes);
            }
            catch
            {
                conversionFailed = true;
            }

            if (conversionFailed)
            {
                return new XmtpMessageResponse<TResponse>(XmtpResultCode.ParseFailure, default);
            }

            return new XmtpMessageResponse<TResponse>(XmtpResultCode.Success, response);
        }

        void SendResponse(T remoteID, Guid requestID, object response)
        {
            XmtpMessage message = new XmtpMessage("response", requestID, [response]);
            byte[] bytes = message.Serialize();
            Clients[remoteID].SenderQueue.Enqueue(bytes);
        }

        void SendResponseError(T remoteID, Guid requestID)
        {
            XmtpMessage message = new XmtpMessage("response", requestID, []);
            byte[] bytes = message.Serialize();
            Clients[remoteID].SenderQueue.Enqueue(bytes);
        }

        void RemoteDisconnected(ConnectedClient<T> client)
        {
            try
            {
                if (!client.WriterCancellationTokenSource.IsCancellationRequested)
                {
                    if (client.Client.Connected)
                    {
                        client.Client.Client.Shutdown(SocketShutdown.Send);
                    }
                    client.WriterCancellationTokenSource.Cancel();
                }
            }
            catch (Exception e) { }

            client.ReaderCancellationTokenSource.Cancel();
            if (Clients.TryRemove(client.RemoteID, out _))
            {
                Disconnected.Invoke(client.RemoteID);
            }
        }

        public bool GetRemoteIP(T remoteID, out IPAddress? ip)
        {
            bool found = Clients.TryGetValue(remoteID, out var client);
            if (found) ip = client!.IPAddress;
            else ip = null!;
            return found;
        }

        public void AddOnConnectAction(Action<T> action)
        {
            Connected += action;
        }

        public void RemoveOnConnectActions()
        {
            Connected = delegate { };
        }

        public void AddOnDisconnectAction(Action<T> action)
        {
            Disconnected += action;
        }

        public void RemoveOnDisconnectActions()
        {
            Disconnected = delegate { };
        }

        void HandleResponse(ConnectedClient<T> client, XmtpDeliveredMessage message)
        {
            try
            {
                if (message.RequestID != null && client.PendingRequests.TryGetValue(message.RequestID.Value, out var task))
                {
                    if (message.Objects.Length > 0)
                    {
                        task.SetResult(message.Objects[0]);
                    }
                    else
                    {
                        task.SetResult(null!);
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
            logger.Log("Invalid response message");
        }

        public void CloseConnection(T remoteID)
        {
            if (Clients.TryGetValue(remoteID, out var client))
            {
                CloseConnection(client);
            }
        }

        void CloseConnection(ConnectedClient<T> client)
        {
            client.WriterCancellationTokenSource.Cancel();
            client.Client.Client.Shutdown(SocketShutdown.Send);
        }

        public void StopServer()
        {
            connectionLock.EnterWriteLock();
            try
            {
                listenerToken.Cancel();
                foreach (var client in Clients)
                {
                    CloseConnection(client.Value);
                }
            }
            finally
            {
                connectionLock.ExitWriteLock();
            }
        }
    }
}
