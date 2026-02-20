# Message Delivery

The framework uses a TCP Client (System.Net.Sockets.TcpClient) for messaging. 
The protocol guarantees successful in-order delivery and execution.

## Quick summary (sending a message):
1. Call message sender method
2. Load message to sender queue
3. Send message from queue<br />
   ...<br />
4. Receive message on remote side
5. Invoke message endpoint method



## Quick summary (sending a request):
1. Call request sender method
2. Create Request ID, add it to message
3. Load message into the sender queue
4. Send message from queue
5. Wait for response to return<br />
   ...<br />
6. Receive message on remote side
7. Invoke message endpoint method
8. Send back endpoint return value with the message request ID<br />
   ...<br />
9. Receive message from remote
10. Return response to the pending request call



## When calling a message sending method:
1. The message gets serialized into a byte[] (ready for sending)
2. The message is enqueued into the sender queues of all selected remotes.

## When calling a request sending method:
1. A request ID is generated for the message and gets stored into the pending requests to the given remote.
2. The message gets serialized into a byte[]
3. The message is enqueued into the sender queues of all selected remotes.
4. Awaits for all responses to return or timeout.
5. Return the responses to the call.



## Message sender:
1. Waits for a message to arrive into the sender queue.
2. When there is a message, sends it to the remote.
3. Return to waiting.

## Message listener:
1. Wait for a message to arrive
2. Reads the message size (first 4 bytes)
3. Creates the appropriate message buffer
4. Reads the message
6. Loads the message into the invocation queue.
7. Return to waiting.

## Message invoker:
1. Waits for a message to be enqueued for invocation.
2. Deserializes the byte[] into an XmtpDeliveredMessage format.
3. If response -> handle response (read Response handling below)
4. Read Endpoint delegate and metadata by endpoint name -> drop message if not found
5. Check arrived parameter count and method parameter count equivalence -> drop message on mismatch
6. Check if Endpoint is request endpoint and message has no request ID -> drop message if calling a request without request ID
7. Get the controller of the endpoint
8. Deserialize objects into the endpoint parameter types. -> drop message on failed parsing
9. Invoke endpoint delegate
10. If the endpoint is a request endpoint -> return the endpoint return value with the request ID to the sender.

### Note:
If the message is dropped for a request, no response will be sent. (It will be patched later to return ResultCode.Blocked)

## Response handling:
1. Check if the message endpoint is "response":
    - A result returned for a pending request -> fulfill the pending request.

`XmtpMessageResponse<T>` has 2 fields:
- Result Code:
    1. Successful: response arrived, parsing into T type successful
    2. Parse Failed: response arrived, but parsing into T type failed
    3. Timeout: no response arrived
    4. Blocked: response arrived with null value as content (blocked by the remote)
- Value:
    1. Contains a T type value if Result Code is Successful.
    2. Otherwise the value is null.

## Methods for sending messages (client side):

1. `void SendMessage (string endpoint, params object[] objects)`
    - Sends a message to the server to the given endpoint.
    - The objects are the method parameters.

2. `Task<XmtpMessageResponse<TResponse>> SendRequest<TResponse>(string endpoint, params object[] objects)`
    - Sends a message to the server to the given endpoint with the parameters.
    - Awaits for a TResponse type response from the server.


## Methods for sending messages (server side):

1. `void SendMessage(T remoteID, string endpoint, params object[] objects)`
    - Sends a message to a selected remote to a selected endpoint with the parameters.

2. `void SendMulticast(IEnumerable<T> remoteIDs, string endpoint, params object[] objects)`
    - Sends a message to multiple selected remotes.

3. `void SendBroadcast(string endpoint, params object[] objects)`
    - Sends a message to all connected remotes.

4. `Task<XmtpMessageResponse<TResponse>> SendRequest<TResponse>(T remoteID, string endpoint, params object[] objects)`
    - Sends a request to one selected remote.
    - Awaits for a TResponse type response from the remote.

5. `Task<KeyValuePair<T, XmtpMessageResponse<TResponse>>> SendMultiRequest<TResponse>(IEnumerable<T> remoteIDs, string endpoint, params object[] objects`
    - Sends a request to multiple selected remotes.
    - Awaits for a TResponse type response from all selected remotes.

6. `Task<KeyValuePair<T, XmtpMessageResponse<TResponse>>> SendBroadcastRequest<TResponse>(string endpoint, params object[] objects)`
    - Sends a request to all connected remotes.
    - Awaits for a TResponse type response from all connected remotes.
