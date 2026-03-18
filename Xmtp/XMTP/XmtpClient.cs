using System.Collections.ObjectModel;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;

namespace Xmtp
{
    public class XmtpClient<T> where T : notnull
    {
        private T remoteID;
        private ClientControllerBase<T>[] controllers;
        private TcpClient client;
        private Stream stream;
        private CancellationTokenSource ReaderCancellationTokenSource;
        private CancellationTokenSource WriterCancellationTokenSource;
        private readonly AsyncQueue<byte[]> SenderQueue;
        private readonly AsyncQueue<byte[]> ReceiverQueue;
        private readonly ConcurrentArrayMap<Guid, TaskCompletionSource<byte[]>> PendingRequests;
        private readonly ReadOnlyDictionary<string, CompiledEndpoint> endpoints;

        readonly ILogger logger;
        readonly IControllerFactory controllerFactory;
        readonly IClientAuthenticator<T> clientAuthenticator;

        readonly ReadOnlyDictionary<Type, object> registeredServices;

        readonly bool useTls;
        readonly RemoteCertificateValidationCallback certificateValidationCallback;
        readonly X509Certificate2 certificate;

        private Action Connected = delegate { };
        private Action Disconnected = delegate { };

        public XmtpClient(string clientType,
            ILogger logger, IClientAuthenticator<T> clientAuthenticator, 
            ServiceLibrary services,
            bool useTls = false, RemoteCertificateValidationCallback certificateValidationCallback = null, X509Certificate2 certificate = null)
        {
            List<ControllerInfo> controllerInfos = 
                ControllerRegistry.RegisterControllers<ClientControllerAttribute>(clientType, typeof(ClientControllerBase<>), typeof(T));
            endpoints = ControllerRegistry.CompileEndpoints<T>(controllerInfos).AsReadOnly();

            this.registeredServices = services.RegisteredServices;

            List<Type> controllerTypes = controllerInfos.Select(s => s.Type).ToList();

            this.logger = logger;
            this.clientAuthenticator = clientAuthenticator;

            this.controllerFactory = new ControllerFactory<T>(controllerTypes, registeredServices);
            controllers = controllerFactory.InstantiateControllers().Select(s => (ClientControllerBase<T>)s).ToArray();

            this.useTls = useTls;
            this.certificateValidationCallback = certificateValidationCallback;
            this.certificate = certificate;

            foreach (var controller in controllers)
            {
                controller.Client = this;
            }

            SenderQueue = new AsyncQueue<byte[]>();
            ReceiverQueue = new AsyncQueue<byte[]>();
            PendingRequests = new ConcurrentArrayMap<Guid, TaskCompletionSource<byte[]>>();


        }

        ~XmtpClient()
        {
            client.Dispose();
            stream.Dispose();
            WriterCancellationTokenSource.Dispose();
            ReaderCancellationTokenSource.Dispose();
        }

        public async Task ConnectAsync(string address, int port, byte[] token, string targetHost = null)
        {
            ReaderCancellationTokenSource = new CancellationTokenSource();
            WriterCancellationTokenSource = new CancellationTokenSource();

            CancellationToken rct = ReaderCancellationTokenSource.Token;
            CancellationToken wct = WriterCancellationTokenSource.Token;

            try
            {
                client = new TcpClient();
                await client.ConnectAsync(address, port, rct);
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                throw;
            }

            stream = client.GetStream();

            try
            {
                if (useTls)
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

                    if (targetHost == null) targetHost = address;

                    X509Certificate2Collection certs = new X509Certificate2Collection();
                
                    if (certificate != null)
                    {
                        certs.Add(certificate);
                    }

                    SslProtocols protocols = SslProtocols.Tls12 | SslProtocols.Tls13;

                    await sslStream.AuthenticateAsClientAsync(targetHost, certs, protocols, false);

                    stream = sslStream;
                }

            
                T? ID = await clientAuthenticator.LogInAsync(stream, token, rct);
                if (ID == null)
                {
                    throw new Exception("Failed to receive remote ID");
                }
                remoteID = ID;
            }
            catch (Exception ex)
            {
                stream.Dispose();
                client.Dispose();
                logger.Log(ex);
                throw;
            }


            _ = Task.Run(() => RunMessageListener(rct));
            _ = Task.Run(() => RunMessageSender(wct));
            _ = Task.Run(() => RunMessageInvoker(rct));

            Connected.Invoke();
        }

