# Xmtp (eXtensible Message Transfer Protocol) - Protocol and Framework
This is an RPC Server-Client Framework and Networking Protocol for native C# using System.Net.Sockets.TcpClient
The project is work in progress. It is currently being tested.

The protocol was designed for a custom task. It provides a symmetric RPC communication between the server and the client sides.
Supports TLS, mTLS with Official or Self Signed Certificates.

XMTP (eXtensible Message Transfer Protocol) Protocol and Framework:

The protocol is an RPC (Remote Procedure Call) protocol, allowing the remotes to
1. Call methods on the remote side
2. Get the return values of the remotely called methods. (Request-response messages)

## Server-Client model
1. The clients and servers are symmetric after building the connection.
2. The framework uses a persistent connection after the initial handshake, making the channel full-duplex.
3. Both clients and servers can send messages or requests to the remote side any time.

## Guarantee: Remote endpoints are invoked in method calling order
1. Messages are enqueued for sending in method calling order
2. Messages are sent in enqueueing order
3. Messages arrive in sending order (TCP guarantee)
4. Messages are enqueued into the invocation queue in arriving order
5. Messages are invoked from the queue in enqueueing order

## Features:
1. The framework uses persistent TCP connection on one multiplexed channel. Each message goes through the same TCP connection independently and routed to the according endpoint method.
2. Different, custom features can be built on top of this channel, making it extensible for any custom purpose.

Documentation in the docs folder
