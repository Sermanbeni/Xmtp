# Request handling

# Introduction
The framework supports request-response messaging. 
Request-response messaging is not a core feature, but an implementation on top of the protocol design.

# Sending a request
1. When a request is sent:
    A Request ID is generated and
    A waiting task is created (TaskCompletionSource)
2. The waiting task is loaded into a library linked to the host to which the request was sent with the request ID as key.
3. The request is sent and the waiting task begins waiting.
   ... (receive response)
4. On response arrived, it is cast into the response type and returned.

# Receiving a response
1. When a message arrives, and the endpoint name is "response" it is not handled as a regular endpoint, but as a response to a pending request.
2. Searches for the Request ID in the pending request library of the remote and finds pending request
3. Completes pending request, returning the response to the waiting request

# Summary
1. Requests are not blocking the messaging flow.
2. The requests and the responses are not different from any other messages, they enter the queue independently.
3. Multiple concurrent requests can coexist without blocking the flow.
4. A response can only be returned from the remote it was sent to - other remote cannot use the request ID of another.