        async Task RunMessageSender(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    byte[] message = await SenderQueue.DequeueAsync(ct);
                    await stream.WriteAsync(message, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.Log(ex);
            }
        }

        async Task RunMessageListener(CancellationToken ct)
        {
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
                    ReceiverQueue.Enqueue(messageBuffer);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.Log(ex);
            }
            finally
            {
                RemoteDisconnected();
            }
        }

        void RemoteDisconnected()
        {
            if (!WriterCancellationTokenSource.IsCancellationRequested)
            {
                WriterCancellationTokenSource.Cancel();
                client.Client.Shutdown(SocketShutdown.Send);
            }
            ReaderCancellationTokenSource.Cancel();
            Disconnected.Invoke();
        }

        async Task RunMessageInvoker(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    byte[] bytes = await ReceiverQueue.DequeueAsync(ct);
                    XmtpDeliveredMessage message = new XmtpDeliveredMessage(bytes);
                    await InvokeEndpoint(message);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.Log(ex.ToString());
            }
        }

        async Task InvokeEndpoint(XmtpDeliveredMessage message)
        {
            try
            {
                if (message.Endpoint == "response")
                {
                    HandleResponse(message);
                    return;
                }
                if (!endpoints.TryGetValue(message.Endpoint, out CompiledEndpoint endpoint))
                {
                    logger.Log($"Endpoint {message.Endpoint} not found");
                    InvocationFailed(message);
                    return;
                }
                if (message.Objects.Length != endpoint.ParameterTypes.Length)
                {
                    logger.Log($"Parameter count mismatch at endpoint {message.Endpoint}");
                    InvocationFailed(message);
                    return;
                }
                if ((endpoint.IsRequest && message.RequestID == null))
                {
                    logger.Log($"Invalid request format at endpoint {message.Endpoint}");
                    InvocationFailed(message);
                    return;
                }
                object controller = controllers[endpoint.ControllerIndex];
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
                        SendResponse(message.RequestID!.Value, result);
                    }
                    else
                    {
                        Func<object, object[], object> Delegate = (Func<object, object[], object>)endpoint.Delegate;
                        object result = Delegate(controller, parameters);
                        SendResponse(message.RequestID!.Value, result);
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
                InvocationFailed(message);
            }
        }

        void InvocationFailed(XmtpDeliveredMessage message)
        {
            if (message.RequestID != null)
            {
                SendResponseError(message.RequestID.Value);
            }
        }

        public void SendMessage(string endpoint, params object[] objects)
        {
            XmtpMessage message = new XmtpMessage(endpoint, objects);
            byte[] bytes = message.Serialize();
            SenderQueue.Enqueue(bytes);
        }

        public async Task<XmtpMessageResponse<TResponse>> SendRequest<TResponse>(string endpoint, params object[] objects)
        {
            Guid requestID = Guid.NewGuid();
            XmtpMessage message = new XmtpMessage(endpoint, requestID, objects);
            byte[] bytes = message.Serialize();

            TaskCompletionSource<byte[]> task = new();
            PendingRequests.Add(requestID, task);

            SenderQueue.Enqueue(bytes);

            Task result = await Task.WhenAny(task.Task, Task.Delay(15000));

            bool timeout = result != task.Task;
            if (timeout)
            {
                PendingRequests.TryRemove(requestID, out _);
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

        void SendResponse(Guid requestID, object response)
        {
            XmtpMessage message = new XmtpMessage("response", requestID, [response]);
            byte[] bytes = message.Serialize();
            SenderQueue.Enqueue(bytes);
        }

        void SendResponseError(Guid requestID)
        {
            XmtpMessage message = new XmtpMessage("response", requestID, []);
            byte[] bytes = message.Serialize();
            SenderQueue.Enqueue(bytes);
        }

        void HandleResponse(XmtpDeliveredMessage message)
        {
            try
            {
                if (message.RequestID != null && PendingRequests.TryGetValue(message.RequestID.Value, out var task))
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

        public void CloseConnection()
        {
            WriterCancellationTokenSource.Cancel();
            client.Client.Shutdown(SocketShutdown.Send);
        }

        public void AddOnConnectEvent(Action action)
        {
            Connected += action;
        }

        public void RemoveOnConnectEvents()
        {
            Connected = delegate { };
        }

        public void AddOnDisconnectEvent(Action action)
        {
            Disconnected += action;
        }

        public void RemoveOnDisconnectEvents()
        {
            Disconnected = delegate { };
        }

    }
}
