# Request handling

## Sending a request
1. When a request is sent a Request ID is generated and a waiting task is created (`TaskCompletionSource`)
2. The waiting task is loaded into a library linked to the host to which the request was sent with the request ID as key.
3. The request is sent and the waiting task begins waiting.<br />
   ... (receive response)<br />
4. On response arrived, it is cast into the response type and returned.

## Receiving a response
Responses are messages arriving as a regular message. Their endpoint is `"response"` and they have a `RequestID`.

1. When a message arrives, and the endpoint name is `"response"` it is not handled as a regular endpoint, but as a response to a pending request.
2. Searches for the Request ID in the pending request library of the remote and finds pending request.
3. Completes pending request, returning the response to the waiting request.

## Summary
1. Requests are not blocking the messaging flow.
2. The requests and the responses are sent like any other messages.
3. Responses are sent back automatically when invoking a request endpoint.
4. Multiple concurrent requests can coexist without blocking the flow.
5. A response can only be returned from the remote it was sent to. Other remote cannot use the request ID of another.
