# Messages

The framework uses a custom message frame:

`[Message Size (4 byte)]`<br />
`[Endpoint Size (4 byte)]`<br />
`[Endpoint]`<br />
`[RequestID Size (4 byte)]`<br />
`[RequestID]`<br />
`[Object Count (4 byte)]`<br />
`--- For each object`<br />
`[Object Size (4 byte)]`<br />
`[Object]`<br />
`---`<br />

- Each message contains:
    - An Endpoint,
    - An optional Request ID
    - N objects

- Message types:
    - Message: The message is delivered to an endpoint without a return value, invoked and returns no response
    - Request: The message is delivered to an endpoint with a return value, invoked and returns the return value of the method as a response.

1. The endpoint determines the method to be called.

2. The Request ID is an optional field used only for requests. 
    If the endpoint is a response endpoint, and the message contains a request ID, the return value will be returned as a response with the request ID.
    If the message is not a request, the request ID is empty.

3. The objects are the parameters for the method, to be parsed for invocation.
    The sent object count and types must match the endpoint method parameter count and types to be called, otherwise the message is dropped.

